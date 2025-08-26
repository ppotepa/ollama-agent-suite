namespace Ollama.Domain.Strategies;

public interface IModeStrategy
{
    StrategyType Type { get; }
    bool CanHandle(ExecutionContext ctx);
    Dictionary<string, object> Execute(ExecutionContext ctx);
}
