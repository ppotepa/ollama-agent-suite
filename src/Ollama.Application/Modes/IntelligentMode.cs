using Ollama.Application.Services;
using Ollama.Domain.Strategies;
using Ollama.Domain.Agents;

namespace Ollama.Application.Modes;

public sealed class IntelligentMode : IModeStrategy
{
    private readonly IAgent _agent;
    private readonly AgentSwitchService _agentSwitchService;
    private readonly ExecutionTreeBuilder _treeBuilder;

    public StrategyType Type => StrategyType.Intelligent;

    public IntelligentMode(IAgent agent, AgentSwitchService agentSwitchService, ExecutionTreeBuilder treeBuilder)
    {
        _agent = agent;
        _agentSwitchService = agentSwitchService;
        _treeBuilder = treeBuilder;
    }

    public bool CanHandle(Domain.Strategies.ExecutionContext ctx)
    {
        // Intelligent mode can handle any query, but prefers complex ones
        var query = ctx.Query.ToLowerInvariant();
        return query.Contains("what is") || 
               query.Contains("calculate") || 
               query.Contains("solve") ||
               query.Contains("analyze") ||
               query.Contains("repository") ||
               query.Contains("github.com") ||
               query.Contains("intelligent") ||
               query.Contains("think") ||
               query.Length > 20; // Longer queries might benefit from intelligent processing
    }

    public Dictionary<string, object> Execute(Domain.Strategies.ExecutionContext ctx)
    {
        var sessionId = ctx.SessionId ?? Guid.NewGuid().ToString();
        
        // Start building execution tree
        _treeBuilder.Begin(ctx.Query);

        // Use the intelligent agent to think about the problem
        var reasoning = _agent.Think(ctx.Query);
        _treeBuilder.AddAnalysis(reasoning);

        // Get the agent's plan
        var plan = _agent.Plan(ctx.Query);
        _treeBuilder.AddCommand($"Plan: {plan}");

        // Execute the plan and get the result
        var result = _agent.Answer(ctx.Query);
        _treeBuilder.AddCommand($"Execution completed");

        _treeBuilder.Finish(result);

        var executionTree = _treeBuilder.Build();

        return new Dictionary<string, object>
        {
            ["reasoning"] = reasoning,
            ["plan"] = plan,
            ["executionTree"] = executionTree,
            ["result"] = result,
            ["response"] = result, // Add response key for CLI compatibility
            ["strategy"] = Type.ToString(),
            ["sessionId"] = sessionId
        };
    }
}
