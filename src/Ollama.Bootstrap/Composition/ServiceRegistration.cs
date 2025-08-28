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
        // Register domain services (minimal set for strategic agent)
        services.AddSingleton<ExecutionTreeBuilder>();
        services.AddSingleton<CollaborationContextService>();
        services.AddSingleton<AgentSwitchService>();
        services.AddSingleton<ILLMCommunicationService, RealLLMCommunicationService>();

        // Register HTTP client for tools
        services.AddHttpClient();
        
        // Register Ollama settings and client
        services.AddSingleton<OllamaSettings>(provider => new OllamaSettings());
        services.AddHttpClient<BuiltInOllamaClient>();
        
        // Register tool repository and tools
        services.AddTransient<MathEvaluator>();
        services.AddTransient<GitHubRepositoryDownloader>();
        services.AddTransient<FileSystemAnalyzer>();
        services.AddTransient<CodeAnalyzer>();
        services.AddTransient<ExternalCommandExecutor>();
        services.AddTransient<ExternalCommandDetector>();
        
        // Configure tool repository with tools using a factory
        services.AddSingleton<IToolRepository>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ToolRepository>>();
            var gitHubDownloaderLogger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<GitHubRepositoryDownloader>>();
            
            var toolRepository = new ToolRepository(logger);
            
            // Register all tools
            toolRepository.RegisterTool(new MathEvaluator());
            toolRepository.RegisterTool(new GitHubRepositoryDownloader(httpClientFactory.CreateClient(), gitHubDownloaderLogger));
            toolRepository.RegisterTool(new FileSystemAnalyzer());
            toolRepository.RegisterTool(new CodeAnalyzer());
            toolRepository.RegisterTool(new ExternalCommandExecutor());
            
            return toolRepository;
        });

        // Register strategic agent and its dependencies
        services.AddSingleton<ISessionFileSystem, Ollama.Infrastructure.Services.SessionFileSystem>();
        services.AddSingleton<IAgentStrategy, Ollama.Infrastructure.Strategies.PessimisticAgentStrategy>();
        services.AddSingleton<Ollama.Infrastructure.Agents.StrategicAgent>(provider =>
        {
            var strategy = provider.GetRequiredService<IAgentStrategy>();
            var sessionFileSystem = provider.GetRequiredService<ISessionFileSystem>();
            var toolRepository = provider.GetRequiredService<IToolRepository>();
            var ollamaClient = provider.GetRequiredService<BuiltInOllamaClient>();
            var communicationService = provider.GetRequiredService<ILLMCommunicationService>();
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Ollama.Infrastructure.Agents.StrategicAgent>>();
            var ollamaSettings = provider.GetRequiredService<OllamaSettings>();
            
            return new Ollama.Infrastructure.Agents.StrategicAgent(strategy, sessionFileSystem, toolRepository, ollamaClient, communicationService, logger, ollamaSettings.DefaultModel);
        });

        return services;
    }
}
