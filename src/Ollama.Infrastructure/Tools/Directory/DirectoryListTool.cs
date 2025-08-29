using Ollama.Domain.Tools;
using Ollama.Domain.Tools.Attributes;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Ollama.Infrastructure.Tools.Directory
{
    /// <summary>
    /// Directory listing tool - equivalent to 'dir' command
    /// Lists contents of directories within session boundaries with cursor navigation support
    /// </summary>
    [ToolDescription(
        "Lists directory contents within session boundaries",
        "Equivalent to 'dir' or 'ls' command. Displays file and directory listings with detailed information including sizes, dates, and attributes. Supports cursor navigation.",
        "Directory Operations")]
    [ToolUsage(
        "List and examine directory contents",
        SecondaryUseCases = new[] { 
            "File exploration", 
            "Directory browsing", 
            "Content inspection", 
            "Workspace navigation",
            // Backend Development Navigation
            "Project structure exploration",
            "Source code directory browsing (src/, Controllers/, Services/)",
            "Configuration directory examination (config/, appsettings/)",
            "Build output directory inspection (bin/, obj/, dist/)",
            "Package directory exploration (packages/, node_modules/)",
            "Test project navigation (tests/, UnitTests/, IntegrationTests/)",
            // Architecture Layer Navigation
            "Clean architecture layer browsing (Domain/, Application/, Infrastructure/)",
            "Microservice directory exploration",
            "API project structure inspection",
            "Shared library directory browsing",
            // Database Development Navigation
            "Migration directory listing (Migrations/, Scripts/)",
            "Entity model directory browsing (Models/, Entities/)",
            "Database configuration inspection",
            "Seed data directory exploration",
            // DevOps Directory Navigation
            "Docker configuration browsing (.docker/, containers/)",
            "CI/CD pipeline directory inspection (.github/, .azure/)",
            "Infrastructure code navigation (terraform/, k8s/)",
            "Deployment script directory exploration",
            // Testing Infrastructure Navigation
            "Test category directory browsing (unit/, integration/, e2e/)",
            "Test data directory inspection (TestData/, Fixtures/)",
            "Mock directory exploration (Mocks/, Stubs/)",
            "Performance test directory navigation",
            // Documentation Navigation
            "Documentation structure browsing (docs/, api-docs/)",
            "Architecture documentation inspection",
            "README and guide exploration",
            // Security & Configuration Navigation
            "Security configuration inspection (certs/, keys/)",
            "Environment configuration browsing (environments/)",
            "Logging configuration exploration",
            // Build & Release Navigation
            "Build script directory inspection (build/, scripts/)",
            "Release artifact exploration (release/, publish/)",
            "Package output directory browsing"
        },
        RequiredParameters = new string[0],
        OptionalParameters = new[] { "path", "cd", "recursive", "showHidden" },
        ExampleInvocation = "DirectoryList with path=\".\" to list current directory",
        ExpectedOutput = "Formatted directory listing with file details",
        RequiresFileSystem = true,
        RequiresNetwork = false,
        SafetyNotes = "Read-only operation within session boundaries",
        PerformanceNotes = "Large directories may take time to enumerate")]
    [ToolCapabilities(
        ToolCapability.DirectoryList | ToolCapability.CursorNavigation | ToolCapability.PathResolution | 
        ToolCapability.BackendDevelopment | ToolCapability.DatabaseDevelopment | 
        ToolCapability.DevOpsDevelopment | ToolCapability.TestingInfrastructure | 
        ToolCapability.ArchitecturalPatterns,
        FallbackStrategy = "Basic file enumeration if advanced listing fails")]
    public class DirectoryListTool : AbstractTool
    {
        public override string Name => "DirectoryList";
        public override string Description => "Lists directory contents (equivalent to 'dir' or 'ls' command)";
        public override IEnumerable<string> Capabilities => new[] { "dir:list", "directory:contents", "fs:explore", "cursor:navigate", "directory:analyze", "fs:ls" };
        public override bool RequiresNetwork => false;
        public override bool RequiresFileSystem => true;

        public DirectoryListTool(ISessionScope sessionScope, ILogger<DirectoryListTool> logger) 
            : base(sessionScope, logger)
        {
        }

        public override Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(true); // Can always list current directory
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for directory listing
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
                
                // Get path parameter or use current directory
                var path = context.Parameters.TryGetValue("path", out var pathObj) 
                    ? pathObj?.ToString() ?? SessionScope.WorkingDirectory
                    : SessionScope.WorkingDirectory;

                // Get options
                var includeHidden = context.Parameters.TryGetValue("includeHidden", out var hiddenObj) 
                    && hiddenObj is bool hidden && hidden;
                var recursive = context.Parameters.TryGetValue("recursive", out var recursiveObj) 
                    && recursiveObj is bool rec && rec;
                var sortBy = context.Parameters.TryGetValue("sortBy", out var sortObj) 
                    ? sortObj?.ToString() ?? "name" 
                    : "name";

                // Get safe path within session
                var safePath = GetSafePath(path);
                
                if (!System.IO.Directory.Exists(safePath))
                {
                    return CreateResult(false, errorMessage: $"Directory not found: {path}", startTime: startTime);
                }

                var result = await ListDirectoryContents(safePath, includeHidden, recursive, sortBy);
                
                Logger.LogInformation("DirectoryList completed for path: {Path}", path);
                
                return CreateSuccessResultWithContext(result, navigationResult, startTime);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error listing directory contents");
                return CreateResult(false, errorMessage: $"Directory listing failed: {ex.Message}", startTime: startTime);
            }
        }

        private async Task<string> ListDirectoryContents(string path, bool includeHidden, bool recursive, string sortBy)
        {
            return await Task.Run(() =>
            {
                var sb = new StringBuilder();
                var directoryInfo = new DirectoryInfo(path);
                
                sb.AppendLine($"Directory: {GetRelativePath(path)}");
                sb.AppendLine();
                
                // Get directories
                var directories = directoryInfo.GetDirectories()
                    .Where(d => includeHidden || !d.Attributes.HasFlag(FileAttributes.Hidden))
                    .ToList();
                
                // Get files
                var files = directoryInfo.GetFiles()
                    .Where(f => includeHidden || !f.Attributes.HasFlag(FileAttributes.Hidden))
                    .ToList();
                
                // Sort directories and files
                directories = SortDirectories(directories, sortBy);
                files = SortFiles(files, sortBy);
                
                // Display directories
                foreach (var dir in directories)
                {
                    sb.AppendLine($"<DIR>        {dir.LastWriteTime:yyyy-MM-dd HH:mm}    {dir.Name}");
                }
                
                // Display files
                foreach (var file in files)
                {
                    var size = FormatFileSize(file.Length);
                    sb.AppendLine($"{size,12} {file.LastWriteTime:yyyy-MM-dd HH:mm}    {file.Name}");
                }
                
                sb.AppendLine();
                sb.AppendLine($"    {directories.Count} Dir(s)");
                sb.AppendLine($"    {files.Count} File(s)");
                
                // Calculate total size
                var totalSize = files.Sum(f => f.Length);
                sb.AppendLine($"    {FormatFileSize(totalSize)} total");
                
                // If recursive, process subdirectories
                if (recursive)
                {
                    foreach (var dir in directories)
                    {
                        try
                        {
                            sb.AppendLine();
                            sb.AppendLine($"--- Contents of {GetRelativePath(dir.FullName)} ---");
                            var subContent = ListDirectoryContents(dir.FullName, includeHidden, false, sortBy).Result;
                            sb.AppendLine(subContent);
                        }
                        catch (Exception ex)
                        {
                            sb.AppendLine($"Error accessing {dir.Name}: {ex.Message}");
                        }
                    }
                }
                
                return sb.ToString();
            });
        }

        private List<DirectoryInfo> SortDirectories(List<DirectoryInfo> directories, string sortBy)
        {
            return sortBy.ToLower() switch
            {
                "name" => directories.OrderBy(d => d.Name).ToList(),
                "date" or "modified" => directories.OrderByDescending(d => d.LastWriteTime).ToList(),
                "created" => directories.OrderByDescending(d => d.CreationTime).ToList(),
                _ => directories.OrderBy(d => d.Name).ToList()
            };
        }

        private List<FileInfo> SortFiles(List<FileInfo> files, string sortBy)
        {
            return sortBy.ToLower() switch
            {
                "name" => files.OrderBy(f => f.Name).ToList(),
                "size" => files.OrderByDescending(f => f.Length).ToList(),
                "date" or "modified" => files.OrderByDescending(f => f.LastWriteTime).ToList(),
                "created" => files.OrderByDescending(f => f.CreationTime).ToList(),
                "extension" => files.OrderBy(f => f.Extension).ThenBy(f => f.Name).ToList(),
                _ => files.OrderBy(f => f.Name).ToList()
            };
        }
    }
}
