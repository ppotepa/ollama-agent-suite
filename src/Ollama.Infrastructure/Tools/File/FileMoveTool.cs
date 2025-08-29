using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Tools.File
{
    /// <summary>
    /// File move tool - equivalent to 'move' or 'mv' command
    /// Moves or renames files within session boundaries
    /// </summary>
    public class FileMoveTool : ITool
    {
        private readonly ISessionScope _sessionScope;
        private readonly ILogger<FileMoveTool> _logger;

        public string Name => "FileMove";
        public string Description => "Moves or renames files (equivalent to 'move' or 'mv' command)";
        public IEnumerable<string> Capabilities => new[] { "file:move", "file:rename", "fs:move", "fs:mv" };
        public bool RequiresNetwork => false;
        public bool RequiresFileSystem => true;

        public FileMoveTool(ISessionScope sessionScope, ILogger<FileMoveTool> logger)
        {
            _sessionScope = sessionScope;
            _logger = logger;
        }

        public Task<bool> DryRunAsync(ToolContext context)
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
            
            var safeSource = _sessionScope.GetSafePath(sourcePath);
            var safeDest = _sessionScope.GetSafePath(destPath);
            
            return Task.FromResult(System.IO.File.Exists(safeSource) && !System.IO.File.Exists(safeDest));
        }

        public Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for file move
        }

        public async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                if (!context.Parameters.TryGetValue("source", out var sourceObj) || 
                    !context.Parameters.TryGetValue("destination", out var destObj) ||
                    string.IsNullOrWhiteSpace(sourceObj?.ToString()) || 
                    string.IsNullOrWhiteSpace(destObj?.ToString()))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = "Both 'source' and 'destination' parameters are required",
                        ExecutionTime = DateTime.Now - startTime
                    };
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
                var safeSource = _sessionScope.GetSafePath(sourcePath);
                var safeDest = _sessionScope.GetSafePath(destPath);
                
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
                
                _logger.LogInformation("FileMove completed from {Source} to {Destination}", sourcePath, destPath);
                
                return new ToolResult
                {
                    Success = true,
                    Output = result,
                    ExecutionTime = DateTime.Now - startTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving file");
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"File move failed: {ex.Message}",
                    ExecutionTime = DateTime.Now - startTime
                };
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
