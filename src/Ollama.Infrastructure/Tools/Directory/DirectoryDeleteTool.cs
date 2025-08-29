using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Tools.Directory
{
    /// <summary>
    /// Directory deletion tool - equivalent to 'rmdir' or 'rd' command
    /// Deletes directories within session boundaries
    /// </summary>
    public class DirectoryDeleteTool : AbstractTool
    {
        public override string Name => "DirectoryDelete";
        public override string Description => "Deletes directories (equivalent to 'rmdir' or 'rd' command)";
        public override IEnumerable<string> Capabilities => new[] { "dir:delete", "directory:remove", "fs:rmdir" };
        public override bool RequiresNetwork => false;
        public override bool RequiresFileSystem => true;

        public DirectoryDeleteTool(ISessionScope sessionScope, ILogger<DirectoryDeleteTool> logger)
            : base(sessionScope, logger)
        {
        }

        public override Task<bool> DryRunAsync(ToolContext context)
        {
            if (!context.Parameters.TryGetValue("path", out var pathObj) || string.IsNullOrWhiteSpace(pathObj?.ToString()))
            {
                return Task.FromResult(false);
            }

            var path = pathObj.ToString()!;
            var safePath = SessionScope.GetSafePath(path);
            return Task.FromResult(System.IO.Directory.Exists(safePath));
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for directory deletion
        }

        public override async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                EnsureSessionScopeInitialized(context);
                
                var navigationResult = ProcessCursorNavigation(context);
                
                if (!context.Parameters.TryGetValue("path", out var pathObj) || string.IsNullOrWhiteSpace(pathObj?.ToString()))
                {
                    return CreateResult(false, errorMessage: "Path parameter is required", startTime: startTime);
                }

                var path = pathObj.ToString()!;
                
                // Get options
                var recursive = context.Parameters.TryGetValue("recursive", out var recursiveObj) 
                    && recursiveObj is bool rec && rec;
                var force = context.Parameters.TryGetValue("force", out var forceObj) 
                    && forceObj is bool f && f;

                // Get safe path within session
                var safePath = SessionScope.GetSafePath(path);
                
                if (!System.IO.Directory.Exists(safePath))
                {
                    return CreateResult(false, errorMessage: $"Directory not found: {GetRelativePath(safePath)}", startTime: startTime);
                }

                // Safety check - prevent deletion of session root
                if (string.Equals(safePath, SessionScope.SessionRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return CreateResult(false, errorMessage: "Cannot delete session root directory", startTime: startTime);
                }

                // Delete directory
                var result = await DeleteDirectory(safePath, recursive, force);
                
                Logger.LogInformation("DirectoryDelete completed for path: {Path}", path);
                
                return CreateSuccessResultWithContext(result, navigationResult, startTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting directory");
                return CreateResult(false, errorMessage: $"Directory deletion failed: {ex.Message}", startTime: startTime);
            }
        }

        private async Task<string> DeleteDirectory(string path, bool recursive, bool force)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var directoryInfo = new DirectoryInfo(path);
                    var relativePath = GetRelativePath(path);
                    
                    // Check if directory is empty
                    var hasContents = directoryInfo.GetFileSystemInfos().Any();
                    
                    if (hasContents && !recursive)
                    {
                        throw new InvalidOperationException($"Directory is not empty: {relativePath}. Use recursive=true to delete with contents.");
                    }

                    if (force)
                    {
                        // Remove read-only attributes from all files and directories
                        RemoveReadOnlyAttributes(directoryInfo, recursive);
                    }

                    // Delete the directory
                    System.IO.Directory.Delete(path, recursive);
                    
                    var message = recursive && hasContents 
                        ? $"Directory and all contents deleted: {relativePath}"
                        : $"Directory deleted: {relativePath}";
                    
                    return message;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to delete directory: {ex.Message}", ex);
                }
            });
        }

        private void RemoveReadOnlyAttributes(DirectoryInfo directory, bool recursive)
        {
            try
            {
                // Remove read-only from directory itself
                if (directory.Attributes.HasFlag(FileAttributes.ReadOnly))
                {
                    directory.Attributes &= ~FileAttributes.ReadOnly;
                }

                // Remove read-only from all files in directory
                foreach (var file in directory.GetFiles())
                {
                    if (file.Attributes.HasFlag(FileAttributes.ReadOnly))
                    {
                        file.Attributes &= ~FileAttributes.ReadOnly;
                    }
                }

                // Recursively handle subdirectories
                if (recursive)
                {
                    foreach (var subDirectory in directory.GetDirectories())
                    {
                        RemoveReadOnlyAttributes(subDirectory, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Warning: Could not remove read-only attributes from {Path}", directory.FullName);
            }
        }
    }
}
