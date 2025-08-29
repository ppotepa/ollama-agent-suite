using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Ollama.Infrastructure.Tools.File
{
    /// <summary>
    /// File attributes tool - equivalent to 'attrib' command
    /// Gets and sets file attributes within session boundaries
    /// </summary>
    public class FileAttributesTool : ITool
    {
        private readonly ISessionScope _sessionScope;
        private readonly ILogger<FileAttributesTool> _logger;

        public string Name => "FileAttributes";
        public string Description => "Gets and sets file attributes (equivalent to 'attrib' command)";
        public IEnumerable<string> Capabilities => new[] { "file:attributes", "file:properties", "fs:attrib" };
        public bool RequiresNetwork => false;
        public bool RequiresFileSystem => true;

        public FileAttributesTool(ISessionScope sessionScope, ILogger<FileAttributesTool> logger)
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
            return Task.FromResult(0.0m); // No cost for file attributes
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
                
                _logger.LogInformation("FileAttributes completed for path: {Path}", path);
                
                return new ToolResult
                {
                    Success = true,
                    Output = result,
                    ExecutionTime = DateTime.Now - startTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file attributes");
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"File attributes operation failed: {ex.Message}",
                    ExecutionTime = DateTime.Now - startTime
                };
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
