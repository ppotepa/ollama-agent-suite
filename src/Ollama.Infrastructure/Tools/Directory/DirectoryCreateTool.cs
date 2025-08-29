using Ollama.Domain.Tools;
using Ollama.Domain.Tools.Attributes;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Tools.Directory
{
    /// <summary>
    /// Directory creation tool - equivalent to 'mkdir' command
    /// Creates directories within session boundaries
    /// </summary>
    [ToolDescription(
        "Creates directories within session boundaries",
        "Equivalent to 'mkdir' command. Creates single directories or directory hierarchies with support for recursive creation of parent directories.",
        "Directory Operations")]
    [ToolUsage(
        "Create new directories in the session workspace",
        SecondaryUseCases = new[] { 
            "Folder creation", 
            "Directory structure setup", 
            "Workspace organization", 
            "Path preparation",
            // Backend Project Structure
            "Project root directory creation",
            "Source code folder structure (src/, Controllers/, Services/, Models/)",
            "Test project organization (tests/, UnitTests/, IntegrationTests/)",
            "Configuration directories (config/, appsettings/)",
            "Static content folders (wwwroot/, assets/, images/)",
            "Data layer organization (Data/, Repositories/, Migrations/)",
            // Clean Architecture Structure
            "Domain layer directory (Domain/, Entities/, ValueObjects/)",
            "Application layer directory (Application/, Commands/, Queries/)",
            "Infrastructure layer directory (Infrastructure/, Persistence/, Services/)",
            "Presentation layer directory (API/, Controllers/, DTOs/)",
            // DevOps Structure
            "Docker configuration directories (.docker/, scripts/)",
            "CI/CD pipeline directories (.github/, .azure/, pipelines/)",
            "Documentation structure (docs/, api-docs/, architecture/)",
            "Build and deployment directories (build/, deploy/, release/)",
            // Testing Organization
            "Test category directories (unit/, integration/, e2e/)",
            "Test data directories (TestData/, Fixtures/, Mocks/)",
            "Performance test structure (PerformanceTests/, LoadTests/)",
            // Database Organization
            "Migration directories (Migrations/, Scripts/, Seed/)",
            "Schema organization (Schemas/, Views/, Procedures/)",
            // Security and Configuration
            "Certificate storage (certs/, keys/)",
            "Environment configuration (environments/, configs/)",
            "Logging directories (logs/, audit/)"
        },
        RequiredParameters = new[] { "path" },
        OptionalParameters = new[] { "cd", "recursive" },
        ExampleInvocation = "DirectoryCreate with path=\"new-folder\" to create directory",
        ExpectedOutput = "Successfully created directory at specified path",
        RequiresFileSystem = true,
        RequiresNetwork = false,
        SafetyNotes = "All operations within session boundaries",
        PerformanceNotes = "Fast operation for single directories")]
    [ToolCapabilities(
        ToolCapability.DirectoryCreate | ToolCapability.CursorNavigation | 
        ToolCapability.BackendDevelopment | ToolCapability.TestingInfrastructure | 
        ToolCapability.DevOpsDevelopment | ToolCapability.DatabaseDevelopment,
        FallbackStrategy = "Recursive creation if direct creation fails")]
    public class DirectoryCreateTool : AbstractTool
    {
        public override string Name => "DirectoryCreate";
        public override string Description => "Creates directories (equivalent to 'mkdir' command). Parameters: path (required) - the directory path to create";
        public override IEnumerable<string> Capabilities => new[] { "dir:create", "directory:make", "fs:mkdir", "folder:create" };
        public override bool RequiresNetwork => false;
        public override bool RequiresFileSystem => true;

        public DirectoryCreateTool(ISessionScope sessionScope, ILogger<DirectoryCreateTool> logger)
            : base(sessionScope, logger)
        {
        }

        public override Task<bool> DryRunAsync(ToolContext context)
        {
            var missing = ValidateRequiredParameters(context, "path");
            if (missing.Length > 0)
            {
                return Task.FromResult(false);
            }

            var path = context.Parameters["path"]?.ToString()!;
            return Task.FromResult(SessionScope.IsPathValid(path));
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for directory creation
        }

        public override Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                // Ensure SessionScope is initialized with correct sessionId from context
                EnsureSessionScopeInitialized(context);
                
                var missing = ValidateRequiredParameters(context, "path");
                if (missing.Length > 0)
                {
                    return Task.FromResult(CreateResult(false, errorMessage: "Path parameter is required", startTime: startTime));
                }

                var path = context.Parameters["path"]?.ToString()!;
                var safePath = GetSafePath(path);

                if (System.IO.Directory.Exists(safePath))
                {
                    return Task.FromResult(CreateSuccessResultWithContext("Directory already exists", startTime: startTime));
                }

                System.IO.Directory.CreateDirectory(safePath);

                Logger.LogInformation("DirectoryCreate completed for path: {Path}", path);

                return Task.FromResult(CreateSuccessResultWithContext($"Directory created successfully: {path}", startTime: startTime));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error creating directory");
                return Task.FromResult(CreateResult(false, errorMessage: $"Directory creation failed: {ex.Message}", startTime: startTime));
            }
        }
    }
}
