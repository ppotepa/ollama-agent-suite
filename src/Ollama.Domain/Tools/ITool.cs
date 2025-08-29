using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ollama.Domain.Tools
{
    public interface ITool
    {
        string Name { get; }
        string Description { get; }
        IEnumerable<string> Capabilities { get; }
        bool RequiresNetwork { get; }
        bool RequiresFileSystem { get; }

        /// <summary>
        /// Enhanced execution with built-in retry and fallback mechanisms
        /// </summary>
        Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Execute with retry parameters and fallback strategies
        /// </summary>
        Task<ToolResult> RunWithRetryAsync(ToolContext context, int maxRetries = 3, TimeSpan? retryDelay = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Try alternative method if primary method fails
        /// </summary>
        Task<ToolResult> TryAlternativeAsync(ToolContext context, string failureReason, CancellationToken cancellationToken = default);
        
        Task<decimal> EstimateCostAsync(ToolContext context);
        Task<bool> DryRunAsync(ToolContext context);
        
        /// <summary>
        /// Get available alternative methods for this tool
        /// </summary>
        IEnumerable<string> GetAlternativeMethods();
    }

    public class ToolContext
    {
        public IDictionary<string, object> Parameters { get; } = new Dictionary<string, object>();
        public IDictionary<string, object> State { get; } = new Dictionary<string, object>();
        public string? WorkingDirectory { get; set; }
        public string? SessionId { get; set; }
        
        /// <summary>
        /// Current retry attempt number (0 for first attempt)
        /// </summary>
        public int RetryAttempt { get; set; } = 0;
        
        /// <summary>
        /// Preferred alternative method to try if primary fails
        /// </summary>
        public string? PreferredAlternative { get; set; }
        
        /// <summary>
        /// Previous failure information for context
        /// </summary>
        public string? PreviousFailureReason { get; set; }
        
        /// <summary>
        /// Execution history for this context
        /// </summary>
        public List<ToolExecutionAttempt> ExecutionHistory { get; } = new();
    }

    public class ToolResult
    {
        public bool Success { get; set; }
        public object? Output { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal CostIncurred { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        
        /// <summary>
        /// Method used for this execution (primary, alternative1, alternative2, etc.)
        /// </summary>
        public string? MethodUsed { get; set; }
        
        /// <summary>
        /// Total attempts made including retries
        /// </summary>
        public int TotalAttempts { get; set; }
        
        /// <summary>
        /// Suggested alternative method if this one failed
        /// </summary>
        public string? SuggestedAlternative { get; set; }
        
        /// <summary>
        /// Whether the tool has more alternatives to try
        /// </summary>
        public bool HasMoreAlternatives { get; set; }
    }
    
    public class ToolExecutionAttempt
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Method { get; set; } = "primary";
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan Duration { get; set; }
        public int AttemptNumber { get; set; }
    }
}
