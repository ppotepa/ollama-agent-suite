using Ollama.Domain.Agents;
using Ollama.Domain.Strategies;

namespace Ollama.Application.Modes;

public sealed class SingleQueryMode : IModeStrategy
{
    private readonly IAgent _agent;

    public StrategyType Type => StrategyType.SingleQuery;

    public SingleQueryMode(IAgent agent)
    {
        _agent = agent;
    }

    public bool CanHandle(Domain.Strategies.ExecutionContext ctx)
    {
        // Single query mode can handle any context, but has lowest priority
        return true;
    }

    public Dictionary<string, object> Execute(Domain.Strategies.ExecutionContext ctx)
    {
        var response = _agent.Answer(ctx.Query);
        
        return new Dictionary<string, object>
        {
            ["response"] = response,
            ["strategy"] = Type.ToString(),
            ["sessionId"] = ctx.SessionId ?? Guid.NewGuid().ToString()
        };
    }
}
