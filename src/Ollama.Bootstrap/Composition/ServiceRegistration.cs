using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Ollama.Application.Modes;
using Ollama.Application.Orchestrator;
using Ollama.Application.Services;
using Ollama.Domain.Agents;
using Ollama.Domain.Strategies;
using Ollama.Domain.Tools;
using Ollama.Infrastructure.Agents;
using Ollama.Infrastructure.Tools;
using System.Net.Http;

namespace Ollama.Bootstrap.Composition;

public static class ServiceRegistration
{
    public static IServiceCollection AddOllamaServices(this IServiceCollection services)
    {
        // Register domain services
        services.AddSingleton<ExecutionTreeBuilder>();
        services.AddSingleton<CollaborationContextService>();
        services.AddSingleton<AgentSwitchService>();

        // Register HTTP client for tools
        services.AddHttpClient();
        
        // Register tool repository and tools
        services.AddSingleton<IToolRepository, ToolRepository>();
        services.AddTransient<MathEvaluator>();
        services.AddTransient<GitHubRepositoryDownloader>();
        services.AddTransient<FileSystemAnalyzer>();
        services.AddTransient<CodeAnalyzer>();
        
        // Configure tool repository with tools
        services.AddSingleton(serviceProvider =>
        {
            var toolRepository = serviceProvider.GetRequiredService<IToolRepository>();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ToolRepository>>();
            
            // Register all tools
            toolRepository.RegisterTool(new MathEvaluator());
            toolRepository.RegisterTool(new GitHubRepositoryDownloader(httpClientFactory.CreateClient()));
            toolRepository.RegisterTool(new FileSystemAnalyzer());
            toolRepository.RegisterTool(new CodeAnalyzer());
            
            return toolRepository;
        });

        // Register intelligent agent
        services.AddSingleton<IntelligentAgent>();

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
            var intelligentAgent = provider.GetRequiredService<IntelligentAgent>();
            var agentSwitchService = provider.GetRequiredService<AgentSwitchService>();
            var treeBuilder = provider.GetRequiredService<ExecutionTreeBuilder>();
            
            return new IntelligentMode(
                intelligentAgent, 
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
