using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Ollama.Application.Modes;
using Ollama.Application.Orchestrator;
using Ollama.Application.Services;
using Ollama.Domain.Agents;
using Ollama.Domain.Services;
using Ollama.Domain.Strategies;
using Ollama.Domain.Tools;
using Ollama.Infrastructure.Agents;
using Ollama.Infrastructure.Tools;
using Ollama.Infrastructure.Services;
using Ollama.Infrastructure.Clients;
using Ollama.Domain.Configuration;
using System.Net.Http;

namespace Ollama.Bootstrap.Composition;

public static class ServiceRegistration
{
    public static IServiceCollection AddOllamaServices(this IServiceCollection services)
    {
        // Register Python subsystem services
        services.AddSingleton<IPythonSubsystemService, PythonSubsystemService>();
        services.AddHttpClient<IPythonLlmClient, PythonLlmClient>(client => {
            client.BaseAddress = new Uri("http://localhost:8000");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register planning services
        services.AddSingleton<IModelRegistryService, ModelRegistryService>();
        services.AddSingleton<IPlanningService, PlanningService>();

        // Register domain services
        services.AddSingleton<ExecutionTreeBuilder>();
        services.AddSingleton<CollaborationContextService>();
        services.AddSingleton<AgentSwitchService>();

        // Register HTTP client for tools
        services.AddHttpClient();
        
        // Register tool repository and tools
        services.AddTransient<MathEvaluator>();
        services.AddTransient<GitHubRepositoryDownloader>();
        services.AddTransient<FileSystemAnalyzer>();
        services.AddTransient<CodeAnalyzer>();
        
        // Configure tool repository with tools using a factory
        services.AddSingleton<IToolRepository>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ToolRepository>>();
            
            var toolRepository = new ToolRepository(logger);
            
            // Register all tools
            toolRepository.RegisterTool(new MathEvaluator());
            toolRepository.RegisterTool(new GitHubRepositoryDownloader(httpClientFactory.CreateClient()));
            toolRepository.RegisterTool(new FileSystemAnalyzer());
            toolRepository.RegisterTool(new CodeAnalyzer());
            
            return toolRepository;
        });

        // Register intelligent agent with explicit factory
        services.AddSingleton<IntelligentAgent>(provider =>
        {
            var toolRepository = provider.GetRequiredService<IToolRepository>();
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<IntelligentAgent>>();
            var pythonService = provider.GetRequiredService<IPythonSubsystemService>();
            var pythonClient = provider.GetRequiredService<IPythonLlmClient>();
            var planningService = provider.GetRequiredService<IPlanningService>();
            
            return new IntelligentAgent(toolRepository, logger, pythonService, pythonClient, planningService);
        });

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
