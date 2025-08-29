using Ollama.Domain.Tools;
using Ollama.Domain.Tools.Attributes;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Ollama.Infrastructure.Tools.File
{
    /// <summary>
    /// File write tool - equivalent to 'echo' or redirection commands
    /// Writes content to files within session boundaries
    /// </summary>
    [ToolDescription(
        "Writes content to files within session boundaries",
        "Equivalent to 'echo' or redirection commands. Creates new files or overwrites existing ones with specified content. Supports various encodings and append modes.",
        "File Operations")]
    [ToolUsage(
        "Create or update files with specified content",
        SecondaryUseCases = new[] { "File creation", "Content writing", "Text output", "Configuration generation" },
        RequiredParameters = new[] { "path", "content" },
        OptionalParameters = new[] { "cd", "encoding", "append", "createDirectories" },
        ExampleInvocation = "FileWrite with path=\"output.txt\" content=\"Hello World\"",
        ExpectedOutput = "Successfully wrote content to file",
        RequiresFileSystem = true,
        RequiresNetwork = false,
        SafetyNotes = "All operations within session boundaries - can overwrite existing files",
        PerformanceNotes = "Large content may take time to write")]
    [ToolCapabilities(
        ToolCapability.FileWrite | ToolCapability.FileCreate | ToolCapability.CursorNavigation,
        FallbackStrategy = "Stream-based writing if direct file writing fails")]
    public class FileWriteTool : AbstractTool
    {
        public override string Name => "FileWrite";
        public override string Description => "Writes content to files (equivalent to 'echo' or redirection commands)";
        public override IEnumerable<string> Capabilities => new[] { "file:write", "file:create", "fs:echo", "fs:redirect" };
        public override bool RequiresNetwork => false;
        public override bool RequiresFileSystem => true;

        public FileWriteTool(ISessionScope sessionScope, ILogger<FileWriteTool> logger)
            : base(sessionScope, logger)
        {
        }

        public override Task<bool> DryRunAsync(ToolContext context)
        {
            if (!context.Parameters.TryGetValue("path", out var pathObj) || 
                !context.Parameters.TryGetValue("content", out var contentObj) ||
                string.IsNullOrWhiteSpace(pathObj?.ToString()))
            {
                return Task.FromResult(false);
            }

            var path = pathObj.ToString()!;
            return Task.FromResult(SessionScope.IsPathValid(path));
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for file writing
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
                
                if (!context.Parameters.TryGetValue("path", out var pathObj) || 
                    !context.Parameters.TryGetValue("content", out var contentObj) ||
                    string.IsNullOrWhiteSpace(pathObj?.ToString()))
                {
                    return CreateResult(false, errorMessage: "Both 'path' and 'content' parameters are required", startTime: startTime);
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
                var safePath = GetSafePath(path);
                
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
                
                Logger.LogInformation("FileWrite completed for path: {Path}", path);
                
                return CreateSuccessResultWithContext(result, navigationResult, startTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error writing file");
                return CreateResult(false, errorMessage: $"File write failed: {ex.Message}", startTime: startTime);
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
    }
}
