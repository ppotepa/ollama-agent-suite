namespace Ollama.Domain.Strategies;

public sealed class ExecutionContext
{
    public string Query { get; }
    public string? SessionId { get; }
    public Dictionary<string, object> Metadata { get; } = new();
    public Dictionary<string, object> Intermediate { get; } = new();

    public ExecutionContext(string query, string? sessionId = null, Dictionary<string, object>? metadata = null)
    {
        Query = query;
        SessionId = sessionId;
        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                Metadata[kvp.Key] = kvp.Value;
            }
        }
    }
}
