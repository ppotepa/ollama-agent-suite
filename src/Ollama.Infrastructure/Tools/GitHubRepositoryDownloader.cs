using Ollama.Domain.Tools;
using System.IO;
using System.Net.Http;
using System.IO.Compression;

namespace Ollama.Infrastructure.Tools
{
    public class GitHubRepositoryDownloader : ITool
    {
        private readonly HttpClient _httpClient;

        public GitHubRepositoryDownloader(HttpClient httpClient)
        {
            _httpClient = httpClient;
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
            
            if (!context.Parameters.TryGetValue("repoUrl", out var repoUrlObj))
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = "Repository URL is required",
                    ExecutionTime = DateTime.Now - startTime
                };
            }

            var repoUrl = repoUrlObj.ToString();
            
            // Convert GitHub URL to API format for download
            var apiUrl = ConvertToGitHubApiUrl(repoUrl!);
            
            try
            {
                // Create a directory in cache if it doesn't exist
                var cacheDir = Path.Combine(context.WorkingDirectory ?? Path.GetTempPath(), "cache");
                Directory.CreateDirectory(cacheDir);
                
                var repoName = ExtractRepoName(repoUrl!);
                var zipPath = Path.Combine(cacheDir, $"{repoName}.zip");
                var extractPath = Path.Combine(cacheDir, repoName);
                
                // Download the repository
                var response = await _httpClient.GetAsync(apiUrl, cancellationToken);
                response.EnsureSuccessStatusCode();
                
                using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs, cancellationToken);
                }
                
                // Extract the ZIP
                if (Directory.Exists(extractPath))
                {
                    Directory.Delete(extractPath, true);
                }
                Directory.CreateDirectory(extractPath);
                ZipFile.ExtractToDirectory(zipPath, extractPath, true);
                
                // Find the actual extracted folder (GitHub creates a folder with commit hash)
                var extractedFolders = Directory.GetDirectories(extractPath);
                if (extractedFolders.Length > 0)
                {
                    context.State["repoPath"] = extractedFolders[0];
                }
                else
                {
                    context.State["repoPath"] = extractPath;
                }
                
                return new ToolResult
                {
                    Success = true,
                    Output = new
                    {
                        RepositoryPath = context.State["repoPath"],
                        DownloadedTo = zipPath
                    },
                    ExecutionTime = DateTime.Now - startTime
                };
            }
            catch (Exception ex)
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to download repository: {ex.Message}",
                    ExecutionTime = DateTime.Now - startTime
                };
            }
        }

        private string ConvertToGitHubApiUrl(string repoUrl)
        {
            // Remove trailing slash if present
            repoUrl = repoUrl.TrimEnd('/');
            
            // Extract owner and repo from URL
            var parts = repoUrl.Split('/');
            var owner = parts[^2];
            var repo = parts[^1];
            
            return $"https://api.github.com/repos/{owner}/{repo}/zipball";
        }
        
        private string ExtractRepoName(string repoUrl)
        {
            return repoUrl.TrimEnd('/').Split('/').Last();
        }
    }
}
