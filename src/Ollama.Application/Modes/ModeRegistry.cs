using Ollama.Domain.Strategies;

namespace Ollama.Application.Modes;

public sealed class ModeRegistry
{
    private readonly Dictionary<StrategyType, IModeStrategy> _strategies;

    public ModeRegistry(IEnumerable<IModeStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.Type, s => s);
    }

    public IModeStrategy GetStrategy(StrategyType type)
    {
        if (_strategies.TryGetValue(type, out var strategy))
            return strategy;
        
        throw new ArgumentException($"No strategy registered for type {type}");
    }

    public IModeStrategy SelectBestStrategy(Domain.Strategies.ExecutionContext context)
    {
        // Try strategies in order of sophistication
        var orderedStrategies = new[]
        {
            StrategyType.Intelligent,
            StrategyType.Collaborative,
            StrategyType.SingleQuery
        };

        foreach (var strategyType in orderedStrategies)
        {
            if (_strategies.TryGetValue(strategyType, out var strategy) && strategy.CanHandle(context))
            {
                return strategy;
            }
        }

        // Fallback to single query mode
        return _strategies[StrategyType.SingleQuery];
    }

    public IEnumerable<StrategyType> GetAvailableStrategyTypes()
    {
        return _strategies.Keys;
    }
}
