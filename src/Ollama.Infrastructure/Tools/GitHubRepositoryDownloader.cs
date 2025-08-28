using Ollama.Domain.Tools;
using System.IO;
using System.Net.Http;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Tools
{
    public class GitHubRepositoryDownloader : ITool
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GitHubRepositoryDownloader>? _logger;

        public GitHubRepositoryDownloader(HttpClient httpClient, ILogger<GitHubRepositoryDownloader>? logger = null)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public string Name => "GitHubDownloader";
        public string Description => "Downloads a GitHub repository as a ZIP archive";
        public IEnumerable<string> Capabilities => new[] { "repo:download", "github:clone" };
        public bool RequiresNetwork => true;
        public bool RequiresFileSystem => true;

        public Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(context.Parameters.ContainsKey("repoUrl"));
        }

        public Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // Free operation
        }

        public async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            _logger?.LogInformation("GitHubDownloader: Starting repository download");
            
            if (!context.Parameters.TryGetValue("repoUrl", out var repoUrlObj))
            {
                _logger?.LogError("GitHubDownloader: Repository URL parameter is missing");
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = "Repository URL is required",
                    ExecutionTime = DateTime.Now - startTime
                };
            }

            var repoUrl = repoUrlObj.ToString();
            _logger?.LogInformation("GitHubDownloader: Processing repository URL: {RepoUrl}", repoUrl);
            
            // Convert GitHub URL to API format for download
            var apiUrl = ConvertToGitHubApiUrl(repoUrl!);
            _logger?.LogInformation("GitHubDownloader: Converted to API URL: {ApiUrl}", apiUrl);
            
            try
            {
                // Create a directory in cache if it doesn't exist
                var cacheDir = Path.Combine(context.WorkingDirectory ?? Path.GetTempPath(), "cache");
                Directory.CreateDirectory(cacheDir);
                _logger?.LogDebug("GitHubDownloader: Created cache directory: {CacheDir}", cacheDir);
                
                var repoName = ExtractRepoName(repoUrl!);
                var zipPath = Path.Combine(cacheDir, $"{repoName}.zip");
                var extractPath = Path.Combine(cacheDir, repoName);
                _logger?.LogInformation("GitHubDownloader: Repository name: {RepoName}, ZIP path: {ZipPath}, Extract path: {ExtractPath}", 
                    repoName, zipPath, extractPath);
                
                // Download the repository
                _logger?.LogInformation("GitHubDownloader: Starting download from GitHub API: {ApiUrl}", apiUrl);
                var response = await _httpClient.GetAsync(apiUrl, cancellationToken);
                
                _logger?.LogInformation("GitHubDownloader: Received response - Status: {StatusCode}, Content-Length: {ContentLength}", 
                    response.StatusCode, response.Content.Headers.ContentLength);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError("GitHubDownloader: HTTP request failed - Status: {StatusCode}, Reason: {ReasonPhrase}", 
                        response.StatusCode, response.ReasonPhrase);
                }
                
                response.EnsureSuccessStatusCode();
                
                _logger?.LogInformation("GitHubDownloader: Writing ZIP file to: {ZipPath}", zipPath);
                using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs, cancellationToken);
                }
                
                var fileInfo = new FileInfo(zipPath);
                _logger?.LogInformation("GitHubDownloader: ZIP file downloaded successfully - Size: {FileSize:N0} bytes", fileInfo.Length);
                
                // Extract the ZIP
                if (Directory.Exists(extractPath))
                {
                    _logger?.LogInformation("GitHubDownloader: Removing existing extract directory: {ExtractPath}", extractPath);
                    Directory.Delete(extractPath, true);
                }
                Directory.CreateDirectory(extractPath);
                
                _logger?.LogInformation("GitHubDownloader: Extracting ZIP file to: {ExtractPath}", extractPath);
                ZipFile.ExtractToDirectory(zipPath, extractPath, true);
                
                // Find the actual extracted folder (GitHub creates a folder with commit hash)
                var extractedFolders = Directory.GetDirectories(extractPath);
                _logger?.LogDebug("GitHubDownloader: Found {FolderCount} extracted folders", extractedFolders.Length);
                
                if (extractedFolders.Length > 0)
                {
                    context.State["repoPath"] = extractedFolders[0];
                    _logger?.LogInformation("GitHubDownloader: Repository extracted to: {RepoPath}", extractedFolders[0]);
                }
                else
                {
                    context.State["repoPath"] = extractPath;
                    _logger?.LogWarning("GitHubDownloader: No subdirectories found, using extract path: {ExtractPath}", extractPath);
                }
                
                var totalTime = DateTime.Now - startTime;
                _logger?.LogInformation("GitHubDownloader: Repository download completed successfully in {TotalTime:F2} seconds", 
                    totalTime.TotalSeconds);
                
                return new ToolResult
                {
                    Success = true,
                    Output = new
                    {
                        RepositoryPath = context.State["repoPath"],
                        DownloadedTo = zipPath
                    },
                    ExecutionTime = totalTime
                };
            }
            catch (Exception ex)
            {
                var totalTime = DateTime.Now - startTime;
                _logger?.LogError(ex, "GitHubDownloader: Repository download failed after {TotalTime:F2} seconds - Error: {ErrorMessage}", 
                    totalTime.TotalSeconds, ex.Message);
                
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to download repository: {ex.Message}",
                    ExecutionTime = totalTime
                };
            }
        }

        private string ConvertToGitHubApiUrl(string repoUrl)
        {
            _logger?.LogDebug("GitHubDownloader: Converting repository URL to API format: {RepoUrl}", repoUrl);
            
            // Remove trailing slash if present
            repoUrl = repoUrl.TrimEnd('/');
            
            // Extract owner and repo from URL
            var parts = repoUrl.Split('/');
            if (parts.Length < 2)
            {
                _logger?.LogError("GitHubDownloader: Invalid repository URL format: {RepoUrl}", repoUrl);
                throw new ArgumentException($"Invalid repository URL format: {repoUrl}");
            }
            
            var owner = parts[^2];
            var repo = parts[^1];
            var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/zipball";
            
            _logger?.LogDebug("GitHubDownloader: Extracted owner: {Owner}, repo: {Repo}, API URL: {ApiUrl}", 
                owner, repo, apiUrl);
            
            return apiUrl;
        }
        
        private string ExtractRepoName(string repoUrl)
        {
            var repoName = repoUrl.TrimEnd('/').Split('/').Last();
            _logger?.LogDebug("GitHubDownloader: Extracted repository name: {RepoName} from URL: {RepoUrl}", repoName, repoUrl);
            return repoName;
        }
    }
}
