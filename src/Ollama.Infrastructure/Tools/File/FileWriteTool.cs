using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Ollama.Infrastructure.Tools.File
{
    /// <summary>
    /// File write tool - equivalent to 'echo' or redirection commands
    /// Writes content to files within session boundaries
    /// </summary>
    public class FileWriteTool : ITool
    {
        private readonly ISessionScope _sessionScope;
        private readonly ILogger<FileWriteTool> _logger;

        public string Name => "FileWrite";
        public string Description => "Writes content to files (equivalent to 'echo' or redirection commands)";
        public IEnumerable<string> Capabilities => new[] { "file:write", "file:create", "fs:echo", "fs:redirect" };
        public bool RequiresNetwork => false;
        public bool RequiresFileSystem => true;

        public FileWriteTool(ISessionScope sessionScope, ILogger<FileWriteTool> logger)
        {
            _sessionScope = sessionScope;
            _logger = logger;
        }

        public Task<bool> DryRunAsync(ToolContext context)
        {
            if (!context.Parameters.TryGetValue("path", out var pathObj) || 
                !context.Parameters.TryGetValue("content", out var contentObj) ||
                string.IsNullOrWhiteSpace(pathObj?.ToString()))
            {
                return Task.FromResult(false);
            }

            var path = pathObj.ToString()!;
            return Task.FromResult(_sessionScope.IsPathValid(path));
        }

        public Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for file writing
        }

        public async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                if (!context.Parameters.TryGetValue("path", out var pathObj) || 
                    !context.Parameters.TryGetValue("content", out var contentObj) ||
                    string.IsNullOrWhiteSpace(pathObj?.ToString()))
                {
                    return new ToolResult
                    {
                        Success = false,
                        ErrorMessage = "Both 'path' and 'content' parameters are required",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }

                var path = pathObj.ToString()!;
                var content = contentObj?.ToString() ?? string.Empty;
                
                // Get options
                var encoding = context.Parameters.TryGetValue("encoding", out var encodingObj) 
                    ? encodingObj?.ToString() ?? "utf-8" 
                    : "utf-8";
                var append = context.Parameters.TryGetValue("append", out var appendObj) 
                    && appendObj is bool app && app;
                var createDirectories = context.Parameters.TryGetValue("createDirectories", out var createDirObj) 
                    ? createDirObj is bool createDir ? createDir : true  // Default to true
                    : true;

                // Get safe path within session
                var safePath = _sessionScope.GetSafePath(path);
                
                // Create parent directories if needed
                if (createDirectories)
                {
                    var parentDir = Path.GetDirectoryName(safePath);
                    if (!string.IsNullOrEmpty(parentDir) && !System.IO.Directory.Exists(parentDir))
                    {
                        System.IO.Directory.CreateDirectory(parentDir);
                    }
                }

                // Write file
                var result = await WriteFileContent(safePath, content, encoding, append, cancellationToken);
                
                _logger.LogInformation("FileWrite completed for path: {Path}", path);
                
                return new ToolResult
                {
                    Success = true,
                    Output = result,
                    ExecutionTime = DateTime.Now - startTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing file");
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"File write failed: {ex.Message}",
                    ExecutionTime = DateTime.Now - startTime
                };
            }
        }

        private async Task<string> WriteFileContent(string path, string content, string encodingName, bool append, CancellationToken cancellationToken)
        {
            try
            {
                var relativePath = GetRelativePath(path);
                var encoding = GetEncoding(encodingName);
                var fileExisted = System.IO.File.Exists(path);
                
                // Write content
                if (append && fileExisted)
                {
                    await System.IO.File.AppendAllTextAsync(path, content, encoding, cancellationToken);
                }
                else
                {
                    await System.IO.File.WriteAllTextAsync(path, content, encoding, cancellationToken);
                }
                
                // Get file info after writing
                var fileInfo = new FileInfo(path);
                var lines = content.Split('\n').Length;
                var bytes = encoding.GetByteCount(content);
                
                var sb = new StringBuilder();
                
                if (append && fileExisted)
                {
                    sb.AppendLine($"Content appended to: {relativePath}");
                }
                else if (fileExisted)
                {
                    sb.AppendLine($"File overwritten: {relativePath}");
                }
                else
                {
                    sb.AppendLine($"File created: {relativePath}");
                }
                
                sb.AppendLine($"Size: {FormatFileSize(fileInfo.Length)}");
                sb.AppendLine($"Lines written: {lines}");
                sb.AppendLine($"Bytes written: {bytes}");
                sb.AppendLine($"Encoding: {encoding.EncodingName}");
                sb.AppendLine($"Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to write file: {ex.Message}", ex);
            }
        }

        private Encoding GetEncoding(string encodingName)
        {
            return encodingName.ToLower() switch
            {
                "utf-8" or "utf8" => Encoding.UTF8,
                "ascii" => Encoding.ASCII,
                "unicode" or "utf-16" or "utf16" => Encoding.Unicode,
                "utf-32" or "utf32" => Encoding.UTF32,
                "windows-1252" or "cp1252" => Encoding.GetEncoding(1252),
                _ => Encoding.UTF8 // Default to UTF-8
            };
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
