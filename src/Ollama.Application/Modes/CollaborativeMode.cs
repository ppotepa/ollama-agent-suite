using Ollama.Application.Services;
using Ollama.Domain.Agents;
using Ollama.Domain.Strategies;

namespace Ollama.Application.Modes;

public sealed class CollaborativeMode : IModeStrategy
{
    private readonly IAgent _thinker;
    private readonly IAgent _coder;
    private readonly CollaborationContextService _contextService;

    public StrategyType Type => StrategyType.Collaborative;

    public CollaborativeMode(IAgent thinker, IAgent coder, CollaborationContextService contextService)
    {
        _thinker = thinker;
        _coder = coder;
        _contextService = contextService;
    }

    public bool CanHandle(Domain.Strategies.ExecutionContext ctx)
    {
        // Check if query indicates collaborative work (multiple steps, coding, etc.)
        var query = ctx.Query.ToLowerInvariant();
        return query.Contains("and then") || 
               query.Contains("figure out") || 
               query.Contains("update") ||
               query.Contains("change") ||
               query.Contains("implement");
    }

    public Dictionary<string, object> Execute(Domain.Strategies.ExecutionContext ctx)
    {
        // Step 1: Thinker analyzes the problem
        var analysis = _thinker.Think(ctx.Query);
        _contextService.AddNote($"Analysis: {analysis}");

        // Step 2: Thinker creates a plan
        var plan = _thinker.Plan(ctx.Query);
        _contextService.AddNote($"Plan: {plan}");

        // Step 3: Coder executes the plan
        var implementation = _coder.Act(plan.ToString() ?? "");
        _contextService.AddNote($"Implementation: {implementation}");

        var sessionId = ctx.SessionId ?? Guid.NewGuid().ToString();

        return new Dictionary<string, object>
        {
            ["analysis"] = analysis,
            ["plan"] = plan,
            ["implementation"] = implementation,
            ["notes"] = _contextService.GetNotes(),
            ["strategy"] = Type.ToString(),
            ["sessionId"] = sessionId
        };
    }
}
