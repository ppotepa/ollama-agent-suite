using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using System.IO;
using System.Net.Http;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

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
        public override string Description => "Downloads a GitHub repository as a ZIP archive with multiple fallback methods";
        public override IEnumerable<string> Capabilities => new[] { "repo:download", "github:clone" };
        public override bool RequiresNetwork => true;
        public override bool RequiresFileSystem => true;
        
        /// <summary>
        /// Available alternative download methods
        /// </summary>
        public override IEnumerable<string> GetAlternativeMethods()
        {
            return new[] { "direct_main", "direct_master", "api_authenticated", "git_clone_fallback" };
        }

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
                // Use session-safe working directory for downloads
                var safeWorkingDir = SessionScope.FileSystem.GetSafeWorkingDirectory(context.SessionId);
                
                // Validate working directory is within session boundaries (should always be true)
                if (!SessionScope.FileSystem.IsWithinSessionBoundary(context.SessionId, safeWorkingDir))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = "Working directory is outside session boundaries",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }
                
                System.IO.Directory.CreateDirectory(safeWorkingDir);
                Logger.LogDebug("GitHubDownloader: Using working directory: {WorkingDir}", safeWorkingDir);
                
                var repoName = ExtractRepoName(repoUrl!);
                var zipPath = Path.Combine(safeWorkingDir, $"{repoName}.zip");
                var extractPath = Path.Combine(safeWorkingDir, repoName);
                
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
                
                // Download the repository with fallback from main to master
                Logger.LogInformation("GitHubDownloader: Starting download from GitHub: {ApiUrl}", apiUrl);
                var response = await _httpClient.GetAsync(apiUrl, cancellationToken);
                
                Logger.LogInformation("GitHubDownloader: Received response - Status: {StatusCode}, Content-Length: {ContentLength}", 
                    response.StatusCode, response.Content.Headers.ContentLength);
                
                // If main branch fails (404), try master branch as fallback
                if (!response.IsSuccessStatusCode && response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    Logger.LogWarning("GitHubDownloader: 'main' branch not found, trying 'master' branch as fallback");
                    
                    // Convert URL from main to master branch
                    var masterUrl = apiUrl.Replace("/archive/refs/heads/main.zip", "/archive/refs/heads/master.zip");
                    Logger.LogInformation("GitHubDownloader: Trying master branch URL: {MasterUrl}", masterUrl);
                    
                    response.Dispose(); // Dispose the failed response
                    response = await _httpClient.GetAsync(masterUrl, cancellationToken);
                    
                    Logger.LogInformation("GitHubDownloader: Master branch response - Status: {StatusCode}, Content-Length: {ContentLength}", 
                        response.StatusCode, response.Content.Headers.ContentLength);
                }
                
                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogError("GitHubDownloader: HTTP request failed - Status: {StatusCode}, Reason: {ReasonPhrase}", 
                        response.StatusCode, response.ReasonPhrase);
                    response.EnsureSuccessStatusCode(); // This will throw with the error details
                }
                
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
            Logger.LogDebug("GitHubDownloader: Converting repository URL to direct download format: {RepoUrl}", repoUrl);
            
            // Remove trailing slash and .git suffix if present
            repoUrl = repoUrl.TrimEnd('/');
            if (repoUrl.EndsWith(".git"))
            {
                repoUrl = repoUrl.Substring(0, repoUrl.Length - 4);
            }
            
            // Extract owner and repo from URL
            var parts = repoUrl.Split('/');
            if (parts.Length < 2)
            {
                Logger.LogError("GitHubDownloader: Invalid repository URL format: {RepoUrl}", repoUrl);
                throw new ArgumentException($"Invalid repository URL format: {repoUrl}");
            }
            
            var owner = parts[^2];
            var repo = parts[^1];
            
            // Use direct download URL pattern: https://github.com/{owner}/{repo}/archive/refs/heads/main.zip
            // This follows the pattern from: https://github.com/notepad-plus-plus/notepad-plus-plus/archive/refs/heads/master.zip
            var directUrl = $"https://github.com/{owner}/{repo}/archive/refs/heads/main.zip";
            
            Logger.LogDebug("GitHubDownloader: Extracted owner: {Owner}, repo: {Repo}, Direct URL: {DirectUrl}", 
                owner, repo, directUrl);
            
            return directUrl;
        }
        
        private string ExtractRepoName(string repoUrl)
        {
            var repoName = repoUrl.TrimEnd('/').Split('/').Last();
            Logger.LogDebug("GitHubDownloader: Extracted repository name: {RepoName} from URL: {RepoUrl}", repoName, repoUrl);
            return repoName;
        }

        /// <summary>
        /// Implement alternative download methods for GitHub repositories
        /// </summary>
        protected override async Task<ToolResult> RunAlternativeMethodAsync(ToolContext context, string methodName, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            Logger.LogInformation("GitHubDownloader: Attempting alternative method: {Method}", methodName);
            
            try
            {
                EnsureSessionScopeInitialized(context);
                
                if (!context.Parameters.TryGetValue("repoUrl", out var repoUrlObj))
                {
                    return CreateResult(false, errorMessage: "Repository URL is required", startTime: startTime);
                }

                var repoUrl = repoUrlObj.ToString()!;
                var (owner, repo) = ExtractOwnerAndRepo(repoUrl);
                
                return methodName switch
                {
                    "direct_main" => await DownloadViaDirectUrl(context, owner, repo, "main", startTime, cancellationToken),
                    "direct_master" => await DownloadViaDirectUrl(context, owner, repo, "master", startTime, cancellationToken),
                    "api_authenticated" => await DownloadViaApiWithAuth(context, owner, repo, startTime, cancellationToken),
                    "git_clone_fallback" => await DownloadViaGitCommand(context, repoUrl, startTime, cancellationToken),
                    _ => CreateResult(false, errorMessage: $"Unknown alternative method: {methodName}", startTime: startTime)
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GitHubDownloader: Alternative method {Method} failed: {Error}", methodName, ex.Message);
                return CreateResult(false, errorMessage: $"Alternative method {methodName} failed: {ex.Message}", startTime: startTime);
            }
        }
        
        /// <summary>
        /// Download using direct GitHub archive URL (github.com/{owner}/{repo}/archive/refs/heads/{branch}.zip)
        /// </summary>
        private async Task<ToolResult> DownloadViaDirectUrl(ToolContext context, string owner, string repo, string branch, DateTime startTime, CancellationToken cancellationToken)
        {
            var directUrl = $"https://github.com/{owner}/{repo}/archive/refs/heads/{branch}.zip";
            Logger.LogInformation("GitHubDownloader: Trying direct download URL: {Url}", directUrl);
            
            var safeWorkingDir = SessionScope.FileSystem.GetSafeWorkingDirectory(context.SessionId!);
            System.IO.Directory.CreateDirectory(safeWorkingDir);
            
            var repoName = $"{owner}-{repo}";
            var zipPath = Path.Combine(safeWorkingDir, $"{repoName}-{branch}.zip");
            var extractPath = Path.Combine(safeWorkingDir, $"{repoName}-{branch}");
            
            try
            {
                var response = await _httpClient.GetAsync(directUrl, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogWarning("GitHubDownloader: Direct URL failed with status: {StatusCode}", response.StatusCode);
                    return CreateResult(false, errorMessage: $"Direct download failed: {response.StatusCode} {response.ReasonPhrase}", startTime: startTime);
                }
                
                // Download and extract
                using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs, cancellationToken);
                }
                
                return await ExtractAndFinalize(context, zipPath, extractPath, startTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GitHubDownloader: Direct URL download failed");
                return CreateResult(false, errorMessage: $"Direct download failed: {ex.Message}", startTime: startTime);
            }
        }
        
        /// <summary>
        /// Download using GitHub API with authentication (if available)
        /// </summary>
        private async Task<ToolResult> DownloadViaApiWithAuth(ToolContext context, string owner, string repo, DateTime startTime, CancellationToken cancellationToken)
        {
            // Check for GitHub token in environment or parameters
            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (string.IsNullOrEmpty(token) && context.Parameters.TryGetValue("github_token", out var tokenObj))
            {
                token = tokenObj?.ToString();
            }
            
            if (string.IsNullOrEmpty(token))
            {
                Logger.LogWarning("GitHubDownloader: No GitHub token available for authenticated API access");
                return CreateResult(false, errorMessage: "No GitHub authentication token available", startTime: startTime);
            }
            
            var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/zipball";
            Logger.LogInformation("GitHubDownloader: Trying authenticated API download: {Url}", apiUrl);
            
            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Add("Authorization", $"token {token}");
            request.Headers.Add("User-Agent", "ollama-agent-suite");
            
            var safeWorkingDir = SessionScope.FileSystem.GetSafeWorkingDirectory(context.SessionId!);
            System.IO.Directory.CreateDirectory(safeWorkingDir);
            
            var repoName = $"{owner}-{repo}";
            var zipPath = Path.Combine(safeWorkingDir, $"{repoName}-api.zip");
            var extractPath = Path.Combine(safeWorkingDir, $"{repoName}-api");
            
            try
            {
                var response = await _httpClient.SendAsync(request, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    Logger.LogWarning("GitHubDownloader: Authenticated API failed with status: {StatusCode}", response.StatusCode);
                    return CreateResult(false, errorMessage: $"Authenticated API download failed: {response.StatusCode} {response.ReasonPhrase}", startTime: startTime);
                }
                
                using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs, cancellationToken);
                }
                
                return await ExtractAndFinalize(context, zipPath, extractPath, startTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GitHubDownloader: Authenticated API download failed");
                return CreateResult(false, errorMessage: $"Authenticated API download failed: {ex.Message}", startTime: startTime);
            }
        }
        
        /// <summary>
        /// Fallback to git clone command if available
        /// </summary>
        private async Task<ToolResult> DownloadViaGitCommand(ToolContext context, string repoUrl, DateTime startTime, CancellationToken cancellationToken)
        {
            Logger.LogInformation("GitHubDownloader: Trying git clone fallback for: {RepoUrl}", repoUrl);
            
            var safeWorkingDir = SessionScope.FileSystem.GetSafeWorkingDirectory(context.SessionId!);
            System.IO.Directory.CreateDirectory(safeWorkingDir);
            
            var repoName = ExtractRepoName(repoUrl);
            var clonePath = Path.Combine(safeWorkingDir, $"{repoName}-git");
            
            try
            {
                // Remove existing directory if it exists
                if (System.IO.Directory.Exists(clonePath))
                {
                    System.IO.Directory.Delete(clonePath, true);
                }
                
                // Try to execute git clone
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"clone --depth 1 \"{repoUrl}\" \"{clonePath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = safeWorkingDir
                    }
                };
                
                Logger.LogDebug("GitHubDownloader: Executing git command: git clone --depth 1 \"{RepoUrl}\" \"{ClonePath}\"", repoUrl, clonePath);
                
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync(cancellationToken);
                
                if (process.ExitCode != 0)
                {
                    Logger.LogWarning("GitHubDownloader: Git clone failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
                    return CreateResult(false, errorMessage: $"Git clone failed: {error}", startTime: startTime);
                }
                
                if (System.IO.Directory.Exists(clonePath))
                {
                    context.State["repoPath"] = clonePath;
                    Logger.LogInformation("GitHubDownloader: Git clone successful to: {ClonePath}", clonePath);
                    
                    return new ToolResult
                    {
                        Success = true,
                        Output = new { RepositoryPath = clonePath, Method = "git_clone" },
                        ExecutionTime = DateTime.Now - startTime,
                        MethodUsed = "git_clone_fallback"
                    };
                }
                else
                {
                    return CreateResult(false, errorMessage: "Git clone completed but directory not found", startTime: startTime);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GitHubDownloader: Git clone command failed");
                return CreateResult(false, errorMessage: $"Git clone command failed: {ex.Message}", startTime: startTime);
            }
        }
        
        /// <summary>
        /// Common extraction and finalization logic
        /// </summary>
        private Task<ToolResult> ExtractAndFinalize(ToolContext context, string zipPath, string extractPath, DateTime startTime)
        {
            try
            {
                if (System.IO.Directory.Exists(extractPath))
                {
                    System.IO.Directory.Delete(extractPath, true);
                }
                System.IO.Directory.CreateDirectory(extractPath);
                
                Logger.LogInformation("GitHubDownloader: Extracting ZIP file to: {ExtractPath}", extractPath);
                ZipFile.ExtractToDirectory(zipPath, extractPath, true);
                
                // Find the actual extracted folder
                var extractedFolders = System.IO.Directory.GetDirectories(extractPath);
                var repoPath = extractedFolders.Length > 0 ? extractedFolders[0] : extractPath;
                
                context.State["repoPath"] = repoPath;
                Logger.LogInformation("GitHubDownloader: Repository extracted successfully to: {RepoPath}", repoPath);
                
                return Task.FromResult(new ToolResult
                {
                    Success = true,
                    Output = new { RepositoryPath = repoPath, DownloadedTo = zipPath },
                    ExecutionTime = DateTime.Now - startTime
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "GitHubDownloader: Extraction failed");
                return Task.FromResult(CreateResult(false, errorMessage: $"Extraction failed: {ex.Message}", startTime: startTime));
            }
        }
        
        /// <summary>
        /// Extract owner and repo from GitHub URL
        /// </summary>
        private (string owner, string repo) ExtractOwnerAndRepo(string repoUrl)
        {
            var uri = new Uri(repoUrl);
            var pathParts = uri.AbsolutePath.Trim('/').Split('/');
            
            if (pathParts.Length >= 2)
            {
                return (pathParts[0], pathParts[1]);
            }
            
            throw new ArgumentException($"Invalid GitHub URL format: {repoUrl}");
        }
    }
}
