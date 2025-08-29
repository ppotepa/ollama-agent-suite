using Microsoft.Extensions.Logging;
using Ollama.Domain.Contracts;

namespace Ollama.Infrastructure.Interceptors;

/// <summary>
/// Console-specific interceptor that handles display formatting and session termination
/// Handles the /end command and formats output for console display
/// </summary>
public class ConsoleInterceptor : IMessageInterceptor
{
    private readonly ILogger<ConsoleInterceptor> _logger;

    public ConsoleInterceptor(ILogger<ConsoleInterceptor> logger)
    {
        _logger = logger;
    }

    public int Priority => 100; // Run after command processing

    public Task<string> InterceptInputAsync(string input, InterceptionContext context)
    {
        // Log input for debugging
        _logger.LogDebug("Console input received in session {SessionId}: {Input}", 
            context.SessionId, input);

        // Store input metadata
        context.Metadata["ConsoleInputTime"] = DateTime.UtcNow.ToString("HH:mm:ss");
        
        // Pass through unchanged - console interceptor doesn't modify input
        return Task.FromResult(input);
    }

    public Task<string> InterceptOutputAsync(string output, InterceptionContext context)
    {
        // Only display output in interactive mode
        if (!context.IsInteractive)
            return Task.FromResult(output);

        // Skip if already processed by command interceptor
        if (context.Properties.ContainsKey("SkipProcessing"))
            return Task.FromResult(output);

        try
        {
            // Format and display the response
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nResponse:");
            Console.ResetColor();
            
            // Display the actual response
            Console.WriteLine(output);
            Console.WriteLine();

            // Add timestamp to context
            context.Metadata["ConsoleOutputTime"] = DateTime.UtcNow.ToString("HH:mm:ss");

            _logger.LogDebug("Console output displayed in session {SessionId}, length: {Length}", 
                context.SessionId, output?.Length ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying console output");
            // Still return the output even if display fails
        }

        return Task.FromResult(output);
    }

    public bool ShouldContinueSession(string input, InterceptionContext context)
    {
        // Check for session termination command
        var shouldTerminate = string.Equals(input?.Trim(), "/end", StringComparison.OrdinalIgnoreCase);
        
        if (shouldTerminate)
        {
            _logger.LogInformation("Session termination requested via /end command in session {SessionId}", 
                context.SessionId);
            
            // Display termination message in interactive mode
            if (context.IsInteractive)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Ending session...");
                Console.ResetColor();
            }
        }

        return !shouldTerminate;
    }
}
