using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Ollama.Infrastructure.Tools
{
    public class FileSystemAnalyzer : AbstractTool
    {
        public FileSystemAnalyzer(ISessionScope sessionScope, ILogger<FileSystemAnalyzer> logger) : base(sessionScope, logger)
        {
        }
        
        public override string Name => "FileSystemAnalyzer";
        public override string Description => "Analyzes file system structure, file types, and sizes";
        public override IEnumerable<string> Capabilities => new[] { "fs:analyze", "repo:structure" };
        public override bool RequiresNetwork => false;
        public override bool RequiresFileSystem => true;

        public override Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(context.State.ContainsKey("repoPath") || context.Parameters.ContainsKey("path"));
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m);
        }

        public override IEnumerable<string> GetAlternativeMethods()
        {
            return new[] { "directory_info_only", "file_enumeration_only", "recursive_scan", "external_dir_command" };
        }

        protected override async Task<ToolResult> RunAlternativeMethodAsync(ToolContext context, string methodName, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                EnsureSessionScopeInitialized(context);
                
                if (string.IsNullOrEmpty(context.SessionId))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = "Session ID is required for file system analysis",
                        ExecutionTime = DateTime.Now - startTime,
                        MethodUsed = methodName
                    };
                }
                
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
                    path = SessionScope.WorkingDirectory;
                }

                var safePath = GetSafePath(path ?? ".");
                var directoryInfo = new DirectoryInfo(safePath);
                
                if (!directoryInfo.Exists)
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = $"Directory does not exist: {GetRelativePath(safePath)}",
                        ExecutionTime = DateTime.Now - startTime,
                        MethodUsed = methodName
                    };
                }

                FileSystemStats fileStats = methodName switch
                {
                    "directory_info_only" => await AnalyzeDirectoryInfoOnly(directoryInfo, cancellationToken),
                    "file_enumeration_only" => await AnalyzeFileEnumerationOnly(directoryInfo, cancellationToken),
                    "recursive_scan" => await AnalyzeRecursiveScan(directoryInfo, cancellationToken),
                    "external_dir_command" => await AnalyzeWithExternalCommand(directoryInfo, cancellationToken),
                    _ => throw new NotSupportedException($"Alternative method '{methodName}' is not supported")
                };

                context.State["fileStats"] = fileStats;
                Logger.LogInformation("FileSystemAnalyzer completed using alternative method {Method} for path: {Path}", methodName, GetRelativePath(safePath));

                return new ToolResult
                {
                    Success = true,
                    Output = fileStats,
                    ExecutionTime = DateTime.Now - startTime,
                    MethodUsed = methodName
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error analyzing directory with alternative method {Method}", methodName);
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Error analyzing directory with method {methodName}: {ex.Message}",
                    ExecutionTime = DateTime.Now - startTime,
                    MethodUsed = methodName
                };
            }
        }

        public override async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                // Ensure SessionScope is initialized with correct sessionId from context
                EnsureSessionScopeInitialized(context);
                
                Logger.LogInformation("FileSystemAnalyzer: Starting file system analysis");
                
                // Validate session context
                if (string.IsNullOrEmpty(context.SessionId))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = "Session ID is required for file system analysis",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }
                
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
                    // Use session working directory if no path provided
                    path = SessionScope.WorkingDirectory;
                }

                // Get session-safe path
                var safePath = GetSafePath(path ?? ".");

                var directoryInfo = new DirectoryInfo(safePath);
                if (!directoryInfo.Exists)
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = $"Directory does not exist: {GetRelativePath(safePath)}",
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
                Logger.LogError(ex, "FileSystemAnalyzer: Error analyzing directory");
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
                        var content = System.IO.File.ReadAllText(file.FullName);
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

        #region Alternative Analysis Methods

        /// <summary>
        /// Alternative method 1: Basic directory info analysis (minimal I/O)
        /// </summary>
        private async Task<FileSystemStats> AnalyzeDirectoryInfoOnly(DirectoryInfo directory, CancellationToken cancellationToken)
        {
            var stats = new FileSystemStats
            {
                TotalDirectories = 1,
                PathName = GetRelativePath(directory.FullName)
            };

            try
            {
                // Quick enumeration without detailed file analysis
                var fileEntries = await Task.Run(() => directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly).ToList(), cancellationToken);
                var dirEntries = await Task.Run(() => directory.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).ToList(), cancellationToken);

                stats.TotalFiles = fileEntries.Count;
                stats.TotalDirectories += dirEntries.Count;
                stats.TotalSize = fileEntries.Sum(f => f.Length);

                // Quick largest files (top 10)
                var largestFiles = fileEntries.OrderByDescending(f => f.Length).Take(10);
                foreach (var file in largestFiles)
                {
                    stats.LargestFiles.Add(new FileSample
                    {
                        Name = file.Name,
                        Path = GetRelativePath(file.FullName),
                        Extension = file.Extension,
                        Size = file.Length,
                        Preview = "Directory info analysis - preview not available"
                    });
                }

                stats.AnalysisMethod = "directory_info_only";
                return stats;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Directory info analysis failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Alternative method 2: File enumeration without full analysis
        /// </summary>
        private async Task<FileSystemStats> AnalyzeFileEnumerationOnly(DirectoryInfo directory, CancellationToken cancellationToken)
        {
            var stats = new FileSystemStats
            {
                TotalDirectories = 1,
                PathName = GetRelativePath(directory.FullName)
            };

            try
            {
                // Enumerate all files without subdirectory recursion
                var allFiles = await Task.Run(() => directory.GetFiles("*", SearchOption.AllDirectories), cancellationToken);
                var allDirs = await Task.Run(() => directory.GetDirectories("*", SearchOption.AllDirectories), cancellationToken);

                stats.TotalFiles = allFiles.Length;
                stats.TotalDirectories += allDirs.Length;
                stats.TotalSize = allFiles.Sum(f => f.Length);

                // Group by extension
                var extensionGroups = allFiles.GroupBy(f => f.Extension.ToLower())
                    .OrderByDescending(g => g.Sum(f => f.Length))
                    .Take(20);

                foreach (var group in extensionGroups)
                {
                    stats.ExtensionStats[group.Key] = group.Sum(f => f.Length);
                }

                // Get largest files
                var largestFiles = allFiles.OrderByDescending(f => f.Length).Take(20);
                foreach (var file in largestFiles)
                {
                    stats.LargestFiles.Add(new FileSample
                    {
                        Name = file.Name,
                        Path = GetRelativePath(file.FullName),
                        Extension = file.Extension,
                        Size = file.Length,
                        Preview = "File enumeration analysis - preview not available"
                    });
                }

                stats.AnalysisMethod = "file_enumeration_only";
                return stats;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"File enumeration analysis failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Alternative method 3: Recursive scan with basic analysis
        /// </summary>
        private async Task<FileSystemStats> AnalyzeRecursiveScan(DirectoryInfo directory, CancellationToken cancellationToken)
        {
            var stats = new FileSystemStats
            {
                TotalDirectories = 1,
                PathName = GetRelativePath(directory.FullName)
            };

            try
            {
                await Task.Run(() => RecursiveDirectoryScan(directory, stats), cancellationToken);
                stats.AnalysisMethod = "recursive_scan";
                return stats;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Recursive scan analysis failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Alternative method 4: Use external directory command (dir/ls) for analysis
        /// </summary>
        private async Task<FileSystemStats> AnalyzeWithExternalCommand(DirectoryInfo directory, CancellationToken cancellationToken)
        {
            var stats = new FileSystemStats
            {
                TotalDirectories = 1,
                PathName = GetRelativePath(directory.FullName)
            };

            try
            {
                // Use dir command to get file listing
                var process = new System.Diagnostics.Process();
                var outputBuilder = new System.Text.StringBuilder();

                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c dir \"{directory.FullName}\" /s /a /-c",
                    WorkingDirectory = directory.FullName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.OutputDataReceived += (sender, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                await process.WaitForExitAsync(combinedCts.Token);

                if (process.ExitCode == 0)
                {
                    // Parse dir output for basic statistics
                    var output = outputBuilder.ToString();
                    var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    
                    // Count files and directories from dir output
                    var fileCount = 0;
                    var dirCount = 0;
                    long totalSize = 0;

                    foreach (var line in lines)
                    {
                        if (line.Contains("<DIR>"))
                        {
                            dirCount++;
                        }
                        else if (line.Trim().Length > 0 && char.IsDigit(line.Trim()[0]))
                        {
                            fileCount++;
                            // Try to parse file size from dir output
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 2 && long.TryParse(parts[2].Replace(",", ""), out var size))
                            {
                                totalSize += size;
                            }
                        }
                    }

                    stats.TotalFiles = fileCount;
                    stats.TotalDirectories = dirCount;
                    stats.TotalSize = totalSize;
                    stats.AnalysisMethod = "external_dir_command";
                }
                else
                {
                    // Fallback to basic file enumeration if dir command fails
                    return await AnalyzeFileEnumerationOnly(directory, cancellationToken);
                }

                return stats;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"External command analysis failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Recursive helper for directory scanning
        /// </summary>
        private void RecursiveDirectoryScan(DirectoryInfo directory, FileSystemStats stats)
        {
            try
            {
                var files = directory.GetFiles();
                stats.TotalFiles += files.Length;
                
                foreach (var file in files)
                {
                    stats.TotalSize += file.Length;
                    
                    // Track largest files
                    if (stats.LargestFiles.Count < 50)
                    {
                        stats.LargestFiles.Add(new FileSample
                        {
                            Name = file.Name,
                            Path = GetRelativePath(file.FullName),
                            Extension = file.Extension,
                            Size = file.Length,
                            Preview = "Recursive scan - preview not available"
                        });
                    }
                    else
                    {
                        // Replace smaller files
                        var smallestLarge = stats.LargestFiles.OrderBy(f => f.Size).First();
                        if (file.Length > smallestLarge.Size)
                        {
                            stats.LargestFiles.Remove(smallestLarge);
                            stats.LargestFiles.Add(new FileSample
                            {
                                Name = file.Name,
                                Path = GetRelativePath(file.FullName),
                                Extension = file.Extension,
                                Size = file.Length,
                                Preview = "Recursive scan - preview not available"
                            });
                        }
                    }
                }

                var subdirectories = directory.GetDirectories();
                stats.TotalDirectories += subdirectories.Length;

                foreach (var subdir in subdirectories)
                {
                    RecursiveDirectoryScan(subdir, stats);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip directories we can't access
            }
            catch (Exception)
            {
                // Skip problematic directories
            }
        }

        #endregion

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
        public Dictionary<string, long> ExtensionStats { get; set; } = new Dictionary<string, long>();
        public List<FileSample> FileSamples { get; set; } = new List<FileSample>();
        public List<FileSample> LargestFiles { get; set; } = new List<FileSample>();
        public List<DirectorySummary> Subdirectories { get; set; } = new List<DirectorySummary>();
        public string AnalysisMethod { get; set; } = "standard";
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
