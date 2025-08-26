using Microsoft.Extensions.DependencyInjection;
using Ollama.Application.Modes;
using Ollama.Application.Orchestrator;
using Ollama.Application.Services;
using Ollama.Domain.Agents;
using Ollama.Domain.Strategies;
using Ollama.Infrastructure.Agents;

namespace Ollama.Bootstrap.Composition;

public static class ServiceRegistration
{
    public static IServiceCollection AddOllamaServices(this IServiceCollection services)
    {
        // Register domain services
        services.AddSingleton<ExecutionTreeBuilder>();
        services.AddSingleton<CollaborationContextService>();
        services.AddSingleton<AgentSwitchService>();

        // Register agents
        services.AddSingleton<IAgent>(provider => new UniversalAgentAdapter("llama2"));
        services.AddSingleton<IAgent>(provider => new UniversalAgentAdapter("codellama"));

        // Factory for creating specific agents by role
        services.AddSingleton<Func<string, IAgent>>(provider => role =>
        {
            return role.ToLowerInvariant() switch
            {
                "thinker" => new UniversalAgentAdapter("llama2"),
                "coder" => new UniversalAgentAdapter("codellama"),
                _ => new UniversalAgentAdapter("llama2")
            };
        });

        // Register strategies
        services.AddSingleton<IModeStrategy>(provider =>
        {
            var agentFactory = provider.GetRequiredService<Func<string, IAgent>>();
            return new SingleQueryMode(agentFactory("default"));
        });

        services.AddSingleton<IModeStrategy>(provider =>
        {
            var agentFactory = provider.GetRequiredService<Func<string, IAgent>>();
            var contextService = provider.GetRequiredService<CollaborationContextService>();
            return new CollaborativeMode(
                agentFactory("thinker"), 
                agentFactory("coder"), 
                contextService);
        });

        services.AddSingleton<IModeStrategy>(provider =>
        {
            var agentFactory = provider.GetRequiredService<Func<string, IAgent>>();
            var agentSwitchService = provider.GetRequiredService<AgentSwitchService>();
            var treeBuilder = provider.GetRequiredService<ExecutionTreeBuilder>();
            
            return new IntelligentMode(
                agentFactory("thinker"), 
                agentSwitchService, 
                treeBuilder);
        });

        // Register mode registry
        services.AddSingleton<ModeRegistry>(provider =>
        {
            var strategies = provider.GetServices<IModeStrategy>();
            return new ModeRegistry(strategies);
        });

        // Register orchestrator
        services.AddSingleton<StrategyOrchestrator>();

        return services;
    }
}
