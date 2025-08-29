using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Tools.File
{
    /// <summary>
    /// File move tool - equivalent to 'move' or 'mv' command
    /// Moves or renames files within session boundaries
    /// </summary>
    public class FileMoveTool : AbstractTool
    {
        public FileMoveTool(ISessionScope sessionScope, ILogger<FileMoveTool> logger)
            : base(sessionScope, logger)
        {
        }

        public override string Name => "FileMove";
        public override string Description => "Moves or renames files (equivalent to 'move' or 'mv' command)";
        public override IEnumerable<string> Capabilities => new[] { "file:move", "file:rename", "fs:move", "fs:mv" };
        public override bool RequiresNetwork => false;
        public override bool RequiresFileSystem => true;

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
            var destPath = destObj.ToString()!;
            
            var safeSource = GetSafePath(sourcePath);
            var safeDest = GetSafePath(destPath);
            
            return Task.FromResult(System.IO.File.Exists(safeSource) && !System.IO.File.Exists(safeDest));
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for file move
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
                var createDirectories = context.Parameters.TryGetValue("createDirectories", out var createDirObj) 
                    ? createDirObj is bool createDir ? createDir : true  // Default to true
                    : true;

                // Get safe paths within session
                var safeSource = GetSafePath(sourcePath);
                var safeDest = GetSafePath(destPath);
                
                if (!System.IO.File.Exists(safeSource))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = $"Source file not found: {GetRelativePath(safeSource)}",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }

                // Move file
                var result = await MoveFile(safeSource, safeDest, overwrite, createDirectories);
                
                Logger.LogInformation("FileMove completed from {Source} to {Destination}", sourcePath, destPath);
                
                return CreateSuccessResultWithContext(result, navigationResult, startTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error moving file");
                return CreateResult(false, errorMessage: $"File move failed: {ex.Message}", startTime: startTime);
            }
        }

        private async Task<string> MoveFile(string sourcePath, string destPath, bool overwrite, bool createDirectories)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var sourceRelative = GetRelativePath(sourcePath);
                    var destRelative = GetRelativePath(destPath);
                    var sourceInfo = new FileInfo(sourcePath);
                    
                    // Check if destination exists
                    if (System.IO.File.Exists(destPath))
                    {
                        if (!overwrite)
                        {
                            throw new InvalidOperationException($"Destination file already exists: {destRelative}. Use overwrite=true to replace.");
                        }
                        
                        // Delete existing destination
                        System.IO.File.Delete(destPath);
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
                    
                    // Move the file
                    System.IO.File.Move(sourcePath, destPath);
                    
                    var destInfo = new FileInfo(destPath);
                    
                    // Determine if this was a rename (same parent) or move (different parent)
                    var sourceParent = Path.GetDirectoryName(sourcePath);
                    var destParent = Path.GetDirectoryName(destPath);
                    
                    var operation = string.Equals(sourceParent, destParent, StringComparison.OrdinalIgnoreCase) 
                        ? "renamed" : "moved";
                    
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"File {operation}: {sourceRelative} → {destRelative}");
                    sb.AppendLine($"Size: {FormatFileSize(destInfo.Length)}");
                    sb.AppendLine($"Modified: {destInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                    
                    return sb.ToString();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to move file: {ex.Message}", ex);
                }
            });
        }
    }
}
