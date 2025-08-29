using Ollama.Domain.Tools;
using Ollama.Domain.Tools.Attributes;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Ollama.Infrastructure.Tools.Navigation
{
    /// <summary>
    /// Cursor navigation tool - equivalent to 'cd' command
    /// Provides navigation within session boundaries with context awareness
    /// </summary>
    [ToolDescription(
        "Navigate through directories within session boundaries",
        "Equivalent to 'cd' command. Provides secure navigation within the session workspace with path validation and context awareness. Essential for multi-step operations.",
        "Navigation Operations")]
    [ToolUsage(
        "Change working directory for subsequent operations",
        SecondaryUseCases = new[] { "Directory navigation", "Path context setting", "Workspace exploration", "Operation preparation" },
        RequiredParameters = new[] { "path" },
        OptionalParameters = new[] { "showPath", "validatePath" },
        ExampleInvocation = "CursorNavigation with path=\"subfolder\" to navigate to directory",
        ExpectedOutput = "Successfully changed working directory",
        RequiresFileSystem = true,
        RequiresNetwork = false,
        SafetyNotes = "All navigation constrained within session boundaries",
        PerformanceNotes = "Fast operation with path validation")]
    [ToolCapabilities(
        ToolCapability.CursorNavigation | ToolCapability.PathResolution | ToolCapability.DirectoryNavigate,
        FallbackStrategy = "Relative path resolution if absolute paths fail")]
    public class CursorNavigationTool : AbstractTool
    {
        public override string Name => "CursorNavigation";
        public override string Description => "Navigate through directories within session (equivalent to 'cd' command)";
        public override IEnumerable<string> Capabilities => new[] { "cursor:navigate", "directory:change", "fs:cd" };
        public override bool RequiresNetwork => false;
        public override bool RequiresFileSystem => true;

        public CursorNavigationTool(ISessionScope sessionScope, ILogger<CursorNavigationTool> logger) 
            : base(sessionScope, logger)
        {
        }

        public override Task<bool> DryRunAsync(ToolContext context)
        {
            // Can always navigate - validation happens during execution
            return Task.FromResult(true);
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for navigation
        }

        public override async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                // Ensure SessionScope is initialized with correct sessionId from context
                EnsureSessionScopeInitialized(context);
                
                // Get navigation target
                var target = "."; // Default to current directory
                
                if (context.Parameters.TryGetValue("path", out var pathObj) && 
                    !string.IsNullOrWhiteSpace(pathObj?.ToString()))
                {
                    target = pathObj.ToString()!;
                }
                else if (context.Parameters.TryGetValue("cd", out var cdObj) && 
                         !string.IsNullOrWhiteSpace(cdObj?.ToString()))
                {
                    target = cdObj.ToString()!;
                }

                // Get additional options
                var showTree = context.Parameters.TryGetValue("showTree", out var treeObj) 
                    && treeObj is bool tree && tree;
                var showFiles = context.Parameters.TryGetValue("showFiles", out var filesObj) 
                    && filesObj is bool files && files;

                // Navigate to the target directory
                var result = await NavigateAndExplore(target, showTree, showFiles);
                
                Logger.LogInformation("CursorNavigation completed to: {Target}", target);
                
                return CreateResult(true, result, startTime: startTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error during navigation");
                return CreateResult(false, errorMessage: $"Navigation failed: {ex.Message}", startTime: startTime);
            }
        }

        private async Task<string> NavigateAndExplore(string target, bool showTree, bool showFiles)
        {
            return await Task.Run(() =>
            {
                var sb = new StringBuilder();
                
                try
                {
                    // Handle special navigation shortcuts
                    target = ProcessNavigationShortcuts(target);
                    
                    // Navigate to the directory
                    var oldDirectory = GetCurrentDirectory();
                    var newDirectory = SessionScope.ChangeDirectory(target);
                    var newRelativeDirectory = GetRelativePath(newDirectory);
                    
                    // Show navigation result
                    sb.AppendLine($"Navigation: {oldDirectory} → {newRelativeDirectory}");
                    sb.AppendLine($"Current directory: {newRelativeDirectory}");
                    sb.AppendLine();
                    
                    // Show directory contents if requested
                    if (showTree || showFiles)
                    {
                        sb.AppendLine("Directory contents:");
                        sb.AppendLine(new string('-', 30));
                        
                        var directoryInfo = new DirectoryInfo(newDirectory);
                        
                        // Show directories
                        var directories = directoryInfo.GetDirectories()
                            .OrderBy(d => d.Name)
                            .ToList();
                        
                        foreach (var dir in directories)
                        {
                            sb.AppendLine($"[DIR]  {dir.Name}");
                        }
                        
                        // Show files if requested
                        if (showFiles)
                        {
                            var files = directoryInfo.GetFiles()
                                .OrderBy(f => f.Name)
                                .ToList();
                            
                            foreach (var file in files)
                            {
                                var size = FormatFileSize(file.Length);
                                sb.AppendLine($"[FILE] {file.Name} ({size})");
                            }
                        }
                        
                        sb.AppendLine(new string('-', 30));
                        sb.AppendLine($"{directories.Count} director{(directories.Count == 1 ? "y" : "ies")}");
                        if (showFiles)
                        {
                            var fileCount = directoryInfo.GetFiles().Length;
                            sb.AppendLine($"{fileCount} file{(fileCount == 1 ? "" : "s")}");
                        }
                        sb.AppendLine();
                    }
                    
                    // Show current context
                    sb.AppendLine("--- Session Context ---");
                    sb.AppendLine($"Session ID: {SessionScope.SessionId}");
                    sb.AppendLine($"Session Root: {GetRelativePath(SessionScope.SessionRoot)}");
                    sb.AppendLine($"Working Directory: {newRelativeDirectory}");
                    
                    return sb.ToString();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to navigate and explore: {ex.Message}", ex);
                }
            });
        }

        private string ProcessNavigationShortcuts(string target)
        {
            return target switch
            {
                "~" or "home" => ".", // Go to session root
                ".." or "up" => "..", // Go up one level
                "." or "current" => ".", // Stay in current directory
                "/" or "root" => ".", // Go to session root
                _ => target // Use as-is
            };
        }
    }
}
