using Microsoft.Extensions.Logging;
using Ollama.Domain.Contracts;

namespace Ollama.Infrastructure.Interceptors;

/// <summary>
/// Dispatcher implementation that manages the interceptor chain
/// Executes interceptors in priority order and handles error recovery
/// </summary>
public class InterceptorDispatcher : IInterceptorDispatcher
{
    private readonly List<IMessageInterceptor> _interceptors = new List<IMessageInterceptor>();
    private readonly ILogger<InterceptorDispatcher> _logger;
    private readonly object _lock = new object();

    public InterceptorDispatcher(ILogger<InterceptorDispatcher> logger)
    {
        _logger = logger;
    }

    public void RegisterInterceptor(IMessageInterceptor interceptor)
    {
        if (interceptor == null)
            throw new ArgumentNullException(nameof(interceptor));

        lock (_lock)
        {
            _interceptors.Add(interceptor);
            // Sort by priority (lower numbers first)
            _interceptors.Sort((x, y) => x.Priority.CompareTo(y.Priority));
        }

        _logger.LogInformation("Registered interceptor {InterceptorType} with priority {Priority}",
            interceptor.GetType().Name, interceptor.Priority);
    }

    public async Task<string> ProcessInputAsync(string input, InterceptionContext context)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        context.OriginalInput = input;
        var currentInput = input;

        _logger.LogDebug("Processing input through {Count} interceptors", _interceptors.Count);

        // Process through each interceptor in priority order
        foreach (var interceptor in _interceptors.ToList()) // ToList to avoid modification during enumeration
        {
            try
            {
                var previousInput = currentInput;
                currentInput = await interceptor.InterceptInputAsync(currentInput, context);

                _logger.LogDebug("Interceptor {InterceptorType} processed input: '{Previous}' -> '{Current}'",
                    interceptor.GetType().Name, previousInput, currentInput);

                // Stop if an interceptor marked this for skipping
                if (context.Properties.ContainsKey("SkipProcessing"))
                {
                    _logger.LogDebug("Processing skipped by {InterceptorType}", interceptor.GetType().Name);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in input interceptor {InterceptorType}", interceptor.GetType().Name);
                // Continue with other interceptors on error
            }
        }

        return currentInput;
    }

    public async Task<string> ProcessOutputAsync(string output, InterceptionContext context)
    {
        if (string.IsNullOrEmpty(output))
            return output;

        // Skip if marked by input processing
        if (context.Properties.ContainsKey("SkipProcessing"))
        {
            _logger.LogDebug("Output processing skipped due to SkipProcessing flag");
            return output;
        }

        var currentOutput = output;

        _logger.LogDebug("Processing output through {Count} interceptors", _interceptors.Count);

        // Process through each interceptor in priority order
        foreach (var interceptor in _interceptors.ToList())
        {
            try
            {
                var previousOutput = currentOutput;
                currentOutput = await interceptor.InterceptOutputAsync(currentOutput, context);

                _logger.LogDebug("Interceptor {InterceptorType} processed output length: {PreviousLength} -> {CurrentLength}",
                    interceptor.GetType().Name, previousOutput?.Length ?? 0, currentOutput?.Length ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in output interceptor {InterceptorType}", interceptor.GetType().Name);
                // Continue with other interceptors on error
            }
        }

        return currentOutput;
    }

    public bool ShouldContinue(string input, InterceptionContext context)
    {
        if (string.IsNullOrEmpty(input))
            return true;

        // Continue only if all interceptors agree
        foreach (var interceptor in _interceptors.ToList())
        {
            try
            {
                if (!interceptor.ShouldContinueSession(input, context))
                {
                    _logger.LogInformation("Session termination requested by {InterceptorType}", interceptor.GetType().Name);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking session continuation in {InterceptorType}", interceptor.GetType().Name);
                // On error, assume continue (safer option)
            }
        }

        return true;
    }

    public IReadOnlyList<IMessageInterceptor> GetRegisteredInterceptors()
    {
        lock (_lock)
        {
            return _interceptors.ToList().AsReadOnly();
        }
    }
}
