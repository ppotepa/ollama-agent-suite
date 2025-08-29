using Microsoft.Extensions.Logging;
using Ollama.Domain.Contracts;
using Ollama.Domain.Agents;

namespace Ollama.Infrastructure.Interactive;

/// <summary>
/// Interactive session handler that coordinates the interceptor chain with the agent
/// Acts as the main controller for interactive mode
/// </summary>
public class InteractiveSessionHandler
{
    private readonly IAgent _agent;
    private readonly IInterceptorDispatcher _dispatcher;
    private readonly ILogger<InteractiveSessionHandler> _logger;

    public InteractiveSessionHandler(
        IAgent agent,
        IInterceptorDispatcher dispatcher,
        ILogger<InteractiveSessionHandler> logger)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Start an interactive session that continues until user types /end
    /// </summary>
    public async Task StartInteractiveSessionAsync()
    {
        var sessionId = Guid.NewGuid().ToString();
        var context = new InterceptionContext 
        { 
            SessionId = sessionId,
            IsInteractive = true,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogInformation("Starting interactive session {SessionId}", sessionId);

        // Display welcome message
        DisplayWelcomeMessage(sessionId);

        bool isSessionActive = true;
        int interactionCount = 0;

        while (isSessionActive)
        {
            try
            {
                // Reset context state for this iteration
                context.Properties.Clear();
                context.Metadata.Clear();
                context.Timestamp = DateTime.UtcNow;
                interactionCount++;

                _logger.LogDebug("Starting interaction {Count} in session {SessionId}", 
                    interactionCount, sessionId);

                // Get user input
                var userInput = GetUserInput();

                if (string.IsNullOrWhiteSpace(userInput))
                    continue;

                // Check if we should continue before processing
                isSessionActive = _dispatcher.ShouldContinue(userInput, context);
                if (!isSessionActive)
                {
                    _logger.LogInformation("Session {SessionId} termination requested", sessionId);
                    break;
                }

                // Process input through interceptors
                var processedInput = await _dispatcher.ProcessInputAsync(userInput, context);

                // Skip agent processing if marked by interceptors
                if (context.Properties.ContainsKey("SkipProcessing"))
                {
                    _logger.LogDebug("Skipping agent processing for interaction {Count}", interactionCount);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(processedInput))
                {
                    _logger.LogDebug("Processed input is empty, skipping agent call");
                    continue;
                }

                // Process with agent (existing logic unchanged)
                _logger.LogDebug("Calling agent with processed input for session {SessionId}", sessionId);
                var response = await _agent.AnswerAsync(processedInput, sessionId);

                // Process output through interceptors
                await _dispatcher.ProcessOutputAsync(response, context);

                _logger.LogDebug("Completed interaction {Count} in session {SessionId}", 
                    interactionCount, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing interaction {Count} in session {SessionId}", 
                    interactionCount, sessionId);

                // Display error to user
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                if (context.Properties.ContainsKey("DebugMode") && (bool)context.Properties["DebugMode"])
                {
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                Console.ResetColor();
                Console.WriteLine();

                // Continue session despite error
            }
        }

        // Display goodbye message
        DisplayGoodbyeMessage(sessionId, interactionCount);
        
        _logger.LogInformation("Interactive session {SessionId} ended after {Count} interactions", 
            sessionId, interactionCount);
    }

    private void DisplayWelcomeMessage(string sessionId)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("┌─────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│               🤖 Interactive Mode Started                  │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────┘");
        Console.WriteLine($"Session ID: {sessionId}");
        Console.WriteLine();
        Console.WriteLine("💡 Type your queries and press Enter");
        Console.WriteLine("📋 Type '/help' for available commands");
        Console.WriteLine("🚪 Type '/end' to exit");
        Console.WriteLine();
        Console.WriteLine("Ready for your questions...");
        Console.WriteLine(new string('─', 60));
        Console.ResetColor();
        Console.WriteLine();
    }

    private string GetUserInput()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("❯ ");
        Console.ResetColor();

        var input = Console.ReadLine();
        return input ?? string.Empty;
    }

    private void DisplayGoodbyeMessage(string sessionId, int interactionCount)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("┌─────────────────────────────────────────────────────────────┐");
        Console.WriteLine("│               👋 Interactive Session Ended                 │");
        Console.WriteLine("└─────────────────────────────────────────────────────────────┘");
        Console.WriteLine($"Session ID: {sessionId}");
        Console.WriteLine($"Interactions: {interactionCount}");
        Console.WriteLine("Thank you for using Ollama Agent Suite!");
        Console.ResetColor();
        Console.WriteLine();
    }
}
