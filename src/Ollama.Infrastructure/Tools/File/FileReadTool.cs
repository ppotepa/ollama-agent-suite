using Ollama.Domain.Tools;
using Ollama.Domain.Tools.Attributes;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Ollama.Infrastructure.Tools.File
{
    /// <summary>
    /// File read tool - equivalent to 'type' or 'cat' command
    /// Reads file contents within session boundaries with cursor navigation support
    /// </summary>
    [ToolDescription(
        "Reads file contents within session boundaries", 
        "Equivalent to 'type' (Windows) or 'cat' (Unix) command. Supports cursor navigation to change working directory before reading files. All file paths are validated to be within session boundaries for security.", 
        "File Operations")]
    [ToolUsage(
        "Read text files to examine their contents",
        SecondaryUseCases = new[] { "Display file contents", "Examine configuration files", "View code files", "Check log files" },
        RequiredParameters = new[] { "path" },
        OptionalParameters = new[] { "cd", "encoding", "showLineNumbers" },
        ExampleInvocation = "FileRead with path=\"config.txt\" to read configuration file",
        ExpectedOutput = "File contents as text with optional line numbers",
        RequiresFileSystem = true,
        SafetyNotes = "All file paths are validated against session boundaries",
        PerformanceNotes = "Large files may take time to read; consider file size before reading")]
    [ToolCapabilities(
        ToolCapability.FileRead | ToolCapability.CursorNavigation | ToolCapability.PathResolution,
        FallbackStrategy = "Multiple read strategies: standard file read, stream-based read, binary-as-text read, retry with different encodings")]
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

        public override IEnumerable<string> GetAlternativeMethods()
        {
            return new[] { "read_all_lines", "read_with_stream", "read_binary_as_text", "read_with_retry" };
        }

        protected override async Task<ToolResult> RunAlternativeMethodAsync(ToolContext context, string methodName, CancellationToken cancellationToken = default)
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
                var safePath = GetSafePath(path);
                
                if (!System.IO.File.Exists(safePath))
                {
                    return CreateResult(false, errorMessage: $"File not found: {GetRelativePath(safePath)}", startTime: startTime);
                }

                var encoding = context.Parameters.TryGetValue("encoding", out var encodingObj) 
                    ? encodingObj?.ToString() ?? "utf-8" 
                    : "utf-8";
                var maxLines = context.Parameters.TryGetValue("maxLines", out var maxLinesObj) 
                    && maxLinesObj is int max ? max : int.MaxValue;
                var startLine = context.Parameters.TryGetValue("startLine", out var startLineObj) 
                    && startLineObj is int start ? start : 1;
                var showLineNumbers = context.Parameters.TryGetValue("showLineNumbers", out var lineNumObj) 
                    && lineNumObj is bool lineNum && lineNum;

                string result = methodName switch
                {
                    "read_all_lines" => await ReadWithAllLines(safePath, encoding, startLine, maxLines, showLineNumbers, cancellationToken),
                    "read_with_stream" => await ReadWithStream(safePath, encoding, startLine, maxLines, showLineNumbers, cancellationToken),
                    "read_binary_as_text" => await ReadBinaryAsText(safePath, startLine, maxLines, showLineNumbers, cancellationToken),
                    "read_with_retry" => await ReadWithRetry(safePath, encoding, startLine, maxLines, showLineNumbers, cancellationToken),
                    _ => throw new NotSupportedException($"Alternative method '{methodName}' is not supported")
                };

                Logger.LogInformation("FileRead completed using alternative method {Method} for path: {Path}", methodName, path);
                return CreateSuccessResultWithContext(result, navigationResult, startTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error reading file with alternative method {Method}", methodName);
                return CreateResult(false, errorMessage: $"File read failed with method {methodName}: {ex.Message}", startTime: startTime);
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

        #region Alternative Reading Methods

        /// <summary>
        /// Alternative method 1: Use ReadAllLines approach
        /// </summary>
        private async Task<string> ReadWithAllLines(string path, string encodingName, int startLine, int maxLines, bool showLineNumbers, CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(path);
            var relativePath = GetRelativePath(path);
            var encoding = GetEncoding(encodingName);
            
            var allLines = await System.IO.File.ReadAllLinesAsync(path, encoding, cancellationToken);
            return FormatFileContent(relativePath, fileInfo, allLines, startLine, maxLines, showLineNumbers, encoding);
        }

        /// <summary>
        /// Alternative method 2: Use StreamReader for large files
        /// </summary>
        private async Task<string> ReadWithStream(string path, string encodingName, int startLine, int maxLines, bool showLineNumbers, CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(path);
            var relativePath = GetRelativePath(path);
            var encoding = GetEncoding(encodingName);
            
            var lines = new List<string>();
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            using var reader = new StreamReader(stream, encoding);
            
            string? line;
            int currentLine = 1;
            
            // Skip to start line
            while (currentLine < startLine && (line = await reader.ReadLineAsync()) != null)
            {
                currentLine++;
            }
            
            // Read up to maxLines
            int linesRead = 0;
            while (linesRead < maxLines && (line = await reader.ReadLineAsync()) != null)
            {
                lines.Add(line);
                linesRead++;
                currentLine++;
            }
            
            return FormatFileContent(relativePath, fileInfo, lines.ToArray(), startLine, maxLines, showLineNumbers, encoding);
        }

        /// <summary>
        /// Alternative method 3: Read as binary and convert to text (for corrupted text files)
        /// </summary>
        private async Task<string> ReadBinaryAsText(string path, int startLine, int maxLines, bool showLineNumbers, CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(path);
            var relativePath = GetRelativePath(path);
            
            var bytes = await System.IO.File.ReadAllBytesAsync(path, cancellationToken);
            
            // Try multiple encodings
            var encodings = new[] { Encoding.UTF8, Encoding.ASCII, Encoding.Unicode, Encoding.GetEncoding(1252) };
            
            foreach (var encoding in encodings)
            {
                try
                {
                    var text = encoding.GetString(bytes);
                    var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
                    
                    // Remove empty trailing lines from split
                    if (lines.Length > 0 && string.IsNullOrEmpty(lines[^1]))
                    {
                        lines = lines[..^1];
                    }
                    
                    return FormatFileContent(relativePath, fileInfo, lines, startLine, maxLines, showLineNumbers, encoding);
                }
                catch
                {
                    continue; // Try next encoding
                }
            }
            
            throw new InvalidOperationException("Could not read file with any supported text encoding");
        }

        /// <summary>
        /// Alternative method 4: Read with retry and error recovery
        /// </summary>
        private async Task<string> ReadWithRetry(string path, string encodingName, int startLine, int maxLines, bool showLineNumbers, CancellationToken cancellationToken)
        {
            var fileInfo = new FileInfo(path);
            var relativePath = GetRelativePath(path);
            var encoding = GetEncoding(encodingName);
            
            const int maxRetries = 3;
            var delay = TimeSpan.FromMilliseconds(100);
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize: 8192, useAsync: true);
                    using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true);
                    
                    var content = await reader.ReadToEndAsync();
                    var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
                    
                    // Remove empty trailing line from split if present
                    if (lines.Length > 0 && string.IsNullOrEmpty(lines[^1]))
                    {
                        lines = lines[..^1];
                    }
                    
                    return FormatFileContent(relativePath, fileInfo, lines, startLine, maxLines, showLineNumbers, encoding);
                }
                catch (IOException) when (attempt < maxRetries)
                {
                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
                }
            }
            
            throw new InvalidOperationException($"Failed to read file after {maxRetries} attempts");
        }

        /// <summary>
        /// Common method to format file content consistently across all reading methods
        /// </summary>
        private string FormatFileContent(string relativePath, FileInfo fileInfo, string[] lines, int startLine, int maxLines, bool showLineNumbers, Encoding encoding)
        {
            var startIndex = Math.Max(0, startLine - 1);
            var endIndex = Math.Min(lines.Length, startIndex + maxLines);
            var linesToShow = lines.Skip(startIndex).Take(endIndex - startIndex).ToArray();
            
            var sb = new StringBuilder();
            
            // Header with file info
            sb.AppendLine($"File: {relativePath}");
            sb.AppendLine($"Size: {FormatFileSize(fileInfo.Length)}");
            sb.AppendLine($"Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Encoding: {encoding.EncodingName}");
            
            if (startLine > 1 || maxLines < lines.Length)
            {
                sb.AppendLine($"Lines: {startLine} to {Math.Min(startLine + linesToShow.Length - 1, lines.Length)} of {lines.Length}");
            }
            else
            {
                sb.AppendLine($"Lines: {lines.Length}");
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
            if (endIndex < lines.Length)
            {
                sb.AppendLine(new string('-', 50));
                sb.AppendLine($"... ({lines.Length - endIndex} more lines)");
            }
            
            return sb.ToString();
        }

        #endregion
    }
}
