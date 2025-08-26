using Ollama.Application.Modes;
using Ollama.Domain.Strategies;

namespace Ollama.Application.Orchestrator;

public sealed class StrategyOrchestrator
{
    private readonly ModeRegistry _registry;
    private readonly Dictionary<string, Dictionary<string, object>> _sessions = new();

    public StrategyOrchestrator(ModeRegistry registry)
    {
        _registry = registry;
    }

    public string ExecuteQuery(string query, string? mode = null)
    {
        var context = new Domain.Strategies.ExecutionContext(query);
        
        IModeStrategy strategy;
        
        if (mode != null && Enum.TryParse<StrategyType>(mode, true, out var requestedType))
        {
            strategy = _registry.GetStrategy(requestedType);
        }
        else
        {
            strategy = _registry.SelectBestStrategy(context);
        }

        var result = strategy.Execute(context);
        var sessionId = result["sessionId"].ToString() ?? Guid.NewGuid().ToString();
        
        _sessions[sessionId] = result;
        
        return sessionId;
    }

    public Dictionary<string, object>? GetSession(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }

    public IEnumerable<string> GetAllSessionIds()
    {
        return _sessions.Keys;
    }

    public void ClearSession(string sessionId)
    {
        _sessions.Remove(sessionId);
    }

    public void ClearAllSessions()
    {
        _sessions.Clear();
    }
}
