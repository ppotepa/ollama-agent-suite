using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Tools.File
{
    /// <summary>
    /// File delete tool - equivalent to 'del' or 'rm' command
    /// Deletes files within session boundaries
    /// </summary>
    public class FileDeleteTool : AbstractTool
    {
        public FileDeleteTool(ISessionScope sessionScope, ILogger<FileDeleteTool> logger)
            : base(sessionScope, logger)
        {
        }

        public override string Name => "FileDelete";
        public override string Description => "Deletes files (equivalent to 'del' or 'rm' command)";
        public override IEnumerable<string> Capabilities => new[] { "file:delete", "file:remove", "fs:del", "fs:rm" };
        public override bool RequiresNetwork => false;
        public override bool RequiresFileSystem => true;

        public override Task<bool> DryRunAsync(ToolContext context)
        {
            if (!context.Parameters.TryGetValue("path", out var pathObj) || string.IsNullOrWhiteSpace(pathObj?.ToString()))
            {
                return Task.FromResult(false);
            }

            var path = pathObj.ToString()!;
            var safePath = GetSafePath(path);
            return Task.FromResult(System.IO.File.Exists(safePath));
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for file deletion
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
                
                if (!context.Parameters.TryGetValue("path", out var pathObj) || string.IsNullOrWhiteSpace(pathObj?.ToString()))
                {
                    return CreateResult(false, errorMessage: "Path parameter is required", startTime: startTime);
                }

                var path = pathObj.ToString()!;
                
                // Get options
                var force = context.Parameters.TryGetValue("force", out var forceObj) 
                    && forceObj is bool f && f;

                // Get safe path within session
                var safePath = GetSafePath(path);
                
                if (!System.IO.File.Exists(safePath))
                {
                    return CreateResult(false, errorMessage: $"File not found: {GetRelativePath(safePath)}", startTime: startTime);
                }

                // Delete file
                var result = await DeleteFile(safePath, force);
                
                Logger.LogInformation("FileDelete completed for path: {Path}", path);
                
                return CreateSuccessResultWithContext(result, navigationResult, startTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting file");
                return CreateResult(false, errorMessage: $"File deletion failed: {ex.Message}", startTime: startTime);
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
    }
}
