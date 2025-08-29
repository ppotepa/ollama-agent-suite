using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Net.Http;
using System.IO.Compression;

namespace Ollama.Infrastructure.Tools.Download
{
    /// <summary>
    /// Comprehensive download tool supporting multiple sources (GitHub, GitLab, NuGet, etc.)
    /// Downloads files and repositories to session directories with cursor navigation support
    /// </summary>
    public class DownloadTool : AbstractTool
    {
        private readonly HttpClient _httpClient;

        public override string Name => "Download";
        public override string Description => "Downloads files, repositories, and packages from various sources";
        public override IEnumerable<string> Capabilities => new[] { 
            "download:file", "download:repository", "download:package", 
            "github:download", "gitlab:download", "nuget:download", 
            "cursor:navigate", "fs:download" 
        };
        public override bool RequiresNetwork => true;
        public override bool RequiresFileSystem => true;

        public DownloadTool(ISessionScope sessionScope, ILogger<DownloadTool> logger, HttpClient httpClient) 
            : base(sessionScope, logger)
        {
            _httpClient = httpClient;
        }

        public override Task<bool> DryRunAsync(ToolContext context)
        {
            var missingParams = ValidateRequiredParameters(context, "url");
            return Task.FromResult(missingParams.Length == 0);
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.1m); // Small cost for network operations
        }

        public override async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                // Ensure SessionScope is initialized with correct sessionId from context
                EnsureSessionScopeInitialized(context);
                
                // Process cursor navigation first
                var navigationResult = ProcessCursorNavigation(context);
                
                // Validate required parameters
                var missingParams = ValidateRequiredParameters(context, "url");
                if (missingParams.Length > 0)
                {
                    return CreateResult(false, errorMessage: $"Missing required parameters: {string.Join(", ", missingParams)}", startTime: startTime);
                }

                var url = context.Parameters["url"].ToString()!;
                
                // Get options
                var targetDirectory = context.Parameters.TryGetValue("targetDirectory", out var targetDirObj) 
                    ? targetDirObj?.ToString() ?? "downloads" 
                    : "downloads";
                var filename = context.Parameters.TryGetValue("filename", out var filenameObj) 
                    ? filenameObj?.ToString() 
                    : null;
                var extractArchives = context.Parameters.TryGetValue("extractArchives", out var extractObj) 
                    && extractObj is bool extract && extract;
                var overwrite = context.Parameters.TryGetValue("overwrite", out var overwriteObj) 
                    && overwriteObj is bool ow && ow;

                // Auto-detect download source
                var downloadSource = DownloadSourceExtensions.DetectFromUrl(url);
                
                // Perform download
                var result = await PerformDownload(url, downloadSource, targetDirectory, filename, extractArchives, overwrite, cancellationToken);
                
                Logger.LogInformation("Download completed from: {Url}", url);
                
                return CreateSuccessResultWithContext(result, navigationResult, startTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during download");
                return CreateResult(false, errorMessage: $"Download failed: {ex.Message}", startTime: startTime);
            }
        }

        private async Task<string> PerformDownload(string url, DownloadSource source, string targetDirectory, string? filename, bool extractArchives, bool overwrite, CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            
            try
            {
                // Create target directory
                var targetPath = GetSafePath(targetDirectory);
                if (!System.IO.Directory.Exists(targetPath))
                {
                    System.IO.Directory.CreateDirectory(targetPath);
                    sb.AppendLine($"Created directory: {GetRelativePath(targetPath)}");
                }

                // Determine filename if not provided
                if (string.IsNullOrEmpty(filename))
                {
                    filename = GetFilenameFromUrl(url, source);
                }

                var filePath = Path.Combine(targetPath, filename);
                
                // Check if file exists and handle overwrite
                if (System.IO.File.Exists(filePath) && !overwrite)
                {
                    throw new InvalidOperationException($"File already exists: {GetRelativePath(filePath)}. Use overwrite=true to replace.");
                }

                sb.AppendLine($"Download source: {source.GetDisplayName()}");
                sb.AppendLine($"Source URL: {url}");
                sb.AppendLine($"Target: {GetRelativePath(filePath)}");
                sb.AppendLine();

                // Perform the download based on source type
                var downloadInfo = await DownloadBySource(url, source, filePath, cancellationToken);
                sb.AppendLine(downloadInfo);

                // Handle archive extraction if requested
                if (extractArchives && IsArchiveFile(filename))
                {
                    var extractionInfo = await ExtractArchive(filePath, targetPath, cancellationToken);
                    sb.AppendLine();
                    sb.AppendLine("--- Archive Extraction ---");
                    sb.AppendLine(extractionInfo);
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Download operation failed: {ex.Message}", ex);
            }
        }

        private async Task<string> DownloadBySource(string url, DownloadSource source, string filePath, CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            var startTime = DateTime.Now;

            switch (source)
            {
                case DownloadSource.GitHub:
                    return await DownloadFromGitHub(url, filePath, cancellationToken);
                    
                case DownloadSource.GitLab:
                    return await DownloadFromGitLab(url, filePath, cancellationToken);
                    
                case DownloadSource.Http:
                default:
                    return await DownloadDirectFile(url, filePath, cancellationToken);
            }
        }

        private async Task<string> DownloadFromGitHub(string url, string filePath, CancellationToken cancellationToken)
        {
            // Convert GitHub URLs to download URLs if needed
            var downloadUrl = ConvertGitHubUrl(url);
            return await DownloadDirectFile(downloadUrl, filePath, cancellationToken);
        }

        private async Task<string> DownloadFromGitLab(string url, string filePath, CancellationToken cancellationToken)
        {
            // Convert GitLab URLs to download URLs if needed
            var downloadUrl = ConvertGitLabUrl(url);
            return await DownloadDirectFile(downloadUrl, filePath, cancellationToken);
        }

        private async Task<string> DownloadDirectFile(string url, string filePath, CancellationToken cancellationToken)
        {
            var startTime = DateTime.Now;
            
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var downloadedBytes = 0L;

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                downloadedBytes += bytesRead;
            }

            var fileInfo = new FileInfo(filePath);
            var duration = DateTime.Now - startTime;

            var sb = new StringBuilder();
            sb.AppendLine($"Download completed successfully");
            sb.AppendLine($"File size: {FormatFileSize(fileInfo.Length)}");
            sb.AppendLine($"Duration: {duration.TotalSeconds:F2} seconds");
            sb.AppendLine($"Average speed: {FormatFileSize((long)(fileInfo.Length / duration.TotalSeconds))}/s");

            return sb.ToString();
        }

        private async Task<string> ExtractArchive(string archiveFilePath, string extractToPath, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var sb = new StringBuilder();
                var archiveInfo = new FileInfo(archiveFilePath);
                var extractDir = Path.Combine(extractToPath, Path.GetFileNameWithoutExtension(archiveFilePath));

                if (System.IO.Directory.Exists(extractDir))
                {
                    System.IO.Directory.Delete(extractDir, true);
                }
                System.IO.Directory.CreateDirectory(extractDir);

                var extension = archiveInfo.Extension.ToLower();
                switch (extension)
                {
                    case ".zip":
                        ZipFile.ExtractToDirectory(archiveFilePath, extractDir);
                        break;
                    case ".gz":
                    case ".tar":
                        throw new NotSupportedException($"Archive format {extension} not yet supported");
                    default:
                        throw new NotSupportedException($"Unknown archive format: {extension}");
                }

                var extractedFiles = System.IO.Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories);
                sb.AppendLine($"Extracted to: {GetRelativePath(extractDir)}");
                sb.AppendLine($"Files extracted: {extractedFiles.Length}");

                return sb.ToString();
            }, cancellationToken);
        }

        private string ConvertGitHubUrl(string url)
        {
            // Convert GitHub repository URLs to archive download URLs
            if (url.Contains("github.com") && !url.Contains("archive/"))
            {
                // Convert https://github.com/owner/repo to https://github.com/owner/repo/archive/refs/heads/main.zip
                var parts = url.TrimEnd('/').Split('/');
                if (parts.Length >= 5)
                {
                    var owner = parts[3];
                    var repo = parts[4];
                    return $"https://github.com/{owner}/{repo}/archive/refs/heads/main.zip";
                }
            }
            return url;
        }

        private string ConvertGitLabUrl(string url)
        {
            // Similar conversion for GitLab URLs
            if (url.Contains("gitlab.com") && !url.Contains("/-/archive/"))
            {
                var parts = url.TrimEnd('/').Split('/');
                if (parts.Length >= 5)
                {
                    var owner = parts[3];
                    var repo = parts[4];
                    return $"https://gitlab.com/{owner}/{repo}/-/archive/main/{repo}-main.zip";
                }
            }
            return url;
        }

        private string GetFilenameFromUrl(string url, DownloadSource source)
        {
            try
            {
                var uri = new Uri(url);
                var filename = Path.GetFileName(uri.LocalPath);
                
                if (string.IsNullOrEmpty(filename) || filename == "/")
                {
                    // Generate filename based on source
                    return source switch
                    {
                        DownloadSource.GitHub => "github-repository.zip",
                        DownloadSource.GitLab => "gitlab-repository.zip",
                        DownloadSource.NuGet => "nuget-package.nupkg",
                        _ => "downloaded-file"
                    };
                }
                
                return filename;
            }
            catch
            {
                return "downloaded-file";
            }
        }

        private bool IsArchiveFile(string filename)
        {
            var extension = Path.GetExtension(filename).ToLower();
            return extension is ".zip" or ".tar" or ".gz" or ".rar" or ".7z";
        }
    }
}
