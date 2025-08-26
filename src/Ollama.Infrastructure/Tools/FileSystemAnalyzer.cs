using Ollama.Domain.Tools;
using System.IO;

namespace Ollama.Infrastructure.Tools
{
    public class FileSystemAnalyzer : ITool
    {
        public string Name => "FileSystemAnalyzer";
        public string Description => "Analyzes file system structure, file types, and sizes";
        public IEnumerable<string> Capabilities => new[] { "fs:analyze", "repo:structure" };
        public bool RequiresNetwork => false;
        public bool RequiresFileSystem => true;

        public Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(context.State.ContainsKey("repoPath") || context.Parameters.ContainsKey("path"));
        }

        public Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m);
        }

        public async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            string? path;
            if (context.State.TryGetValue("repoPath", out var repoPathObj))
            {
                path = repoPathObj?.ToString();
            }
            else if (context.Parameters.TryGetValue("path", out var pathObj))
            {
                path = pathObj?.ToString();
            }
            else
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = "No path provided for analysis",
                    ExecutionTime = DateTime.Now - startTime
                };
            }

            try
            {
                var directoryInfo = new DirectoryInfo(path!);
                if (!directoryInfo.Exists)
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = $"Directory does not exist: {path}",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }

                var fileStats = await Task.Run(() => AnalyzeDirectory(directoryInfo), cancellationToken);

                // Save stats in state for other tools
                context.State["fileStats"] = fileStats;

                return new ToolResult
                {
                    Success = true,
                    Output = fileStats,
                    ExecutionTime = DateTime.Now - startTime
                };
            }
            catch (Exception ex)
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Error analyzing directory: {ex.Message}",
                    ExecutionTime = DateTime.Now - startTime
                };
            }
        }

        private FileSystemStats AnalyzeDirectory(DirectoryInfo directory)
        {
            var stats = new FileSystemStats
            {
                TotalDirectories = 1, // Count the current directory
                PathName = directory.FullName
            };

            try
            {
                // Get all files in the current directory
                var files = directory.GetFiles();
                stats.TotalFiles += files.Length;
                stats.TotalSize += files.Sum(f => f.Length);

                // Group files by extension
                var filesByExtension = files.GroupBy(f => f.Extension.ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.Count());

                foreach (var ext in filesByExtension)
                {
                    stats.FileTypeDistribution[ext.Key] = ext.Value;
                }

                // Sample some files for content analysis
                var sampleFiles = files
                    .Where(f => IsTextFile(f.Extension))
                    .OrderByDescending(f => f.Length)
                    .Take(5)
                    .ToList();

                foreach (var file in sampleFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(file.FullName);
                        stats.FileSamples.Add(new FileSample
                        {
                            Name = file.Name,
                            Path = file.FullName,
                            Extension = file.Extension,
                            Size = file.Length,
                            Preview = content.Length > 1000 ? content.Substring(0, 1000) + "..." : content
                        });
                    }
                    catch
                    {
                        // Skip files that can't be read
                    }
                }

                // Recursively analyze subdirectories
                foreach (var subDir in directory.GetDirectories())
                {
                    var subStats = AnalyzeDirectory(subDir);
                    stats.TotalDirectories += subStats.TotalDirectories;
                    stats.TotalFiles += subStats.TotalFiles;
                    stats.TotalSize += subStats.TotalSize;

                    // Merge file type distributions
                    foreach (var typeCount in subStats.FileTypeDistribution)
                    {
                        if (stats.FileTypeDistribution.ContainsKey(typeCount.Key))
                        {
                            stats.FileTypeDistribution[typeCount.Key] += typeCount.Value;
                        }
                        else
                        {
                            stats.FileTypeDistribution[typeCount.Key] = typeCount.Value;
                        }
                    }

                    // Collect some subdirectory info
                    stats.Subdirectories.Add(new DirectorySummary
                    {
                        Name = subDir.Name,
                        Path = subDir.FullName,
                        FileCount = subStats.TotalFiles
                    });
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we can't access
            }
            catch (Exception)
            {
                // Skip any problematic directories
            }

            return stats;
        }

        private bool IsTextFile(string extension)
        {
            var textExtensions = new[]
            {
                ".txt", ".md", ".cs", ".js", ".ts", ".html", ".css", ".json", ".xml",
                ".yml", ".yaml", ".config", ".csproj", ".sln", ".py", ".java", ".c", ".cpp",
                ".h", ".hpp", ".sh", ".bat", ".ps1", ".sql", ".gitignore", ".env",
                ".dockerfile", ".jsx", ".tsx", ".scss", ".less", ".go", ".rb", ".php"
            };
            
            return textExtensions.Contains(extension.ToLowerInvariant());
        }
    }

    public class FileSystemStats
    {
        public string PathName { get; set; } = string.Empty;
        public int TotalDirectories { get; set; }
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public Dictionary<string, int> FileTypeDistribution { get; set; } = new Dictionary<string, int>();
        public List<FileSample> FileSamples { get; set; } = new List<FileSample>();
        public List<DirectorySummary> Subdirectories { get; set; } = new List<DirectorySummary>();
    }

    public class FileSample
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Preview { get; set; } = string.Empty;
    }

    public class DirectorySummary
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int FileCount { get; set; }
    }
}
