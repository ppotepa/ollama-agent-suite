using Ollama.Domain.Tools;
using Ollama.Domain.Tools.Attributes;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Tools.File
{
    /// <summary>
    /// File copy tool - equivalent to 'copy' or 'cp' command
    /// Copies files within session boundaries
    /// </summary>
    [ToolDescription(
        "Copies files within session boundaries",
        "Equivalent to 'copy' or 'cp' command. Creates exact duplicates of files with support for overwrite protection and attribute preservation. All operations within session workspace.",
        "File Operations")]
    [ToolUsage(
        "Copy files within the session workspace",
        SecondaryUseCases = new[] { "File duplication", "Backup creation", "File replication", "Template copying" },
        RequiredParameters = new[] { "sourcePath", "destinationPath" },
        OptionalParameters = new[] { "cd", "overwrite", "preserveAttributes" },
        ExampleInvocation = "FileCopy with sourcePath=\"original.txt\" destinationPath=\"copy.txt\"",
        ExpectedOutput = "Successfully copied file to destination",
        RequiresFileSystem = true,
        RequiresNetwork = false,
        SafetyNotes = "All operations within session boundaries",
        PerformanceNotes = "Large files may take time to copy")]
    [ToolCapabilities(
        ToolCapability.FileCopy | ToolCapability.FileRead | ToolCapability.FileWrite | ToolCapability.CursorNavigation,
        FallbackStrategy = "Stream-based copy if direct file copy fails")]
    public class FileCopyTool : AbstractTool
    {
        public override string Name => "FileCopy";
        public override string Description => "Copies files (equivalent to 'copy' or 'cp' command)";
        public override IEnumerable<string> Capabilities => new[] { "file:copy", "file:duplicate", "fs:copy", "fs:cp" };
        public override bool RequiresNetwork => false;
        public override bool RequiresFileSystem => true;

        public FileCopyTool(ISessionScope sessionScope, ILogger<FileCopyTool> logger)
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
            var safeSource = GetSafePath(sourcePath);
            
            return Task.FromResult(System.IO.File.Exists(safeSource));
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for file copy
        }

        public override async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                // Ensure SessionScope is initialized with correct sessionId from context
                EnsureSessionScopeInitialized(context);
                
                // Process cursor navigation first (if any)
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
                var createDirectories = context.Parameters.TryGetValue("createDirectories", out var createDirObj) 
                    ? createDirObj is bool createDir ? createDir : true  // Default to true
                    : true;

                // Get safe paths within session
                var safeSource = GetSafePath(sourcePath);
                var safeDest = GetSafePath(destPath);
                
                if (!System.IO.File.Exists(safeSource))
                {
                    return CreateResult(false, errorMessage: $"Source file not found: {GetRelativePath(safeSource)}", startTime: startTime);
                }

                // Copy file
                var result = await CopyFile(safeSource, safeDest, overwrite, preserveAttributes, createDirectories);
                
                Logger.LogInformation("FileCopy completed from {Source} to {Destination}", sourcePath, destPath);
                
                return CreateSuccessResultWithContext(result, navigationResult, startTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error copying file");
                return CreateResult(false, errorMessage: $"File copy failed: {ex.Message}", startTime: startTime);
            }
        }

        private async Task<string> CopyFile(string sourcePath, string destPath, bool overwrite, bool preserveAttributes, bool createDirectories)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var sourceRelative = GetRelativePath(sourcePath);
                    var destRelative = GetRelativePath(destPath);
                    var sourceInfo = new FileInfo(sourcePath);
                    
                    // Check if destination exists
                    if (System.IO.File.Exists(destPath) && !overwrite)
                    {
                        throw new InvalidOperationException($"Destination file already exists: {destRelative}. Use overwrite=true to replace.");
                    }
                    
                    // Create destination directory if needed
                    if (createDirectories)
                    {
                        var destDir = Path.GetDirectoryName(destPath);
                        if (!string.IsNullOrEmpty(destDir) && !System.IO.Directory.Exists(destDir))
                        {
                            System.IO.Directory.CreateDirectory(destDir);
                        }
                    }
                    
                    // Copy the file
                    System.IO.File.Copy(sourcePath, destPath, overwrite);
                    
                    var destInfo = new FileInfo(destPath);
                    
                    // Preserve attributes if requested
                    if (preserveAttributes)
                    {
                        try
                        {
                            destInfo.Attributes = sourceInfo.Attributes;
                            destInfo.CreationTime = sourceInfo.CreationTime;
                            destInfo.LastWriteTime = sourceInfo.LastWriteTime;
                            destInfo.LastAccessTime = sourceInfo.LastAccessTime;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "Warning: Could not preserve all attributes for {File}", destPath);
                        }
                    }
                    
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"File copied: {sourceRelative} → {destRelative}");
                    sb.AppendLine($"Size: {FormatFileSize(destInfo.Length)}");
                    sb.AppendLine($"Modified: {destInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                    
                    if (preserveAttributes)
                    {
                        sb.AppendLine("Attributes preserved: Yes");
                    }
                    
                    return sb.ToString();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to copy file: {ex.Message}", ex);
                }
            });
        }
    }
}
