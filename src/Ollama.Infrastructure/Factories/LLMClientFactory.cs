using Microsoft.Extensions.Logging;
using Ollama.Domain.Configuration;
using Ollama.Domain.Contracts;

namespace Ollama.Infrastructure.Factories;

/// <summary>
/// Factory for creating LLM clients based on configuration
/// Supports Ollama and LM Studio clients
/// </summary>
public interface ILLMClientFactory
{
    /// <summary>
    /// Create the configured LLM client instance
    /// </summary>
    /// <returns>Configured ILLMClient implementation</returns>
    ILLMClient CreateClient();

    /// <summary>
    /// Get the configured client type
    /// </summary>
    /// <returns>Client type string (ollama, lmstudio)</returns>
    string GetConfiguredClientType();
}

public class LLMClientFactory : ILLMClientFactory
{
    private readonly AppSettings _appSettings;
    private readonly OllamaSettings _ollamaSettings;
    private readonly LMStudioSettings _lmStudioSettings;
    private readonly HttpClient _httpClient;
    private readonly ILoggerFactory _loggerFactory;

    public LLMClientFactory(
        AppSettings appSettings,
        OllamaSettings ollamaSettings,
        LMStudioSettings lmStudioSettings,
        HttpClient httpClient,
        ILoggerFactory loggerFactory)
    {
        _appSettings = appSettings;
        _ollamaSettings = ollamaSettings;
        _lmStudioSettings = lmStudioSettings;
        _httpClient = httpClient;
        _loggerFactory = loggerFactory;
    }

    public ILLMClient CreateClient()
    {
        var clientType = _appSettings.DefaultClient.ToLowerInvariant();

        return clientType switch
        {
            "ollama" => new Clients.OllamaLLMClient(
                _httpClient, 
                _loggerFactory.CreateLogger<Clients.OllamaLLMClient>(), 
                _ollamaSettings),
            
            "lmstudio" => new Clients.LMStudioLLMClient(
                _httpClient, 
                _loggerFactory.CreateLogger<Clients.LMStudioLLMClient>(), 
                _lmStudioSettings),
            
            _ => throw new InvalidOperationException(
                $"Unsupported LLM client type: {_appSettings.DefaultClient}. " +
                "Supported types: ollama, lmstudio")
        };
    }

    public string GetConfiguredClientType()
    {
        return _appSettings.DefaultClient;
    }
}
