using Ollama.Domain.Contracts;

namespace Ollama.Domain.Contracts;

/// <summary>
/// Dispatcher interface to manage the interceptor chain
/// Coordinates the execution of multiple interceptors in proper order
/// </summary>
public interface IInterceptorDispatcher
{
    /// <summary>
    /// Register an interceptor in the chain
    /// Interceptors are ordered by their Priority property
    /// </summary>
    /// <param name="interceptor">Interceptor to register</param>
    void RegisterInterceptor(IMessageInterceptor interceptor);

    /// <summary>
    /// Process input through the interceptor chain
    /// </summary>
    /// <param name="input">Original user input</param>
    /// <param name="context">Interception context</param>
    /// <returns>Processed input after all interceptors</returns>
    Task<string> ProcessInputAsync(string input, InterceptionContext context);

    /// <summary>
    /// Process output through the interceptor chain
    /// </summary>
    /// <param name="output">Agent response</param>
    /// <param name="context">Interception context</param>
    /// <returns>Processed output after all interceptors</returns>
    Task<string> ProcessOutputAsync(string output, InterceptionContext context);

    /// <summary>
    /// Check if the session should continue based on interceptor evaluation
    /// </summary>
    /// <param name="input">User input to evaluate</param>
    /// <param name="context">Interception context</param>
    /// <returns>True if session should continue</returns>
    bool ShouldContinue(string input, InterceptionContext context);

    /// <summary>
    /// Get list of registered interceptors (for debugging/monitoring)
    /// </summary>
    IReadOnlyList<IMessageInterceptor> GetRegisteredInterceptors();
}
