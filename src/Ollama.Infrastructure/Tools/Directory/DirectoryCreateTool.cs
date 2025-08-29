using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Tools.Directory
{
    /// <summary>
    /// Directory creation tool - equivalent to 'mkdir' command
    /// Creates directories within session boundaries
    /// </summary>
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
