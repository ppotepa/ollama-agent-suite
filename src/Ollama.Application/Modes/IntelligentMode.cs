using Ollama.Application.Services;
using Ollama.Domain.Strategies;

namespace Ollama.Application.Modes;

public sealed class IntelligentMode : IModeStrategy
{
    private readonly object _thinker; // keep generic; only needs a Think method
    private readonly AgentSwitchService _agentSwitchService;
    private readonly ExecutionTreeBuilder _treeBuilder;

    public StrategyType Type => StrategyType.Intelligent;

    public IntelligentMode(object thinker, AgentSwitchService agentSwitchService, ExecutionTreeBuilder treeBuilder)
    {
        _thinker = thinker;
        _agentSwitchService = agentSwitchService;
        _treeBuilder = treeBuilder;
    }

    public bool CanHandle(Domain.Strategies.ExecutionContext ctx)
    {
        // Check if query indicates complex autonomous work
        var query = ctx.Query.ToLowerInvariant();
        return query.Contains("debug") || 
               query.Contains("autonomous") || 
               query.Contains("intelligent") ||
               query.Contains("dynamic") ||
               query.Contains("complex");
    }

    public Dictionary<string, object> Execute(Domain.Strategies.ExecutionContext ctx)
    {
        var sessionId = ctx.SessionId ?? Guid.NewGuid().ToString();
        
        // Start building execution tree
        _treeBuilder.Begin(ctx.Query);

        // Use reflection to call Think method on the thinker object
        var thinkerType = _thinker.GetType();
        var thinkMethod = thinkerType.GetMethod("Think");
        
        if (thinkMethod == null)
            throw new InvalidOperationException("Thinker object must have a Think method");

        var reasoning = thinkMethod.Invoke(_thinker, new object[] { ctx.Query })?.ToString() ?? "";
        _treeBuilder.AddAnalysis(reasoning);

        // Dynamically decide on next steps based on reasoning
        var steps = DetermineNextSteps(reasoning);
        
        foreach (var step in steps)
        {
            _treeBuilder.AddCommand(step);
        }

        var result = "Intelligent execution completed with dynamic agent switching";
        _treeBuilder.Finish(result);

        var executionTree = _treeBuilder.Build();

        return new Dictionary<string, object>
        {
            ["reasoning"] = reasoning,
            ["steps"] = steps,
            ["executionTree"] = executionTree,
            ["result"] = result,
            ["strategy"] = Type.ToString(),
            ["sessionId"] = sessionId
        };
    }

    private List<string> DetermineNextSteps(string reasoning)
    {
        // Simple heuristic for determining next steps
        var steps = new List<string>();
        
        if (reasoning.ToLowerInvariant().Contains("analyze"))
            steps.Add("Perform deep analysis");
        
        if (reasoning.ToLowerInvariant().Contains("code") || reasoning.ToLowerInvariant().Contains("implement"))
            steps.Add("Generate implementation");
        
        if (reasoning.ToLowerInvariant().Contains("test") || reasoning.ToLowerInvariant().Contains("verify"))
            steps.Add("Run verification tests");

        return steps.Any() ? steps : new List<string> { "Execute default action" };
    }
}
