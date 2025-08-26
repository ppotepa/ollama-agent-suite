using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ollama.Domain.Configuration;
using Ollama.Domain.Services;
using Ollama.Infrastructure.Repositories;

namespace Ollama.Bootstrap.Configuration
{
    public static class ConfigurationExtensions
    {
        public static IServiceCollection AddOllamaConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind configuration sections
            var appSettings = new AppSettings();
            configuration.Bind(appSettings);
            services.AddSingleton(appSettings);

            var ollamaSettings = new OllamaSettings();
            configuration.GetSection("OllamaSettings").Bind(ollamaSettings);
            services.AddSingleton(ollamaSettings);

            var agentSettings = new AgentSettings();
            configuration.GetSection("AgentSettings").Bind(agentSettings);
            services.AddSingleton(agentSettings);

            var modeSettings = new ModeSettings();
            configuration.GetSection("ModeSettings").Bind(modeSettings);
            services.AddSingleton(modeSettings);

            var infrastructureSettings = new InfrastructureSettings();
            configuration.GetSection("Infrastructure").Bind(infrastructureSettings);
            services.AddSingleton(infrastructureSettings);

            // Register model repository
            services.AddSingleton<IModelRepository, OllamaModelRepository>();

            return services;
        }
    }
}
