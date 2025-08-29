using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Ollama.Domain.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Ollama.Infrastructure.Tools
{
    /// <summary>
    /// Enhanced abstract base class for all tools providing retry logic and alternative method support
    /// </summary>
    public abstract class AbstractTool : ITool
    {
        protected readonly ISessionScope SessionScope;
        protected readonly ILogger Logger;
        protected readonly ICursorConfigurationService? CursorConfigurationService;

        protected AbstractTool(ISessionScope sessionScope, ILogger logger, ICursorConfigurationService? cursorConfigurationService = null)
        {
            SessionScope = sessionScope;
            Logger = logger;
            CursorConfigurationService = cursorConfigurationService;
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

        /// <summary>
        /// Primary execution method - derived classes implement this
        /// </summary>
        public abstract Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Enhanced execution with built-in retry and fallback mechanisms
        /// </summary>
        public virtual async Task<ToolResult> RunWithRetryAsync(ToolContext context, int maxRetries = 3, TimeSpan? retryDelay = null, CancellationToken cancellationToken = default)
        {
            var delay = retryDelay ?? TimeSpan.FromSeconds(1);
            var totalStartTime = DateTime.UtcNow;
            ToolResult? lastResult = null;
            
            Logger.LogInformation("Tool {ToolName}: Starting execution with retry (max {MaxRetries} attempts)", 
                Name, maxRetries);

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                context.RetryAttempt = attempt;
                var attemptStartTime = DateTime.UtcNow;
                
                try
                {
                    Logger.LogDebug("Tool {ToolName}: Attempt {Attempt}/{MaxAttempts}", 
                        Name, attempt + 1, maxRetries + 1);
                    
                    // Try primary method
                    var result = await RunAsync(context, cancellationToken);
                    result.TotalAttempts = attempt + 1;
                    result.MethodUsed = "primary";
                    
                    // Record attempt
                    var attemptRecord = new ToolExecutionAttempt
                    {
                        Method = "primary",
                        Success = result.Success,
                        ErrorMessage = result.ErrorMessage,
                        Duration = DateTime.UtcNow - attemptStartTime,
                        AttemptNumber = attempt + 1
                    };
                    context.ExecutionHistory.Add(attemptRecord);
                    
                    if (result.Success)
                    {
                        Logger.LogInformation("Tool {ToolName}: Succeeded on attempt {Attempt}", 
                            Name, attempt + 1);
                        return result;
                    }
                    
                    lastResult = result;
                    Logger.LogWarning("Tool {ToolName}: Attempt {Attempt} failed: {Error}", 
                        Name, attempt + 1, result.ErrorMessage);
                    
                    // If we have alternatives and this is the last retry, try alternatives
                    if (attempt == maxRetries && GetAlternativeMethods().Any())
                    {
                        Logger.LogInformation("Tool {ToolName}: Trying alternative methods after {Attempts} failed attempts", 
                            Name, attempt + 1);
                        
                        var alternativeResult = await TryAlternativeAsync(context, result.ErrorMessage ?? "Unknown error", cancellationToken);
                        alternativeResult.TotalAttempts = attempt + 1;
                        
                        if (alternativeResult.Success)
                        {
                            return alternativeResult;
                        }
                        
                        // Combine error messages
                        alternativeResult.ErrorMessage = $"Primary method failed: {result.ErrorMessage}. Alternative method failed: {alternativeResult.ErrorMessage}";
                        return alternativeResult;
                    }
                    
                    // Wait before retry (except on last attempt)
                    if (attempt < maxRetries)
                    {
                        Logger.LogDebug("Tool {ToolName}: Waiting {Delay}ms before retry", 
                            Name, delay.TotalMilliseconds);
                        await Task.Delay(delay, cancellationToken);
                        
                        // Exponential backoff
                        delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 1.5);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Tool {ToolName}: Exception on attempt {Attempt}: {Error}", 
                        Name, attempt + 1, ex.Message);
                    
                    var attemptRecord = new ToolExecutionAttempt
                    {
                        Method = "primary",
                        Success = false,
                        ErrorMessage = ex.Message,
                        Duration = DateTime.UtcNow - attemptStartTime,
                        AttemptNumber = attempt + 1
                    };
                    context.ExecutionHistory.Add(attemptRecord);
                    
                    lastResult = new ToolResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message,
                        ExecutionTime = DateTime.UtcNow - totalStartTime,
                        TotalAttempts = attempt + 1,
                        MethodUsed = "primary"
                    };
                    
                    // If this is the last attempt, try alternatives
                    if (attempt == maxRetries && GetAlternativeMethods().Any())
                    {
                        try
                        {
                            var alternativeResult = await TryAlternativeAsync(context, ex.Message, cancellationToken);
                            if (alternativeResult.Success)
                            {
                                return alternativeResult;
                            }
                        }
                        catch (Exception altEx)
                        {
                            Logger.LogError(altEx, "Tool {ToolName}: Alternative method also failed", Name);
                            lastResult.ErrorMessage += $". Alternative method failed: {altEx.Message}";
                        }
                    }
                    
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(delay, cancellationToken);
                        delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 1.5);
                    }
                }
            }
            
            Logger.LogError("Tool {ToolName}: All {MaxRetries} attempts failed", Name, maxRetries + 1);
            return lastResult ?? new ToolResult
            {
                Success = false,
                ErrorMessage = "All retry attempts failed",
                ExecutionTime = DateTime.UtcNow - totalStartTime,
                TotalAttempts = maxRetries + 1,
                MethodUsed = "primary"
            };
        }
        
        /// <summary>
        /// Try alternative method if primary method fails
        /// Default implementation tries each alternative method in order
        /// </summary>
        public virtual async Task<ToolResult> TryAlternativeAsync(ToolContext context, string failureReason, CancellationToken cancellationToken = default)
        {
            var alternatives = GetAlternativeMethods().ToList();
            
            if (!alternatives.Any())
            {
                Logger.LogDebug("Tool {ToolName}: No alternative methods available", Name);
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"No alternative methods available. Original error: {failureReason}",
                    MethodUsed = "none",
                    HasMoreAlternatives = false
                };
            }
            
            context.PreviousFailureReason = failureReason;
            
            foreach (var alternative in alternatives)
            {
                var attemptStartTime = DateTime.UtcNow;
                Logger.LogInformation("Tool {ToolName}: Trying alternative method: {Alternative}", Name, alternative);
                
                try
                {
                    var result = await RunAlternativeMethodAsync(context, alternative, cancellationToken);
                    result.MethodUsed = alternative;
                    result.HasMoreAlternatives = alternatives.IndexOf(alternative) < alternatives.Count - 1;
                    
                    // Record attempt
                    var attemptRecord = new ToolExecutionAttempt
                    {
                        Method = alternative,
                        Success = result.Success,
                        ErrorMessage = result.ErrorMessage,
                        Duration = DateTime.UtcNow - attemptStartTime,
                        AttemptNumber = context.ExecutionHistory.Count + 1
                    };
                    context.ExecutionHistory.Add(attemptRecord);
                    
                    if (result.Success)
                    {
                        Logger.LogInformation("Tool {ToolName}: Alternative method {Alternative} succeeded", Name, alternative);
                        return result;
                    }
                    
                    Logger.LogWarning("Tool {ToolName}: Alternative method {Alternative} failed: {Error}", 
                        Name, alternative, result.ErrorMessage);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Tool {ToolName}: Alternative method {Alternative} threw exception: {Error}", 
                        Name, alternative, ex.Message);
                    
                    var attemptRecord = new ToolExecutionAttempt
                    {
                        Method = alternative,
                        Success = false,
                        ErrorMessage = ex.Message,
                        Duration = DateTime.UtcNow - attemptStartTime,
                        AttemptNumber = context.ExecutionHistory.Count + 1
                    };
                    context.ExecutionHistory.Add(attemptRecord);
                }
            }
            
            Logger.LogError("Tool {ToolName}: All alternative methods failed", Name);
            return new ToolResult
            {
                Success = false,
                ErrorMessage = $"All alternative methods failed. Original error: {failureReason}",
                MethodUsed = "alternatives_exhausted",
                HasMoreAlternatives = false
            };
        }
        
        /// <summary>
        /// Execute a specific alternative method - derived classes can override this
        /// </summary>
        protected virtual Task<ToolResult> RunAlternativeMethodAsync(ToolContext context, string methodName, CancellationToken cancellationToken = default)
        {
            // Default implementation just returns the same method
            // Derived classes should override this to implement actual alternative methods
            Logger.LogWarning("Tool {ToolName}: Alternative method {Method} not implemented, falling back to primary", 
                Name, methodName);
            return RunAsync(context, cancellationToken);
        }
        
        /// <summary>
        /// Get available alternative methods - derived classes should override this
        /// </summary>
        public virtual IEnumerable<string> GetAlternativeMethods()
        {
            return Enumerable.Empty<string>();
        }

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
        /// Converts an absolute path to a relative path from session root with security controls
        /// </summary>
        protected string GetRelativePath(string fullPath)
        {
            var sessionRoot = SessionScope.SessionRoot;
            
            // Use the cursor configuration service if available
            if (CursorConfigurationService != null)
            {
                return CursorConfigurationService.FormatPath(fullPath, sessionRoot);
            }
            
            // Fallback to secure default behavior if service is not available
            if (fullPath.StartsWith(sessionRoot, StringComparison.OrdinalIgnoreCase))
            {
                var relative = fullPath.Substring(sessionRoot.Length);
                var trimmed = relative.TrimStart('\\', '/');
                return string.IsNullOrEmpty(trimmed) ? "." : trimmed;
            }
            else
            {
                // Default to masking external paths for security
                return "[EXTERNAL_PATH]";
            }
        }

        /// <summary>
        /// Gets a secure display path that respects cursor settings and session boundaries
        /// </summary>
        protected string GetSecureDisplayPath(string fullPath, string context = "")
        {
            var relativePath = GetRelativePath(fullPath);
            
            // Add debug context if cursor configuration service has debug enabled
            if (CursorConfigurationService?.Settings.IncludeDebugPaths == true && !string.IsNullOrEmpty(context))
            {
                return $"{relativePath} ({context})";
            }
            
            return relativePath;
        }

        /// <summary>
        /// Creates a cursor context summary for tool output
        /// <summary>
        /// Creates a cursor context summary for tool output
        /// </summary>
        protected string GetCursorContext()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"Session: {SessionScope.SessionId}");
            sb.AppendLine($"Current directory: {GetCurrentDirectory()}");
            
            if (CursorConfigurationService?.Settings.IncludeDebugPaths == true)
            {
                sb.AppendLine($"Session root: {GetRelativePath(SessionScope.SessionRoot)}");
            }
            
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
