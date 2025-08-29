using Ollama.Domain.Tools;
using Ollama.Domain.Tools.Attributes;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Ollama.Infrastructure.Tools.File
{
    /// <summary>
    /// File attributes tool - equivalent to 'attrib' command
    /// Gets and sets file attributes within session boundaries
    /// </summary>
    [ToolDescription(
        "Gets and sets file attributes within session boundaries",
        "Equivalent to 'attrib' command. Displays and modifies file attributes such as read-only, hidden, system, and archive flags. Works within session workspace only.",
        "File Operations")]
    [ToolUsage(
        "View and modify file attributes and properties",
        SecondaryUseCases = new[] { "File property inspection", "Attribute modification", "Security settings", "File flag management" },
        RequiredParameters = new[] { "path" },
        OptionalParameters = new[] { "cd", "setAttributes", "removeAttributes", "recursive" },
        ExampleInvocation = "FileAttributes with path=\"document.txt\" to view file attributes",
        ExpectedOutput = "File attributes and properties information",
        RequiresFileSystem = true,
        RequiresNetwork = false,
        SafetyNotes = "Attribute changes are reversible - limited to session files",
        PerformanceNotes = "Fast operation for single files")]
    [ToolCapabilities(
        ToolCapability.FileAttributes | ToolCapability.FileRead | ToolCapability.CursorNavigation,
        FallbackStrategy = "Basic file info if attribute access fails")]
    public class FileAttributesTool : AbstractTool
    {
        public FileAttributesTool(ISessionScope sessionScope, ILogger<FileAttributesTool> logger)
            : base(sessionScope, logger)
        {
        }

        public override string Name => "FileAttributes";
        public override string Description => "Gets and sets file attributes (equivalent to 'attrib' command)";
        public override IEnumerable<string> Capabilities => new[] { "file:attributes", "file:properties", "fs:attrib" };
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
            return Task.FromResult(0.0m); // No cost for file attributes
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
                
                // Get safe path within session
                var safePath = GetSafePath(path);
                
                if (!System.IO.File.Exists(safePath))
                {
                    return CreateResult(false, errorMessage: $"File not found: {GetRelativePath(safePath)}", startTime: startTime);
                }

                // Check if this is a set operation
                var hasSetOperations = context.Parameters.ContainsKey("setReadOnly") ||
                                     context.Parameters.ContainsKey("setHidden") ||
                                     context.Parameters.ContainsKey("setSystem") ||
                                     context.Parameters.ContainsKey("setArchive");

                string result;
                if (hasSetOperations)
                {
                    result = await SetFileAttributes(safePath, context.Parameters);
                }
                else
                {
                    result = await GetFileAttributes(safePath);
                }
                
                Logger.LogInformation("FileAttributes completed for path: {Path}", path);
                
                return CreateSuccessResultWithContext(result, navigationResult, startTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing file attributes");
                return CreateResult(false, errorMessage: $"File attributes operation failed: {ex.Message}", startTime: startTime);
            }
        }

        private async Task<string> GetFileAttributes(string path)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var relativePath = GetRelativePath(path);
                    var fileInfo = new FileInfo(path);
                    
                    var sb = new StringBuilder();
                    sb.AppendLine($"File: {relativePath}");
                    sb.AppendLine($"Size: {FormatFileSize(fileInfo.Length)}");
                    sb.AppendLine($"Created: {fileInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"Accessed: {fileInfo.LastAccessTime:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine();
                    sb.AppendLine("Attributes:");
                    
                    var attributes = fileInfo.Attributes;
                    sb.AppendLine($"  Read-only: {attributes.HasFlag(FileAttributes.ReadOnly)}");
                    sb.AppendLine($"  Hidden: {attributes.HasFlag(FileAttributes.Hidden)}");
                    sb.AppendLine($"  System: {attributes.HasFlag(FileAttributes.System)}");
                    sb.AppendLine($"  Archive: {attributes.HasFlag(FileAttributes.Archive)}");
                    sb.AppendLine($"  Directory: {attributes.HasFlag(FileAttributes.Directory)}");
                    sb.AppendLine($"  Compressed: {attributes.HasFlag(FileAttributes.Compressed)}");
                    sb.AppendLine($"  Encrypted: {attributes.HasFlag(FileAttributes.Encrypted)}");
                    sb.AppendLine($"  Temporary: {attributes.HasFlag(FileAttributes.Temporary)}");
                    
                    sb.AppendLine();
                    sb.AppendLine($"Raw attributes value: {(int)attributes} (0x{(int)attributes:X})");
                    
                    return sb.ToString();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to get file attributes: {ex.Message}", ex);
                }
            });
        }

        private async Task<string> SetFileAttributes(string path, IDictionary<string, object> parameters)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var relativePath = GetRelativePath(path);
                    var fileInfo = new FileInfo(path);
                    var originalAttributes = fileInfo.Attributes;
                    var newAttributes = originalAttributes;
                    var changes = new List<string>();
                    
                    // Process attribute changes
                    if (parameters.TryGetValue("setReadOnly", out var readOnlyObj) && readOnlyObj is bool readOnly)
                    {
                        if (readOnly)
                        {
                            newAttributes |= FileAttributes.ReadOnly;
                            changes.Add("ReadOnly: ON");
                        }
                        else
                        {
                            newAttributes &= ~FileAttributes.ReadOnly;
                            changes.Add("ReadOnly: OFF");
                        }
                    }
                    
                    if (parameters.TryGetValue("setHidden", out var hiddenObj) && hiddenObj is bool hidden)
                    {
                        if (hidden)
                        {
                            newAttributes |= FileAttributes.Hidden;
                            changes.Add("Hidden: ON");
                        }
                        else
                        {
                            newAttributes &= ~FileAttributes.Hidden;
                            changes.Add("Hidden: OFF");
                        }
                    }
                    
                    if (parameters.TryGetValue("setSystem", out var systemObj) && systemObj is bool system)
                    {
                        if (system)
                        {
                            newAttributes |= FileAttributes.System;
                            changes.Add("System: ON");
                        }
                        else
                        {
                            newAttributes &= ~FileAttributes.System;
                            changes.Add("System: OFF");
                        }
                    }
                    
                    if (parameters.TryGetValue("setArchive", out var archiveObj) && archiveObj is bool archive)
                    {
                        if (archive)
                        {
                            newAttributes |= FileAttributes.Archive;
                            changes.Add("Archive: ON");
                        }
                        else
                        {
                            newAttributes &= ~FileAttributes.Archive;
                            changes.Add("Archive: OFF");
                        }
                    }
                    
                    // Apply changes if any
                    if (newAttributes != originalAttributes)
                    {
                        fileInfo.Attributes = newAttributes;
                    }
                    
                    var sb = new StringBuilder();
                    sb.AppendLine($"File attributes updated: {relativePath}");
                    sb.AppendLine();
                    
                    if (changes.Any())
                    {
                        sb.AppendLine("Changes applied:");
                        foreach (var change in changes)
                        {
                            sb.AppendLine($"  {change}");
                        }
                    }
                    else
                    {
                        sb.AppendLine("No changes applied.");
                    }
                    
                    sb.AppendLine();
                    sb.AppendLine($"Original attributes: {(int)originalAttributes} (0x{(int)originalAttributes:X})");
                    sb.AppendLine($"New attributes: {(int)newAttributes} (0x{(int)newAttributes:X})");
                    
                    return sb.ToString();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to set file attributes: {ex.Message}", ex);
                }
            });
        }
    }
}
