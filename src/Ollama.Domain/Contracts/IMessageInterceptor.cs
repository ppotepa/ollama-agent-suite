using System.ComponentModel.DataAnnotations;

namespace Ollama.Domain.Contracts;

/// <summary>
/// Primary interceptor interface for data exchange interception
/// Follows the Interceptor pattern to handle input/output without modifying core business logic
/// </summary>
public interface IMessageInterceptor
{
    /// <summary>
    /// Intercept and potentially transform input before it reaches the agent
    /// </summary>
    /// <param name="input">Original input from user</param>
    /// <param name="context">Interception context containing session data</param>
    /// <returns>Transformed input or original input</returns>
    Task<string> InterceptInputAsync(string input, InterceptionContext context);

    /// <summary>
    /// Intercept and potentially transform output before it reaches the user
    /// </summary>
    /// <param name="output">Response from agent</param>
    /// <param name="context">Interception context containing session data</param>
    /// <returns>Transformed output or original output</returns>
    Task<string> InterceptOutputAsync(string output, InterceptionContext context);

    /// <summary>
    /// Determine if the session should continue based on the input
    /// </summary>
    /// <param name="input">User input to evaluate</param>
    /// <param name="context">Interception context</param>
    /// <returns>True if session should continue, false otherwise</returns>
    bool ShouldContinueSession(string input, InterceptionContext context);

    /// <summary>
    /// Priority order for this interceptor (lower numbers execute first)
    /// </summary>
    int Priority { get; }
}

/// <summary>
/// Context object passed through the interceptor chain
/// </summary>
public class InterceptionContext
{
    /// <summary>
    /// Unique session identifier
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Properties bag for sharing data between interceptors
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Timestamp of the current interaction
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if this is running in interactive mode
    /// </summary>
    public bool IsInteractive { get; set; } = true;

    /// <summary>
    /// Original user input (preserved throughout the chain)
    /// </summary>
    public string OriginalInput { get; set; } = string.Empty;

    /// <summary>
    /// Metadata about the current interaction
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}
