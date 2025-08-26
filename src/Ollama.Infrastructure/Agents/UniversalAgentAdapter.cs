using Ollama.Domain.Agents;

namespace Ollama.Infrastructure.Agents;

public sealed class UniversalAgentAdapter : IAgent
{
    private readonly string _model;

    public UniversalAgentAdapter(string model = "llama2")
    {
        _model = model;
    }

    public string Answer(string prompt)
    {
        // TODO: Implement actual Ollama API call
        return $"[{_model}] Answer to: {prompt}";
    }

    public string Think(string prompt)
    {
        // TODO: Implement actual Ollama API call with thinking prompt
        return $"[{_model}] Thinking about: {prompt}";
    }

    public object Plan(string prompt)
    {
        // TODO: Implement actual Ollama API call with planning prompt
        return new { 
            model = _model, 
            plan = $"Plan for: {prompt}",
            steps = new[] { "Step 1", "Step 2", "Step 3" }
        };
    }

    public object Act(string instruction)
    {
        // TODO: Implement actual action execution
        return new { 
            ok = true, 
            instruction = instruction,
            model = _model,
            executed = DateTime.UtcNow
        };
    }
}
