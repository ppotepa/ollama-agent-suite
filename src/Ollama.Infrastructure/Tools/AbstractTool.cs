using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Ollama.Infrastructure.Tools
{
    /// <summary>
    /// Abstract base class for all tools providing common cursor navigation and session functionality
    /// </summary>
    public abstract class AbstractTool : ITool
    {
        protected readonly ISessionScope SessionScope;
        protected readonly ILogger Logger;

        protected AbstractTool(ISessionScope sessionScope, ILogger logger)
        {
            SessionScope = sessionScope;
            Logger = logger;
        }

        /// <summary>
        /// Ensures SessionScope is initialized with the correct sessionId from ToolContext
        /// This prevents using hardcoded default session values
        /// </summary>
        protected void EnsureSessionScopeInitialized(ToolContext context)
        {
            if (!string.IsNullOrEmpty(context.SessionId))
            {
                SessionScope.Initialize(context.SessionId);
                Logger.LogDebug("SessionScope reinitialized with sessionId: {SessionId}", context.SessionId);
            }
        }

        // ITool implementation - derived classes must implement these
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract IEnumerable<string> Capabilities { get; }
        public abstract bool RequiresNetwork { get; }
        public abstract bool RequiresFileSystem { get; }

        public abstract Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default);
        public abstract Task<decimal> EstimateCostAsync(ToolContext context);
        public abstract Task<bool> DryRunAsync(ToolContext context);

        #region Cursor Navigation Methods

        /// <summary>
        /// Gets the current working directory relative to session root
        /// </summary>
        protected string GetCurrentDirectory()
        {
            return GetRelativePath(SessionScope.WorkingDirectory);
        }

        /// <summary>
        /// Changes the current directory and returns navigation info
        /// </summary>
        protected string NavigateToDirectory(string relativePath)
        {
            try
            {
                var oldDirectory = GetCurrentDirectory();
                var newDirectory = SessionScope.ChangeDirectory(relativePath);
                var newRelativeDirectory = GetRelativePath(newDirectory);
                
                var sb = new StringBuilder();
                sb.AppendLine($"Navigation: {oldDirectory} → {newRelativeDirectory}");
                sb.AppendLine($"Current directory: {newRelativeDirectory}");
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to navigate to directory '{relativePath}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Processes cursor/navigation parameters if present in the context
        /// Returns navigation result or null if no navigation occurred
        /// </summary>
        protected string? ProcessCursorNavigation(ToolContext context)
        {
            // Check for cursor navigation parameters
            if (context.Parameters.TryGetValue("cd", out var cdObj) && 
                !string.IsNullOrWhiteSpace(cdObj?.ToString()))
            {
                return NavigateToDirectory(cdObj.ToString()!);
            }

            if (context.Parameters.TryGetValue("changeDirectory", out var changeDirObj) && 
                !string.IsNullOrWhiteSpace(changeDirObj?.ToString()))
            {
                return NavigateToDirectory(changeDirObj.ToString()!);
            }

            if (context.Parameters.TryGetValue("navigate", out var navObj) && 
                !string.IsNullOrWhiteSpace(navObj?.ToString()))
            {
                return NavigateToDirectory(navObj.ToString()!);
            }

            return null;
        }

        /// <summary>
        /// Gets a safe absolute path from a relative path using session scope
        /// </summary>
        protected string GetSafePath(string relativePath)
        {
            return SessionScope.GetSafePath(relativePath);
        }

        /// <summary>
        /// Validates that a path is within session boundaries
        /// </summary>
        protected bool IsPathValid(string path)
        {
            return SessionScope.IsPathValid(path);
        }

        /// <summary>
        /// Converts an absolute path to a relative path from session root
        /// </summary>
        protected string GetRelativePath(string fullPath)
        {
            var sessionRoot = SessionScope.SessionRoot;
            if (fullPath.StartsWith(sessionRoot, StringComparison.OrdinalIgnoreCase))
            {
                var relative = fullPath.Substring(sessionRoot.Length);
                return relative.TrimStart('\\', '/') ?? ".";
            }
            return fullPath;
        }

        /// <summary>
        /// Creates a cursor context summary for tool output
        /// </summary>
        protected string GetCursorContext()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Session: {SessionScope.SessionId}");
            sb.AppendLine($"Current directory: {GetCurrentDirectory()}");
            return sb.ToString();
        }

        #endregion

        #region Common Helper Methods

        /// <summary>
        /// Formats file size in human-readable format
        /// </summary>
        protected string FormatFileSize(long bytes)
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

        /// <summary>
        /// Creates a standard tool result with execution time
        /// </summary>
        protected ToolResult CreateResult(bool success, object? output = null, string? errorMessage = null, DateTime? startTime = null)
        {
            return new ToolResult
            {
                Success = success,
                Output = output,
                ErrorMessage = errorMessage,
                ExecutionTime = startTime.HasValue ? DateTime.Now - startTime.Value : TimeSpan.Zero
            };
        }

        /// <summary>
        /// Creates a successful tool result with cursor context included
        /// </summary>
        protected ToolResult CreateSuccessResultWithContext(string output, string? navigationResult = null, DateTime? startTime = null)
        {
            var sb = new StringBuilder();
            
            // Add navigation result if present
            if (!string.IsNullOrEmpty(navigationResult))
            {
                sb.AppendLine(navigationResult);
                sb.AppendLine();
            }
            
            // Add main output
            sb.AppendLine(output);
            
            // Add cursor context
            sb.AppendLine();
            sb.AppendLine("--- Cursor Context ---");
            sb.Append(GetCursorContext());
            
            return CreateResult(true, sb.ToString(), startTime: startTime);
        }

        /// <summary>
        /// Validates required parameters and returns missing parameter names
        /// </summary>
        protected string[] ValidateRequiredParameters(ToolContext context, params string[] requiredParameters)
        {
            var missing = new List<string>();
            
            foreach (var param in requiredParameters)
            {
                if (!context.Parameters.TryGetValue(param, out var value) || 
                    value == null || 
                    (value is string str && string.IsNullOrWhiteSpace(str)))
                {
                    missing.Add(param);
                }
            }
            
            return missing.ToArray();
        }

        #endregion
    }
}
