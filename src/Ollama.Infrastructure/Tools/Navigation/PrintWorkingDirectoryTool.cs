using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Ollama.Infrastructure.Tools.Navigation
{
    /// <summary>
    /// Print Working Directory tool - equivalent to 'pwd' command
    /// Shows current directory location within session with full context
    /// </summary>
    public class PrintWorkingDirectoryTool : AbstractTool
    {
        public override string Name => "PrintWorkingDirectory";
        public override string Description => "Shows current directory location (equivalent to 'pwd' command)";
        public override IEnumerable<string> Capabilities => new[] { "cursor:location", "directory:current", "fs:pwd" };
        public override bool RequiresNetwork => false;
        public override bool RequiresFileSystem => true;

        public PrintWorkingDirectoryTool(ISessionScope sessionScope, ILogger<PrintWorkingDirectoryTool> logger) 
            : base(sessionScope, logger)
        {
        }

        public override Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(true); // Always can show current directory
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for showing current directory
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
                
                // Get options
                var showDetails = context.Parameters.TryGetValue("showDetails", out var detailsObj) 
                    && detailsObj is bool details && details;
                var showTree = context.Parameters.TryGetValue("showTree", out var treeObj) 
                    && treeObj is bool tree && tree;

                var result = await GetWorkingDirectoryInfo(showDetails, showTree);
                
                Logger.LogInformation("PrintWorkingDirectory completed");
                
                return CreateSuccessResultWithContext(result, navigationResult, startTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting working directory info");
                return CreateResult(false, errorMessage: $"Failed to get working directory: {ex.Message}", startTime: startTime);
            }
        }

        private async Task<string> GetWorkingDirectoryInfo(bool showDetails, bool showTree)
        {
            return await Task.Run(() =>
            {
                var sb = new StringBuilder();
                
                try
                {
                    var currentDir = SessionScope.WorkingDirectory;
                    var relativePath = GetCurrentDirectory();
                    var sessionRoot = SessionScope.SessionRoot;
                    
                    // Basic directory info
                    sb.AppendLine($"Current Directory: {relativePath}");
                    sb.AppendLine($"Full Path: {currentDir}");
                    sb.AppendLine($"Session Root: {GetRelativePath(sessionRoot)}");
                    
                    if (showDetails)
                    {
                        var dirInfo = new DirectoryInfo(currentDir);
                        sb.AppendLine();
                        sb.AppendLine("--- Directory Details ---");
                        sb.AppendLine($"Created: {dirInfo.CreationTime:yyyy-MM-dd HH:mm:ss}");
                        sb.AppendLine($"Modified: {dirInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                        sb.AppendLine($"Attributes: {dirInfo.Attributes}");
                        
                        // Count contents
                        var subdirs = dirInfo.GetDirectories().Length;
                        var files = dirInfo.GetFiles().Length;
                        sb.AppendLine($"Subdirectories: {subdirs}");
                        sb.AppendLine($"Files: {files}");
                        
                        // Calculate total size
                        var totalSize = CalculateDirectorySize(dirInfo);
                        sb.AppendLine($"Total Size: {FormatFileSize(totalSize)}");
                    }
                    
                    if (showTree)
                    {
                        sb.AppendLine();
                        sb.AppendLine("--- Directory Tree ---");
                        sb.AppendLine(BuildDirectoryTree(currentDir, sessionRoot));
                    }
                    
                    return sb.ToString();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to get directory info: {ex.Message}", ex);
                }
            });
        }

        private long CalculateDirectorySize(DirectoryInfo dirInfo)
        {
            try
            {
                long size = 0;
                
                // Add files in current directory
                foreach (var file in dirInfo.GetFiles())
                {
                    size += file.Length;
                }
                
                // Add subdirectories (non-recursive to avoid long operations)
                // Just count immediate subdirectory files
                foreach (var subDir in dirInfo.GetDirectories())
                {
                    try
                    {
                        foreach (var file in subDir.GetFiles())
                        {
                            size += file.Length;
                        }
                    }
                    catch
                    {
                        // Skip directories we can't access
                    }
                }
                
                return size;
            }
            catch
            {
                return 0;
            }
        }

        private string BuildDirectoryTree(string currentDir, string sessionRoot)
        {
            var sb = new StringBuilder();
            
            try
            {
                // Build path from session root to current directory
                var relativePath = GetRelativePath(currentDir);
                var pathParts = relativePath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                
                sb.AppendLine($"📁 . (session root)");
                
                var indent = "";
                var currentPath = sessionRoot;
                
                foreach (var part in pathParts)
                {
                    indent += "  ";
                    currentPath = Path.Combine(currentPath, part);
                    var isCurrent = string.Equals(currentPath, currentDir, StringComparison.OrdinalIgnoreCase);
                    var marker = isCurrent ? "📂" : "📁";
                    sb.AppendLine($"{indent}{marker} {part}" + (isCurrent ? " ← current" : ""));
                }
                
                // Show immediate contents of current directory
                var dirInfo = new DirectoryInfo(currentDir);
                indent += "  ";
                
                var subdirs = dirInfo.GetDirectories().Take(5).ToList(); // Limit to 5 for brevity
                var files = dirInfo.GetFiles().Take(5).ToList(); // Limit to 5 for brevity
                
                foreach (var subdir in subdirs)
                {
                    sb.AppendLine($"{indent}📁 {subdir.Name}");
                }
                
                foreach (var file in files)
                {
                    sb.AppendLine($"{indent}📄 {file.Name}");
                }
                
                if (dirInfo.GetDirectories().Length > 5 || dirInfo.GetFiles().Length > 5)
                {
                    sb.AppendLine($"{indent}... (use DirectoryList for full contents)");
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error building tree: {ex.Message}";
            }
        }
    }
}
