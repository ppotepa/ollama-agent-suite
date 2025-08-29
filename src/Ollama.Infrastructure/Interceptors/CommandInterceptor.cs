using Microsoft.Extensions.Logging;
using Ollama.Domain.Contracts;

namespace Ollama.Infrastructure.Interceptors;

/// <summary>
/// Command interceptor that handles special commands like /help, /clear, etc.
/// Processes commands before they reach the agent and provides immediate responses
/// </summary>
public class CommandInterceptor : IMessageInterceptor
{
    private readonly ILogger<CommandInterceptor> _logger;

    public CommandInterceptor(ILogger<CommandInterceptor> logger)
    {
        _logger = logger;
    }

    public int Priority => 10; // Run early in the chain

    public Task<string> InterceptInputAsync(string input, InterceptionContext context)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Task.FromResult(input);

        var trimmedInput = input.Trim();

        // Handle special commands
        if (trimmedInput.StartsWith("/"))
        {
            var handled = HandleCommand(trimmedInput, context);
            if (handled)
            {
                _logger.LogInformation("Command {Command} handled in session {SessionId}", 
                    trimmedInput, context.SessionId);
                
                // Mark for skipping further processing
                context.Properties["SkipProcessing"] = true;
                return Task.FromResult(string.Empty);
            }
        }

        return Task.FromResult(input);
    }

    public Task<string> InterceptOutputAsync(string output, InterceptionContext context)
    {
        // Commands are handled in input processing, so just pass through
        return Task.FromResult(output);
    }

    public bool ShouldContinueSession(string input, InterceptionContext context)
    {
        // Let other interceptors handle session termination
        // Commands themselves don't terminate sessions (except /end, handled by ConsoleInterceptor)
        return true;
    }

    private bool HandleCommand(string command, InterceptionContext context)
    {
        switch (command.ToLowerInvariant())
        {
            case "/help":
                DisplayHelp(context);
                return true;

            case "/clear":
                ClearConsole(context);
                return true;

            case "/session":
                DisplaySessionInfo(context);
                return true;

            case "/history":
                DisplayHistory(context);
                return true;

            case "/debug":
                ToggleDebugMode(context);
                return true;

            case "/commands":
                DisplayAvailableCommands(context);
                return true;

            default:
                // Unknown command - let it pass to the agent
                if (context.IsInteractive)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Unknown command: {command}. Type /help for available commands.");
                    Console.ResetColor();
                }
                return true;
        }
    }

    private void DisplayHelp(InterceptionContext context)
    {
        if (!context.IsInteractive) return;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=== Interactive Mode Help ===");
        Console.WriteLine("Available commands:");
        Console.WriteLine("  /end       - End the interactive session");
        Console.WriteLine("  /clear     - Clear the console screen");
        Console.WriteLine("  /help      - Show this help text");
        Console.WriteLine("  /session   - Show current session information");
        Console.WriteLine("  /history   - Show conversation history");
        Console.WriteLine("  /debug     - Toggle debug mode");
        Console.WriteLine("  /commands  - List all available commands");
        Console.WriteLine("\nFor regular queries, just type your question and press Enter.");
        Console.ResetColor();
        Console.WriteLine();
    }

    private void ClearConsole(InterceptionContext context)
    {
        if (!context.IsInteractive) return;

        if (OperatingSystem.IsWindows())
        {
            Console.Clear();
        }
        else
        {
            // ANSI escape code for clear screen (Linux/macOS)
            Console.Write("\x1b[2J\x1b[H");
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Console cleared. Session continues...");
        Console.ResetColor();
        Console.WriteLine();
    }

    private void DisplaySessionInfo(InterceptionContext context)
    {
        if (!context.IsInteractive) return;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=== Session Information ===");
        Console.WriteLine($"Session ID: {context.SessionId}");
        Console.WriteLine($"Started: {context.Timestamp:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Interactive Mode: {context.IsInteractive}");
        
        if (context.Properties.Any())
        {
            Console.WriteLine("Properties:");
            foreach (var prop in context.Properties)
            {
                Console.WriteLine($"  {prop.Key}: {prop.Value}");
            }
        }

        if (context.Metadata.Any())
        {
            Console.WriteLine("Metadata:");
            foreach (var meta in context.Metadata)
            {
                Console.WriteLine($"  {meta.Key}: {meta.Value}");
            }
        }

        Console.ResetColor();
        Console.WriteLine();
    }

    private void DisplayHistory(InterceptionContext context)
    {
        if (!context.IsInteractive) return;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=== Conversation History ===");
        Console.WriteLine("History tracking is not yet implemented.");
        Console.WriteLine("This will show previous queries and responses in a future version.");
        Console.ResetColor();
        Console.WriteLine();
    }

    private void ToggleDebugMode(InterceptionContext context)
    {
        if (!context.IsInteractive) return;

        var currentDebugMode = context.Properties.ContainsKey("DebugMode") && 
                              (bool)context.Properties["DebugMode"];
        
        var newDebugMode = !currentDebugMode;
        context.Properties["DebugMode"] = newDebugMode;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Debug mode {(newDebugMode ? "enabled" : "disabled")}.");
        Console.ResetColor();
        Console.WriteLine();
    }

    private void DisplayAvailableCommands(InterceptionContext context)
    {
        if (!context.IsInteractive) return;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n=== Available Commands ===");
        Console.WriteLine("/help      - Show detailed help");
        Console.WriteLine("/clear     - Clear console");
        Console.WriteLine("/session   - Session info");
        Console.WriteLine("/history   - Conversation history");
        Console.WriteLine("/debug     - Toggle debug mode");
        Console.WriteLine("/commands  - This list");
        Console.WriteLine("/end       - Exit session");
        Console.ResetColor();
        Console.WriteLine();
    }
}
