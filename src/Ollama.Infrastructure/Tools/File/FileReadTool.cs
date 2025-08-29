using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Ollama.Infrastructure.Tools.File
{
    /// <summary>
    /// File read tool - equivalent to 'type' or 'cat' command
    /// Reads file contents within session boundaries with cursor navigation support
    /// </summary>
    public class FileReadTool : AbstractTool
    {
        public override string Name => "FileRead";
        public override string Description => "Reads file contents (equivalent to 'type' or 'cat' command)";
        public override IEnumerable<string> Capabilities => new[] { "file:read", "file:content", "fs:type", "fs:cat", "cursor:navigate" };
        public override bool RequiresNetwork => false;
        public override bool RequiresFileSystem => true;

        public FileReadTool(ISessionScope sessionScope, ILogger<FileReadTool> logger) 
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
            var safePath = GetSafePath(path);
            return Task.FromResult(System.IO.File.Exists(safePath));
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for file reading
        }

        public override async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                // Ensure SessionScope is initialized with correct sessionId from context
                EnsureSessionScopeInitialized(context);
                
                // Process cursor navigation first
                var navigationResult = ProcessCursorNavigation(context);
                
                if (!context.Parameters.TryGetValue("path", out var pathObj) || string.IsNullOrWhiteSpace(pathObj?.ToString()))
                {
                    return CreateResult(false, errorMessage: "Path parameter is required", startTime: startTime);
                }

                var path = pathObj.ToString()!;
                
                // Get options
                var encoding = context.Parameters.TryGetValue("encoding", out var encodingObj) 
                    ? encodingObj?.ToString() ?? "utf-8" 
                    : "utf-8";
                var maxLines = context.Parameters.TryGetValue("maxLines", out var maxLinesObj) 
                    && maxLinesObj is int max ? max : int.MaxValue;
                var startLine = context.Parameters.TryGetValue("startLine", out var startLineObj) 
                    && startLineObj is int start ? start : 1;
                var showLineNumbers = context.Parameters.TryGetValue("showLineNumbers", out var lineNumObj) 
                    && lineNumObj is bool lineNum && lineNum;

                // Get safe path within session
                var safePath = GetSafePath(path);
                
                if (!System.IO.File.Exists(safePath))
                {
                    return CreateResult(false, errorMessage: $"File not found: {GetRelativePath(safePath)}", startTime: startTime);
                }

                // Read file
                var result = await ReadFileContent(safePath, encoding, startLine, maxLines, showLineNumbers, cancellationToken);
                
                Logger.LogInformation("FileRead completed for path: {Path}", path);
                
                return CreateSuccessResultWithContext(result, navigationResult, startTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error reading file");
                return CreateResult(false, errorMessage: $"File read failed: {ex.Message}", startTime: startTime);
            }
        }

        private async Task<string> ReadFileContent(string path, string encodingName, int startLine, int maxLines, bool showLineNumbers, CancellationToken cancellationToken)
        {
            try
            {
                var fileInfo = new FileInfo(path);
                var relativePath = GetRelativePath(path);
                
                // Get encoding
                var encoding = GetEncoding(encodingName);
                
                // Read all lines
                var allLines = await System.IO.File.ReadAllLinesAsync(path, encoding, cancellationToken);
                
                // Calculate range
                var startIndex = Math.Max(0, startLine - 1);
                var endIndex = Math.Min(allLines.Length, startIndex + maxLines);
                var linesToShow = allLines.Skip(startIndex).Take(endIndex - startIndex).ToArray();
                
                var sb = new StringBuilder();
                
                // Header with file info
                sb.AppendLine($"File: {relativePath}");
                sb.AppendLine($"Size: {FormatFileSize(fileInfo.Length)}");
                sb.AppendLine($"Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Encoding: {encoding.EncodingName}");
                
                if (startLine > 1 || maxLines < allLines.Length)
                {
                    sb.AppendLine($"Lines: {startLine} to {Math.Min(startLine + linesToShow.Length - 1, allLines.Length)} of {allLines.Length}");
                }
                else
                {
                    sb.AppendLine($"Lines: {allLines.Length}");
                }
                
                sb.AppendLine(new string('-', 50));
                
                // Content
                for (int i = 0; i < linesToShow.Length; i++)
                {
                    if (showLineNumbers)
                    {
                        var lineNumber = startIndex + i + 1;
                        sb.AppendLine($"{lineNumber,6}: {linesToShow[i]}");
                    }
                    else
                    {
                        sb.AppendLine(linesToShow[i]);
                    }
                }
                
                // Footer if truncated
                if (endIndex < allLines.Length)
                {
                    sb.AppendLine(new string('-', 50));
                    sb.AppendLine($"... ({allLines.Length - endIndex} more lines)");
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read file: {ex.Message}", ex);
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
    }
}
