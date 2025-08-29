using Ollama.Domain.Tools;
using Ollama.Domain.Tools.Attributes;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Ollama.Infrastructure.Tools.Directory
{
    /// <summary>
    /// Directory copy tool - equivalent to 'xcopy' or 'robocopy' command
    /// Copies directories and their contents within session boundaries
    /// </summary>
    [ToolDescription(
        "Copies directories and their contents within session boundaries",
        "Equivalent to 'xcopy' or 'robocopy' command. Recursively copies directory structures with all files and subdirectories. Maintains file attributes and timestamps.",
        "Directory Operations")]
    [ToolUsage(
        "Copy directory structures with all contents",
        SecondaryUseCases = new[] { "Directory backup", "Structure duplication", "Content migration", "Recursive copying" },
        RequiredParameters = new[] { "sourcePath", "destinationPath" },
        OptionalParameters = new[] { "cd", "recursive", "preserveAttributes" },
        ExampleInvocation = "DirectoryCopy with sourcePath=\"source\" destinationPath=\"backup\"",
        ExpectedOutput = "Successfully copied directory with all contents",
        RequiresFileSystem = true,
        RequiresNetwork = false,
        SafetyNotes = "All operations within session boundaries - cannot escape sandbox",
        PerformanceNotes = "Large directories may take time to copy completely")]
    [ToolCapabilities(
        ToolCapability.DirectoryCopy | ToolCapability.FileCopy | ToolCapability.CursorNavigation,
        FallbackStrategy = "File-by-file copy if bulk operations fail")]
    public class DirectoryCopyTool : AbstractTool
    {
        public override string Name => "DirectoryCopy";
        public override string Description => "Copies directories and their contents (equivalent to 'xcopy' or 'robocopy' command)";
        public override IEnumerable<string> Capabilities => new[] { "dir:copy", "directory:duplicate", "fs:xcopy" };
        public override bool RequiresNetwork => false;
        public override bool RequiresFileSystem => true;

        public DirectoryCopyTool(ISessionScope sessionScope, ILogger<DirectoryCopyTool> logger)
            : base(sessionScope, logger)
        {
        }

        public override Task<bool> DryRunAsync(ToolContext context)
        {
            if (!context.Parameters.TryGetValue("source", out var sourceObj) || 
                !context.Parameters.TryGetValue("destination", out var destObj) ||
                string.IsNullOrWhiteSpace(sourceObj?.ToString()) || 
                string.IsNullOrWhiteSpace(destObj?.ToString()))
            {
                return Task.FromResult(false);
            }

            var sourcePath = sourceObj.ToString()!;
            var safeSource = SessionScope.GetSafePath(sourcePath);
            
            return Task.FromResult(System.IO.Directory.Exists(safeSource));
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for directory copy
        }

        public override async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                EnsureSessionScopeInitialized(context);
                
                var navigationResult = ProcessCursorNavigation(context);
                
                if (!context.Parameters.TryGetValue("source", out var sourceObj) || 
                    !context.Parameters.TryGetValue("destination", out var destObj) ||
                    string.IsNullOrWhiteSpace(sourceObj?.ToString()) || 
                    string.IsNullOrWhiteSpace(destObj?.ToString()))
                {
                    return CreateResult(false, errorMessage: "Both 'source' and 'destination' parameters are required", startTime: startTime);
                }

                var sourcePath = sourceObj.ToString()!;
                var destPath = destObj.ToString()!;
                
                // Get options
                var overwrite = context.Parameters.TryGetValue("overwrite", out var overwriteObj) 
                    && overwriteObj is bool ow && ow;
                var preserveAttributes = context.Parameters.TryGetValue("preserveAttributes", out var attrObj) 
                    && attrObj is bool attr && attr;
                var copySubdirectories = context.Parameters.TryGetValue("copySubdirectories", out var subObj) 
                    ? subObj is bool sub ? sub : true  // Default to true
                    : true;

                // Get safe paths within session
                var safeSource = SessionScope.GetSafePath(sourcePath);
                var safeDest = SessionScope.GetSafePath(destPath);
                
                if (!System.IO.Directory.Exists(safeSource))
                {
                    return CreateResult(false, errorMessage: $"Source directory not found: {GetRelativePath(safeSource)}", startTime: startTime);
                }

                // Copy directory
                var result = await CopyDirectory(safeSource, safeDest, overwrite, preserveAttributes, copySubdirectories, cancellationToken);
                
                Logger.LogInformation("DirectoryCopy completed from {Source} to {Destination}", sourcePath, destPath);
                
                return CreateSuccessResultWithContext(result, navigationResult, startTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error copying directory");
                return CreateResult(false, errorMessage: $"Directory copy failed: {ex.Message}", startTime: startTime);
            }
        }

        private async Task<string> CopyDirectory(string sourcePath, string destPath, bool overwrite, bool preserveAttributes, bool copySubdirectories, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var sourceRelative = GetRelativePath(sourcePath);
                    var destRelative = GetRelativePath(destPath);
                    var stats = new CopyStats();
                    
                    // Check if destination exists
                    if (System.IO.Directory.Exists(destPath) && !overwrite)
                    {
                        throw new InvalidOperationException($"Destination directory already exists: {destRelative}. Use overwrite=true to replace contents.");
                    }
                    
                    // Create destination directory if it doesn't exist
                    if (!System.IO.Directory.Exists(destPath))
                    {
                        System.IO.Directory.CreateDirectory(destPath);
                        stats.DirectoriesCreated++;
                    }
                    
                    // Copy directory contents
                    CopyDirectoryRecursive(sourcePath, destPath, overwrite, preserveAttributes, copySubdirectories, stats, cancellationToken);
                    
                    var sb = new StringBuilder();
                    sb.AppendLine($"Directory copied: {sourceRelative} → {destRelative}");
                    sb.AppendLine($"Files copied: {stats.FilesCopied}");
                    sb.AppendLine($"Directories created: {stats.DirectoriesCreated}");
                    if (stats.FilesSkipped > 0)
                    {
                        sb.AppendLine($"Files skipped: {stats.FilesSkipped}");
                    }
                    sb.AppendLine($"Total size: {FormatFileSize(stats.TotalBytes)}");
                    
                    return sb.ToString();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to copy directory: {ex.Message}", ex);
                }
            }, cancellationToken);
        }

        private void CopyDirectoryRecursive(string sourcePath, string destPath, bool overwrite, bool preserveAttributes, bool copySubdirectories, CopyStats stats, CancellationToken cancellationToken)
        {
            var sourceDir = new DirectoryInfo(sourcePath);
            
            // Copy files
            foreach (var file in sourceDir.GetFiles())
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var destFilePath = Path.Combine(destPath, file.Name);
                
                if (System.IO.File.Exists(destFilePath) && !overwrite)
                {
                    stats.FilesSkipped++;
                    continue;
                }
                
                file.CopyTo(destFilePath, overwrite);
                stats.FilesCopied++;
                stats.TotalBytes += file.Length;
                
                if (preserveAttributes)
                {
                    try
                    {
                        var destFile = new FileInfo(destFilePath);
                        destFile.Attributes = file.Attributes;
                        destFile.CreationTime = file.CreationTime;
                        destFile.LastWriteTime = file.LastWriteTime;
                        destFile.LastAccessTime = file.LastAccessTime;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Warning: Could not preserve attributes for {File}", destFilePath);
                    }
                }
            }
            
            // Copy subdirectories if requested
            if (copySubdirectories)
            {
                foreach (var subDir in sourceDir.GetDirectories())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var destSubPath = Path.Combine(destPath, subDir.Name);
                    
                    if (!System.IO.Directory.Exists(destSubPath))
                    {
                        System.IO.Directory.CreateDirectory(destSubPath);
                        stats.DirectoriesCreated++;
                        
                        if (preserveAttributes)
                        {
                            try
                            {
                                var destDirInfo = new DirectoryInfo(destSubPath);
                                destDirInfo.Attributes = subDir.Attributes;
                                destDirInfo.CreationTime = subDir.CreationTime;
                                destDirInfo.LastWriteTime = subDir.LastWriteTime;
                                destDirInfo.LastAccessTime = subDir.LastAccessTime;
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning(ex, "Warning: Could not preserve attributes for {Directory}", destSubPath);
                            }
                        }
                    }
                    
                    // Recursively copy subdirectory
                    CopyDirectoryRecursive(subDir.FullName, destSubPath, overwrite, preserveAttributes, true, stats, cancellationToken);
                }
            }
        }

        private class CopyStats
        {
            public int FilesCopied { get; set; }
            public int FilesSkipped { get; set; }
            public int DirectoriesCreated { get; set; }
            public long TotalBytes { get; set; }
        }
    }
}
