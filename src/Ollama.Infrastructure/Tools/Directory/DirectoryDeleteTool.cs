using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Tools.Directory
{
    /// <summary>
    /// Directory deletion tool - equivalent to 'rmdir' or 'rd' command
    /// Deletes directories within session boundaries
    /// </summary>
    public class DirectoryDeleteTool : ITool
    {
        private readonly ISessionScope _sessionScope;
        private readonly ILogger<DirectoryDeleteTool> _logger;

        public string Name => "DirectoryDelete";
        public string Description => "Deletes directories (equivalent to 'rmdir' or 'rd' command)";
        public IEnumerable<string> Capabilities => new[] { "dir:delete", "directory:remove", "fs:rmdir" };
        public bool RequiresNetwork => false;
        public bool RequiresFileSystem => true;

        public DirectoryDeleteTool(ISessionScope sessionScope, ILogger<DirectoryDeleteTool> logger)
        {
            _sessionScope = sessionScope;
            _logger = logger;
        }

        public Task<bool> DryRunAsync(ToolContext context)
        {
            if (!context.Parameters.TryGetValue("path", out var pathObj) || string.IsNullOrWhiteSpace(pathObj?.ToString()))
            {
                return Task.FromResult(false);
            }

            var path = pathObj.ToString()!;
            var safePath = _sessionScope.GetSafePath(path);
            return Task.FromResult(System.IO.Directory.Exists(safePath));
        }

        public Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for directory deletion
        }

        public async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                if (!context.Parameters.TryGetValue("path", out var pathObj) || string.IsNullOrWhiteSpace(pathObj?.ToString()))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = "Path parameter is required",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }

                var path = pathObj.ToString()!;
                
                // Get options
                var recursive = context.Parameters.TryGetValue("recursive", out var recursiveObj) 
                    && recursiveObj is bool rec && rec;
                var force = context.Parameters.TryGetValue("force", out var forceObj) 
                    && forceObj is bool f && f;

                // Get safe path within session
                var safePath = _sessionScope.GetSafePath(path);
                
                if (!System.IO.Directory.Exists(safePath))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = $"Directory not found: {GetRelativePath(safePath)}",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }

                // Safety check - prevent deletion of session root
                if (string.Equals(safePath, _sessionScope.SessionRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = "Cannot delete session root directory",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }

                // Delete directory
                var result = await DeleteDirectory(safePath, recursive, force);
                
                _logger.LogInformation("DirectoryDelete completed for path: {Path}", path);
                
                return new ToolResult
                {
                    Success = true,
                    Output = result,
                    ExecutionTime = DateTime.Now - startTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting directory");
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Directory deletion failed: {ex.Message}",
                    ExecutionTime = DateTime.Now - startTime
                };
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
                _logger.LogWarning(ex, "Warning: Could not remove read-only attributes from {Path}", directory.FullName);
            }
        }

        private string GetRelativePath(string fullPath)
        {
            var sessionRoot = _sessionScope.SessionRoot;
            if (fullPath.StartsWith(sessionRoot))
            {
                var relative = fullPath.Substring(sessionRoot.Length);
                return relative.TrimStart('\\', '/') ?? ".";
            }
            return fullPath;
        }
    }
}
