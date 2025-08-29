using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Tools.File
{
    /// <summary>
    /// File delete tool - equivalent to 'del' or 'rm' command
    /// Deletes files within session boundaries
    /// </summary>
    public class FileDeleteTool : ITool
    {
        private readonly ISessionScope _sessionScope;
        private readonly ILogger<FileDeleteTool> _logger;

        public string Name => "FileDelete";
        public string Description => "Deletes files (equivalent to 'del' or 'rm' command)";
        public IEnumerable<string> Capabilities => new[] { "file:delete", "file:remove", "fs:del", "fs:rm" };
        public bool RequiresNetwork => false;
        public bool RequiresFileSystem => true;

        public FileDeleteTool(ISessionScope sessionScope, ILogger<FileDeleteTool> logger)
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
            return Task.FromResult(System.IO.File.Exists(safePath));
        }

        public Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for file deletion
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
                var force = context.Parameters.TryGetValue("force", out var forceObj) 
                    && forceObj is bool f && f;

                // Get safe path within session
                var safePath = _sessionScope.GetSafePath(path);
                
                if (!System.IO.File.Exists(safePath))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = $"File not found: {GetRelativePath(safePath)}",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }

                // Delete file
                var result = await DeleteFile(safePath, force);
                
                _logger.LogInformation("FileDelete completed for path: {Path}", path);
                
                return new ToolResult
                {
                    Success = true,
                    Output = result,
                    ExecutionTime = DateTime.Now - startTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file");
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"File deletion failed: {ex.Message}",
                    ExecutionTime = DateTime.Now - startTime
                };
            }
        }

        private async Task<string> DeleteFile(string path, bool force)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var relativePath = GetRelativePath(path);
                    var fileInfo = new FileInfo(path);
                    
                    // Store file info before deletion
                    var size = fileInfo.Length;
                    var lastModified = fileInfo.LastWriteTime;
                    var attributes = fileInfo.Attributes;
                    
                    // Remove read-only attribute if force is specified
                    if (force && fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
                    {
                        fileInfo.Attributes &= ~FileAttributes.ReadOnly;
                    }
                    
                    // Check if file is read-only and force is not specified
                    if (!force && fileInfo.Attributes.HasFlag(FileAttributes.ReadOnly))
                    {
                        throw new InvalidOperationException($"File is read-only: {relativePath}. Use force=true to delete read-only files.");
                    }
                    
                    // Delete the file
                    System.IO.File.Delete(path);
                    
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine($"File deleted: {relativePath}");
                    sb.AppendLine($"Size: {FormatFileSize(size)}");
                    sb.AppendLine($"Last modified: {lastModified:yyyy-MM-dd HH:mm:ss}");
                    
                    if (attributes.HasFlag(FileAttributes.ReadOnly))
                    {
                        sb.AppendLine("Read-only file: Yes (forced deletion)");
                    }
                    
                    return sb.ToString();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to delete file: {ex.Message}", ex);
                }
            });
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
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
