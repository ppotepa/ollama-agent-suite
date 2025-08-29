using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Ollama.Application.Modes;
using Ollama.Application.Orchestrator;
using Ollama.Application.Services;
using Ollama.Domain.Agents;
using Ollama.Domain.Services;
using Ollama.Domain.Strategies;
using Ollama.Domain.Tools;
using Ollama.Infrastructure.Agents;
using Ollama.Infrastructure.Tools;
using Ollama.Infrastructure.Tools.Directory;
using Ollama.Infrastructure.Tools.File;
using Ollama.Infrastructure.Tools.Navigation;
using Ollama.Infrastructure.Tools.Download;
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
        
        // Register session-scoped services for cursor navigation
        services.AddScoped<ISessionScope, SessionScope>();
        services.AddSingleton<ICursorConfigurationService, CursorConfigurationService>();
        
        // Register Ollama settings and client
        services.AddSingleton<OllamaSettings>(provider => new OllamaSettings());
        services.AddHttpClient<BuiltInOllamaClient>();
        
        // Register legacy tools (to be converted to AbstractTool)
        services.AddTransient<MathEvaluator>();
        services.AddTransient<GitHubRepositoryDownloader>(provider => 
            new GitHubRepositoryDownloader(
                provider.GetRequiredService<ISessionScope>(), 
                provider.GetRequiredService<ILogger<GitHubRepositoryDownloader>>(),
                provider.GetRequiredService<IHttpClientFactory>().CreateClient()));
        services.AddTransient<FileSystemAnalyzer>(provider => 
            new FileSystemAnalyzer(
                provider.GetRequiredService<ISessionScope>(), 
                provider.GetRequiredService<ILogger<FileSystemAnalyzer>>()));
        services.AddTransient<CodeAnalyzer>();
        services.AddTransient<ExternalCommandExecutor>();
        services.AddTransient<ExternalCommandDetector>();
        
        // Register new AbstractTool-based tools with cursor navigation
        services.AddTransient<CursorNavigationTool>();
        services.AddTransient<PrintWorkingDirectoryTool>();
        services.AddTransient<DirectoryListTool>();
        services.AddTransient<DirectoryCreateTool>();
        services.AddTransient<DirectoryDeleteTool>();
        services.AddTransient<DirectoryMoveTool>();
        services.AddTransient<DirectoryCopyTool>();
        services.AddTransient<FileReadTool>();
        services.AddTransient<FileWriteTool>();
        services.AddTransient<FileCopyTool>();
        services.AddTransient<FileMoveTool>();
        services.AddTransient<FileDeleteTool>();
        services.AddTransient<FileAttributesTool>();
        services.AddTransient<DownloadTool>();
        
        // Configure tool repository with tools using a factory
        services.AddSingleton<IToolRepository>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ToolRepository>>();
            var gitHubDownloaderLogger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<GitHubRepositoryDownloader>>();
            var sessionFileSystem = serviceProvider.GetRequiredService<ISessionFileSystem>();
            
            var toolRepository = new ToolRepository(logger);
            
            // SessionScope will be re-initialized with correct sessionId when tools execute
            var sessionScopeLogger = serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SessionScope>>();
            var dynamicSessionScope = new SessionScope(sessionFileSystem, sessionScopeLogger);
            
            // Register enhanced AbstractTool-based tools
            toolRepository.RegisterTool(new MathEvaluator(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MathEvaluator>>()));
            toolRepository.RegisterTool(new CodeAnalyzer(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CodeAnalyzer>>()));
            toolRepository.RegisterTool(new ExternalCommandExecutor(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ExternalCommandExecutor>>(), sessionFileSystem));
            
            // Register new AbstractTool-based tools with cursor navigation support
            // Tools are registered with a factory pattern to create dynamic session scopes
            // When tools are executed, they will use sessionId from ToolContext
            
            toolRepository.RegisterTool(new GitHubRepositoryDownloader(dynamicSessionScope, gitHubDownloaderLogger, httpClientFactory.CreateClient()));
            toolRepository.RegisterTool(new FileSystemAnalyzer(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FileSystemAnalyzer>>()));
            toolRepository.RegisterTool(new CursorNavigationTool(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CursorNavigationTool>>()));
            toolRepository.RegisterTool(new PrintWorkingDirectoryTool(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<PrintWorkingDirectoryTool>>()));
            toolRepository.RegisterTool(new DirectoryListTool(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DirectoryListTool>>()));
            toolRepository.RegisterTool(new DirectoryCreateTool(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DirectoryCreateTool>>()));
            toolRepository.RegisterTool(new DirectoryDeleteTool(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DirectoryDeleteTool>>()));
            toolRepository.RegisterTool(new DirectoryMoveTool(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DirectoryMoveTool>>()));
            toolRepository.RegisterTool(new DirectoryCopyTool(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DirectoryCopyTool>>()));
            toolRepository.RegisterTool(new FileReadTool(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FileReadTool>>()));
            toolRepository.RegisterTool(new FileWriteTool(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FileWriteTool>>()));
            toolRepository.RegisterTool(new FileCopyTool(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FileCopyTool>>()));
            toolRepository.RegisterTool(new FileMoveTool(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FileMoveTool>>()));
            toolRepository.RegisterTool(new FileDeleteTool(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FileDeleteTool>>()));
            toolRepository.RegisterTool(new FileAttributesTool(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<FileAttributesTool>>()));
            toolRepository.RegisterTool(new DownloadTool(dynamicSessionScope, serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DownloadTool>>(), httpClientFactory.CreateClient()));
            
            return toolRepository;
        });

        // Register strategic agent and its dependencies
        // NOTE: System configured for PESSIMISTIC STRATEGY ONLY
        // This ensures conservative, backend-focused execution for all queries
        // with comprehensive validation and specific development guidance
        
        // Register prompt system services
        services.AddSingleton<Ollama.Domain.Configuration.PromptConfiguration>(provider => 
            new Ollama.Domain.Configuration.PromptConfiguration
            {
                PromptBasePath = "prompts",
                PessimisticPromptFileName = "pessimistic-initial-system-prompt.txt",
                RequirePromptFiles = true,
                MaxPromptFileSize = 1048576 // 1MB
            });
        
        services.AddSingleton<Ollama.Domain.Prompts.IPlaceholderDecorator, Ollama.Infrastructure.Prompts.ToolReflectionDecorator>();
        services.AddSingleton<Ollama.Infrastructure.Prompts.PromptService>();
        
        services.AddSingleton<ISessionFileSystem, Ollama.Infrastructure.Services.SessionFileSystem>();
        services.AddSingleton<Ollama.Infrastructure.Services.SessionLogger>();
        services.AddSingleton<IAgentStrategy, Ollama.Infrastructure.Strategies.PessimisticAgentStrategy>();
        services.AddSingleton<Ollama.Infrastructure.Agents.StrategicAgent>(provider =>
        {
            var strategy = provider.GetRequiredService<IAgentStrategy>();
            var sessionFileSystem = provider.GetRequiredService<ISessionFileSystem>();
            var sessionLogger = provider.GetRequiredService<Ollama.Infrastructure.Services.SessionLogger>();
            var toolRepository = provider.GetRequiredService<IToolRepository>();
            var ollamaClient = provider.GetRequiredService<BuiltInOllamaClient>();
            var communicationService = provider.GetRequiredService<ILLMCommunicationService>();
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Ollama.Infrastructure.Agents.StrategicAgent>>();
            var ollamaSettings = provider.GetRequiredService<OllamaSettings>();
            
            return new Ollama.Infrastructure.Agents.StrategicAgent(strategy, sessionFileSystem, sessionLogger, toolRepository, ollamaClient, communicationService, logger, ollamaSettings.DefaultModel);
        });

        return services;
    }
}
