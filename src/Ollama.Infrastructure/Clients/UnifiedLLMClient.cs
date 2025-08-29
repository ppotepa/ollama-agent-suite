using Microsoft.Extensions.Logging;
using Ollama.Domain.Contracts;
using Ollama.Infrastructure.Factories;

namespace Ollama.Infrastructure.Clients;

/// <summary>
/// Backward compatibility wrapper for the unified LLM client system
/// Maintains the same interface as BuiltInOllamaClient while using the new ILLMClient factory
/// </summary>
public class UnifiedLLMClient
{
    private readonly ILLMClient _llmClient;
    private readonly ILogger<UnifiedLLMClient> _logger;

    public UnifiedLLMClient(ILLMClientFactory llmClientFactory, ILogger<UnifiedLLMClient> logger)
    {
        _llmClient = llmClientFactory.CreateClient();
        _logger = logger;
        
        _logger.LogInformation("Initialized UnifiedLLMClient with {ClientType}", _llmClient.ClientType);
    }

    /// <summary>
    /// Send a chat request with conversation context
    /// Maintains compatibility with existing BuiltInOllamaClient.ChatAsync method
    /// </summary>
    /// <param name="model">The model to use for generation</param>
    /// <param name="messages">List of conversation messages (role, content)</param>
    /// <returns>Generated response text</returns>
    public async Task<string> ChatAsync(string model, List<(string role, string content)> messages)
    {
        try
        {
            _logger.LogDebug("Forwarding chat request to {ClientType} client", _llmClient.ClientType);
            return await _llmClient.ChatAsync(model, messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UnifiedLLMClient.ChatAsync using {ClientType}", _llmClient.ClientType);
            throw;
        }
    }

    /// <summary>
    /// Send a single generation request without conversation context
    /// Maintains compatibility with existing BuiltInOllamaClient.GenerateAsync method
    /// </summary>
    /// <param name="model">The model to use for generation</param>
    /// <param name="prompt">The prompt text</param>
    /// <returns>Generated response text</returns>
    public async Task<string> GenerateAsync(string model, string prompt)
    {
        try
        {
            _logger.LogDebug("Forwarding generate request to {ClientType} client", _llmClient.ClientType);
            return await _llmClient.GenerateAsync(model, prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UnifiedLLMClient.GenerateAsync using {ClientType}", _llmClient.ClientType);
            throw;
        }
    }

    /// <summary>
    /// Get list of available models from the LLM backend
    /// </summary>
    /// <returns>List of available model names</returns>
    public async Task<List<string>> GetAvailableModelsAsync()
    {
        try
        {
            _logger.LogDebug("Forwarding get models request to {ClientType} client", _llmClient.ClientType);
            return await _llmClient.GetAvailableModelsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UnifiedLLMClient.GetAvailableModelsAsync using {ClientType}", _llmClient.ClientType);
            throw;
        }
    }

    /// <summary>
    /// Check if the LLM backend is reachable and operational
    /// </summary>
    /// <returns>True if backend is healthy, false otherwise</returns>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            _logger.LogDebug("Forwarding health check to {ClientType} client", _llmClient.ClientType);
            return await _llmClient.IsHealthyAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UnifiedLLMClient.IsHealthyAsync using {ClientType}", _llmClient.ClientType);
            return false;
        }
    }

    /// <summary>
    /// The type of LLM client currently in use
    /// </summary>
    public string ClientType => _llmClient.ClientType;
}
