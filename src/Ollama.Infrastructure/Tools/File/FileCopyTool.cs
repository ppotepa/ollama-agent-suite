using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Tools.File
{
    /// <summary>
    /// File copy tool - equivalent to 'copy' or 'cp' command
    /// Copies files within session boundaries
    /// </summary>
    public class FileCopyTool : ITool
    {
        private readonly ISessionScope _sessionScope;
        private readonly ILogger<FileCopyTool> _logger;

        public string Name => "FileCopy";
        public string Description => "Copies files (equivalent to 'copy' or 'cp' command)";
        public IEnumerable<string> Capabilities => new[] { "file:copy", "file:duplicate", "fs:copy", "fs:cp" };
        public bool RequiresNetwork => false;
        public bool RequiresFileSystem => true;

        public FileCopyTool(ISessionScope sessionScope, ILogger<FileCopyTool> logger)
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
            var safeSource = _sessionScope.GetSafePath(sourcePath);
            
            return Task.FromResult(System.IO.File.Exists(safeSource));
        }

        public Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for file copy
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
                var preserveAttributes = context.Parameters.TryGetValue("preserveAttributes", out var attrObj) 
                    && attrObj is bool attr && attr;
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

                // Copy file
                var result = await CopyFile(safeSource, safeDest, overwrite, preserveAttributes, createDirectories);
                
                _logger.LogInformation("FileCopy completed from {Source} to {Destination}", sourcePath, destPath);
                
                return new ToolResult
                {
                    Success = true,
                    Output = result,
                    ExecutionTime = DateTime.Now - startTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying file");
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"File copy failed: {ex.Message}",
                    ExecutionTime = DateTime.Now - startTime
                };
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
                            _logger.LogWarning(ex, "Warning: Could not preserve all attributes for {File}", destPath);
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
