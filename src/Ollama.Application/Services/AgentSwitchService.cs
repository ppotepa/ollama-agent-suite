namespace Ollama.Application.Services;

public sealed class AgentSwitchService
{
    private readonly Dictionary<string, object> _registry = new();

    public void RegisterAgent(string name, object agent)
    {
        _registry[name] = agent;
    }

    public T? GetAgent<T>(string name) where T : class
    {
        return _registry.TryGetValue(name, out var agent) ? agent as T : null;
    }

    public bool HasAgent(string name)
    {
        return _registry.ContainsKey(name);
    }

    public IEnumerable<string> GetRegisteredAgentNames()
    {
        return _registry.Keys;
    }
}
