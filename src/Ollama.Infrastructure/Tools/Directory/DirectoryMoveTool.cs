using Ollama.Domain.Tools;
using Ollama.Domain.Tools.Attributes;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Tools.Directory
{
    /// <summary>
    /// Directory move/rename tool - equivalent to 'move' or 'mv' command
    /// Moves or renames directories within session boundaries
    /// </summary>
    [ToolDescription(
        "Moves or renames directories within session boundaries",
        "Equivalent to 'move' or 'mv' command. Relocates directories from one location to another within the session workspace. Can also be used for directory renaming.",
        "Directory Operations")]
    [ToolUsage(
        "Move or rename directories in session workspace",
        SecondaryUseCases = new[] { "Directory relocation", "Directory renaming", "Structure reorganization", "Workspace cleanup" },
        RequiredParameters = new[] { "sourcePath", "destinationPath" },
        OptionalParameters = new[] { "cd", "overwrite" },
        ExampleInvocation = "DirectoryMove with sourcePath=\"old-name\" destinationPath=\"new-name\"",
        ExpectedOutput = "Successfully moved/renamed directory",
        RequiresFileSystem = true,
        RequiresNetwork = false,
        SafetyNotes = "All operations within session boundaries",
        PerformanceNotes = "Large directories may take time to move")]
    [ToolCapabilities(
        ToolCapability.DirectoryMove | ToolCapability.CursorNavigation,
        FallbackStrategy = "Copy and delete if direct move fails")]
    public class DirectoryMoveTool : AbstractTool
    {
        public DirectoryMoveTool(ISessionScope sessionScope, ILogger<DirectoryMoveTool> logger)
            : base(sessionScope, logger)
        {
        }

        public override string Name => "DirectoryMove";
        public override string Description => "Moves or renames directories (equivalent to 'move' or 'mv' command)";
        public override IEnumerable<string> Capabilities => new[] { "dir:move", "dir:rename", "directory:relocate", "fs:move" };
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
            
            return Task.FromResult(System.IO.Directory.Exists(safeSource) && !System.IO.Directory.Exists(safeDest));
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for directory move
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

                // Get safe paths within session
                var safeSource = GetSafePath(sourcePath);
                var safeDest = GetSafePath(destPath);
                
                if (!System.IO.Directory.Exists(safeSource))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = $"Source directory not found: {GetRelativePath(safeSource)}",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }

                // Safety check - prevent moving session root
                if (string.Equals(safeSource, SessionScope.SessionRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = "Cannot move session root directory",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }

                // Move directory
                var result = await MoveDirectory(safeSource, safeDest, overwrite);
                
                Logger.LogInformation("DirectoryMove completed from {Source} to {Destination}", sourcePath, destPath);
                
                return new ToolResult
                {
                    Success = true,
                    Output = result,
                    ExecutionTime = DateTime.Now - startTime
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error moving directory");
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Directory move failed: {ex.Message}",
                    ExecutionTime = DateTime.Now - startTime
                };
            }
        }

        private async Task<string> MoveDirectory(string sourcePath, string destPath, bool overwrite)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var sourceRelative = GetRelativePath(sourcePath);
                    var destRelative = GetRelativePath(destPath);
                    
                    // Check if destination exists
                    if (System.IO.Directory.Exists(destPath))
                    {
                        if (!overwrite)
                        {
                            throw new InvalidOperationException($"Destination directory already exists: {destRelative}. Use overwrite=true to replace.");
                        }
                        
                        // Delete existing destination
                        System.IO.Directory.Delete(destPath, true);
                    }
                    
                    // Ensure destination parent directory exists
                    var destParent = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(destParent) && !System.IO.Directory.Exists(destParent))
                    {
                        System.IO.Directory.CreateDirectory(destParent);
                    }
                    
                    // Move the directory
                    System.IO.Directory.Move(sourcePath, destPath);
                    
                    // Determine if this was a rename (same parent) or move (different parent)
                    var sourceParent = Path.GetDirectoryName(sourcePath);
                    var destParentCheck = Path.GetDirectoryName(destPath);
                    
                    var operation = string.Equals(sourceParent, destParentCheck, StringComparison.OrdinalIgnoreCase) 
                        ? "renamed" : "moved";
                    
                    return $"Directory {operation}: {sourceRelative} → {destRelative}";
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to move directory: {ex.Message}", ex);
                }
            });
        }
    }
}
