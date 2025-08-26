using Ollama.Application.Services;
using Ollama.Domain.Strategies;
using ExecutionContext = Ollama.Domain.Strategies.ExecutionContext;

namespace Ollama.Application.Modes;

/// <summary>
/// Default mode that provides a straightforward way to execute prompts.
/// This mode uses the Intelligent strategy as the underlying implementation
/// and is designed to be the fallback when no specific mode is requested.
/// </summary>
public sealed class DefaultMode : IModeStrategy
{
    private readonly IntelligentMode _intelligentMode;

    public StrategyType Type => StrategyType.Default;

    public DefaultMode(IntelligentMode intelligentMode)
    {
        _intelligentMode = intelligentMode ?? throw new ArgumentNullException(nameof(intelligentMode));
    }

    /// <summary>
    /// Default mode can handle any query when no other specific mode is chosen.
    /// This is the most permissive handler and should be used as a fallback.
    /// </summary>
    /// <param name="ctx">The execution context containing the query and parameters</param>
    /// <returns>True if this is a default execution (no specific mode requested), false otherwise</returns>
    public bool CanHandle(ExecutionContext ctx)
    {
        // Default mode handles cases where:
        // 1. No specific mode is explicitly requested
        // 2. The query doesn't match other specialized modes
        // 3. User just wants to execute a straightforward prompt
        
        // Check if any specific mode indicators are present
        var query = ctx.Query.ToLowerInvariant();
        
        // If query contains specific mode keywords, let other modes handle it
        if (query.Contains("collaborate") || 
            query.Contains("collaboration") ||
            query.Contains("multi-agent") ||
            query.Contains("team"))
        {
            return false; // Let collaborative mode handle this
        }

        if (query.Contains("single") || 
            query.Contains("simple") ||
            query.Contains("quick"))
        {
            return false; // Let single query mode handle this
        }

        // Check if mode is explicitly specified in context metadata
        if (ctx.Metadata.ContainsKey("mode") && 
            !string.Equals(ctx.Metadata["mode"]?.ToString(), "default", StringComparison.OrdinalIgnoreCase))
        {
            return false; // Specific mode requested
        }

        // Default mode is a catch-all for general queries
        return true;
    }

    /// <summary>
    /// Executes the query using the intelligent mode strategy.
    /// This provides the best balance of capability and simplicity for general use.
    /// </summary>
    /// <param name="ctx">The execution context</param>
    /// <returns>Execution results from the intelligent mode</returns>
    public Dictionary<string, object> Execute(ExecutionContext ctx)
    {
        // Add metadata to indicate this was executed via default mode
        var result = _intelligentMode.Execute(ctx);
        
        // Add default mode metadata
        result["ExecutedViaDefaultMode"] = true;
        result["UnderlyingStrategy"] = StrategyType.Intelligent.ToString();
        result["DefaultModeTimestamp"] = DateTime.UtcNow;
        
        return result;
    }
}
