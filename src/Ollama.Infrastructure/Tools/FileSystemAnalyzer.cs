using Ollama.Domain.Tools;
using Ollama.Domain.Tools.Attributes;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Ollama.Infrastructure.Tools
{
    [ToolDescription(
        "Analyzes file system structure, file types, and sizes within session boundaries",
        "Comprehensive file system analysis tool that examines directory structures, file distributions, size patterns, and provides statistical insights about the session workspace.",
        "File System Analysis")]
    [ToolUsage(
        "Analyze file system structure and generate detailed reports",
        SecondaryUseCases = new[] { 
            "Directory analysis", 
            "File size reporting", 
            "Storage insights", 
            "File type distribution",
            // Backend Project Analysis
            "Project structure assessment",
            "Code organization analysis",
            "Dependency file detection (.csproj, package.json, requirements.txt)",
            "Configuration file identification (appsettings.json, web.config)",
            "Build artifact analysis (bin/, obj/, dist/)",
            "Source code distribution analysis",
            // Architecture Analysis
            "Clean architecture layer detection",
            "Microservice project structure analysis",
            "Domain-driven design structure assessment",
            "Test project organization analysis",
            "Documentation structure evaluation",
            // Database Project Analysis
            "Migration file detection and analysis",
            "Database script organization",
            "Entity model file distribution",
            "Data seeding file analysis",
            // DevOps Structure Analysis
            "Docker configuration detection (Dockerfile, docker-compose.yml)",
            "CI/CD pipeline file analysis (.github/, .azure/, jenkins/)",
            "Infrastructure as Code file detection (terraform/, pulumi/)",
            "Kubernetes manifest analysis",
            "Deployment script organization",
            // Security & Configuration Analysis
            "Security configuration file detection",
            "Environment configuration analysis",
            "Certificate and key file identification",
            "Logging configuration assessment",
            // Code Quality Analysis
            "Test coverage file analysis",
            "Code quality configuration detection (.editorconfig, .eslintrc)",
            "Static analysis configuration assessment",
            "Code style configuration analysis",
            // Package & Dependency Analysis
            "Package manager file analysis (packages/, node_modules/)",
            "NuGet package structure analysis",
            "Third-party library organization",
            "Framework file distribution",
            // Build & Deployment Analysis
            "Build configuration analysis (MSBuild, webpack, etc.)",
            "Output directory structure assessment",
            "Release artifact organization",
            "Environment-specific build analysis"
        },
        RequiredParameters = new[] { "path" },
        OptionalParameters = new[] { "includeSubdirectories", "maxDepth", "includeHidden" },
        ExampleInvocation = "FileSystemAnalyzer with path=\".\" to analyze current directory",
        ExpectedOutput = "Detailed file system analysis with statistics and insights",
        RequiresFileSystem = true,
        RequiresNetwork = false,
        SafetyNotes = "Read-only analysis within session boundaries",
        PerformanceNotes = "Large directories may take time to analyze")]
    [ToolCapabilities(
        ToolCapability.FileSystemAnalysis | ToolCapability.DirectoryList | ToolCapability.DataAnalysis | 
        ToolCapability.BackendDevelopment | ToolCapability.DatabaseDevelopment | 
        ToolCapability.DevOpsDevelopment | ToolCapability.TestingInfrastructure | 
        ToolCapability.ArchitecturalPatterns,
        FallbackStrategy = "Basic directory listing if advanced analysis fails")]
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

            var allFiles = new List<FileInfo>();

            try
            {
                // Recursively collect all files
                CollectAllFiles(directory, allFiles);

                stats.TotalFiles = allFiles.Count;
                stats.TotalSize = allFiles.Sum(f => f.Length);

                // Group files by extension for file type distribution only
                var filesByExtension = allFiles.GroupBy(f => f.Extension.ToLowerInvariant());
                foreach (var extensionGroup in filesByExtension)
                {
                    var extension = string.IsNullOrEmpty(extensionGroup.Key) ? "(no extension)" : extensionGroup.Key;
                    
                    // Update file type distribution (count ALL files)
                    stats.FileTypeDistribution[extension] = extensionGroup.Count();
                }

                // Populate LargestFiles (top 10 across all extensions)
                var largestFiles = allFiles.OrderByDescending(f => f.Length).Take(10);
                foreach (var file in largestFiles)
                {
                    stats.LargestFiles.Add(new FileSample
                    {
                        Name = file.Name,
                        Path = GetRelativePath(file.FullName),
                        ParentFolder = GetRelativePath(file.Directory?.FullName ?? ""),
                        Extension = file.Extension,
                        Size = file.Length,
                        Preview = $"File in {GetRelativePath(file.Directory?.FullName ?? "")}"
                    });
                }

                // Sample some files for content analysis (text files only)
                var sampleFiles = allFiles
                    .Where(f => IsTextFile(f.Extension))
                    .OrderByDescending(f => f.Length)
                    .Take(3)  // Reduce from 5 to 3 samples
                    .ToList();

                foreach (var file in sampleFiles)
                {
                    try
                    {
                        var content = System.IO.File.ReadAllText(file.FullName);
                        stats.FileSamples.Add(new FileSample
                        {
                            Name = file.Name,
                            Path = GetRelativePath(file.FullName),
                            ParentFolder = GetRelativePath(file.Directory?.FullName ?? ""),
                            Extension = file.Extension,
                            Size = file.Length,
                            Preview = content.Length > 200 ? content.Substring(0, 200) + "..." : content
                        });
                    }
                    catch
                    {
                        // Skip files that can't be read
                    }
                }

                // Count directories and collect subdirectory info
                CountDirectoriesAndSubdirectories(directory, stats);
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

        private void CollectAllFiles(DirectoryInfo directory, List<FileInfo> allFiles)
        {
            try
            {
                // Add files from current directory
                allFiles.AddRange(directory.GetFiles());

                // Recursively collect from subdirectories
                foreach (var subDir in directory.GetDirectories())
                {
                    CollectAllFiles(subDir, allFiles);
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
        }

        private void CountDirectoriesAndSubdirectories(DirectoryInfo directory, FileSystemStats stats)
        {
            try
            {
                foreach (var subDir in directory.GetDirectories())
                {
                    stats.TotalDirectories++;
                    
                    // Collect some subdirectory info (only direct children, limit to 10)
                    if (subDir.Parent?.FullName == directory.FullName && stats.Subdirectories.Count < 10)
                    {
                        var fileCount = CountFilesInDirectory(subDir);
                        stats.Subdirectories.Add(new DirectorySummary
                        {
                            Name = subDir.Name,
                            Path = GetRelativePath(subDir.FullName),
                            FileCount = fileCount
                        });
                    }

                    // Recursively count subdirectories
                    CountDirectoriesAndSubdirectories(subDir, stats);
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
        }

        private int CountFilesInDirectory(DirectoryInfo directory)
        {
            try
            {
                return directory.GetFiles("*", SearchOption.AllDirectories).Length;
            }
            catch
            {
                return 0;
            }
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
                var largestFiles = allFiles.OrderByDescending(f => f.Length).Take(10);
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
                    if (stats.LargestFiles.Count < 15)
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
        // Removed FilesByExtension to reduce data size - LargestFiles provides the essential info
        public string AnalysisMethod { get; set; } = "standard";
    }

    public class FileSample
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string ParentFolder { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Preview { get; set; } = string.Empty;
    }

    public class FileDetail
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string ParentFolder { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class DirectorySummary
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public int FileCount { get; set; }
    }
}
