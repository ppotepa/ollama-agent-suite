using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using System.IO;
using System.Net.Http;
using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Tools
{
    public class GitHubRepositoryDownloader : AbstractTool
    {
        private readonly HttpClient _httpClient;

        public GitHubRepositoryDownloader(ISessionScope sessionScope, ILogger<GitHubRepositoryDownloader> logger, HttpClient httpClient) 
            : base(sessionScope, logger)
        {
            _httpClient = httpClient;
        }

        public override string Name => "GitHubDownloader";
        public override string Description => "Downloads a GitHub repository as a ZIP archive";
        public override IEnumerable<string> Capabilities => new[] { "repo:download", "github:clone" };
        public override bool RequiresNetwork => true;
        public override bool RequiresFileSystem => true;

        public override Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(context.Parameters.ContainsKey("repoUrl"));
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // Free operation
        }

        public override async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                // Ensure SessionScope is initialized with correct sessionId from context
                EnsureSessionScopeInitialized(context);
                
                Logger.LogInformation("GitHubDownloader: Starting repository download");
                
                if (!context.Parameters.TryGetValue("repoUrl", out var repoUrlObj))
                {
                    Logger.LogError("GitHubDownloader: Repository URL parameter is missing");
                    return CreateResult(false, errorMessage: "Repository URL is required", startTime: startTime);
                }

                // Validate session context
                if (string.IsNullOrEmpty(context.SessionId))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = "Session ID is required for repository download",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }

                var repoUrl = repoUrlObj.ToString();
                Logger.LogInformation("GitHubDownloader: Processing repository URL: {RepoUrl}", repoUrl);
                
                // Convert GitHub URL to API format for download
                var apiUrl = ConvertToGitHubApiUrl(repoUrl!);
                Logger.LogInformation("GitHubDownloader: Converted to API URL: {ApiUrl}", apiUrl);
                // Use session-safe working directory for cache
                var safeWorkingDir = SessionScope.FileSystem.GetSafeWorkingDirectory(context.SessionId);
                var cacheDir = Path.Combine(safeWorkingDir, "cache");
                
                // Validate cache directory is within session boundaries
                if (!SessionScope.FileSystem.IsWithinSessionBoundary(context.SessionId, cacheDir))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = "Cache directory is outside session boundaries",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }
                
                System.IO.Directory.CreateDirectory(cacheDir);
                Logger.LogDebug("GitHubDownloader: Created cache directory: {CacheDir}", cacheDir);
                
                var repoName = ExtractRepoName(repoUrl!);
                var zipPath = Path.Combine(cacheDir, $"{repoName}.zip");
                var extractPath = Path.Combine(cacheDir, repoName);
                
                // Validate all paths are within session boundaries
                if (!SessionScope.FileSystem.IsWithinSessionBoundary(context.SessionId, zipPath) ||
                    !SessionScope.FileSystem.IsWithinSessionBoundary(context.SessionId, extractPath))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = "Download paths are outside session boundaries",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }
                
                Logger.LogInformation("GitHubDownloader: Repository name: {RepoName}, ZIP path: {ZipPath}, Extract path: {ExtractPath}", 
                    repoName, zipPath, extractPath);
                
                // Download the repository
                Logger.LogInformation("GitHubDownloader: Starting download from GitHub API: {ApiUrl}", apiUrl);
                var response = await _httpClient.GetAsync(apiUrl, cancellationToken);
                
                Logger.LogInformation("GitHubDownloader: Received response - Status: {StatusCode}, Content-Length: {ContentLength}", 
                    response.StatusCode, response.Content.Headers.ContentLength);
                
                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogError("GitHubDownloader: HTTP request failed - Status: {StatusCode}, Reason: {ReasonPhrase}", 
                        response.StatusCode, response.ReasonPhrase);
                }
                
                response.EnsureSuccessStatusCode();
                
                Logger.LogInformation("GitHubDownloader: Writing ZIP file to: {ZipPath}", zipPath);
                using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs, cancellationToken);
                }
                
                var fileInfo = new FileInfo(zipPath);
                Logger.LogInformation("GitHubDownloader: ZIP file downloaded successfully - Size: {FileSize:N0} bytes", fileInfo.Length);
                
                // Extract the ZIP
                if (System.IO.Directory.Exists(extractPath))
                {
                    Logger.LogInformation("GitHubDownloader: Removing existing extract directory: {ExtractPath}", extractPath);
                    System.IO.Directory.Delete(extractPath, true);
                }
                System.IO.Directory.CreateDirectory(extractPath);
                
                Logger.LogInformation("GitHubDownloader: Extracting ZIP file to: {ExtractPath}", extractPath);
                ZipFile.ExtractToDirectory(zipPath, extractPath, true);
                
                // Find the actual extracted folder (GitHub creates a folder with commit hash)
                var extractedFolders = System.IO.Directory.GetDirectories(extractPath);
                Logger.LogDebug("GitHubDownloader: Found {FolderCount} extracted folders", extractedFolders.Length);
                
                if (extractedFolders.Length > 0)
                {
                    context.State["repoPath"] = extractedFolders[0];
                    Logger.LogInformation("GitHubDownloader: Repository extracted to: {RepoPath}", extractedFolders[0]);
                }
                else
                {
                    context.State["repoPath"] = extractPath;
                    Logger.LogWarning("GitHubDownloader: No subdirectories found, using extract path: {ExtractPath}", extractPath);
                }
                
                var totalTime = DateTime.Now - startTime;
                Logger.LogInformation("GitHubDownloader: Repository download completed successfully in {TotalTime:F2} seconds", 
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
                Logger.LogError(ex, "GitHubDownloader: Repository download failed after {TotalTime:F2} seconds - Error: {ErrorMessage}", 
                    totalTime.TotalSeconds, ex.Message);
                
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to download repository: {ex.Message}",
                    ExecutionTime = totalTime
                };
            }
        }

        private ToolResult CreateResult(bool success, string? output = null, string? errorMessage = null, DateTime? startTime = null)
        {
            return new ToolResult
            {
                Success = success,
                Output = output,
                ErrorMessage = errorMessage,
                ExecutionTime = startTime.HasValue ? DateTime.Now - startTime.Value : TimeSpan.Zero
            };
        }

        private string ConvertToGitHubApiUrl(string repoUrl)
        {
            Logger.LogDebug("GitHubDownloader: Converting repository URL to API format: {RepoUrl}", repoUrl);
            
            // Remove trailing slash if present
            repoUrl = repoUrl.TrimEnd('/');
            
            // Extract owner and repo from URL
            var parts = repoUrl.Split('/');
            if (parts.Length < 2)
            {
                Logger.LogError("GitHubDownloader: Invalid repository URL format: {RepoUrl}", repoUrl);
                throw new ArgumentException($"Invalid repository URL format: {repoUrl}");
            }
            
            var owner = parts[^2];
            var repo = parts[^1];
            var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/zipball";
            
            Logger.LogDebug("GitHubDownloader: Extracted owner: {Owner}, repo: {Repo}, API URL: {ApiUrl}", 
                owner, repo, apiUrl);
            
            return apiUrl;
        }
        
        private string ExtractRepoName(string repoUrl)
        {
            var repoName = repoUrl.TrimEnd('/').Split('/').Last();
            Logger.LogDebug("GitHubDownloader: Extracted repository name: {RepoName} from URL: {RepoUrl}", repoName, repoUrl);
            return repoName;
        }
    }
}
