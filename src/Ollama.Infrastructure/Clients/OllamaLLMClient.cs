using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ollama.Domain.Configuration;
using Ollama.Domain.Contracts;

namespace Ollama.Infrastructure.Clients;

/// <summary>
/// Ollama LLM client implementation
/// Provides communication with Ollama API endpoints
/// </summary>
public class OllamaLLMClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaLLMClient> _logger;
    private readonly OllamaSettings _ollamaSettings;

    public string ClientType => "Ollama";

    public OllamaLLMClient(HttpClient httpClient, ILogger<OllamaLLMClient> logger, OllamaSettings ollamaSettings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _ollamaSettings = ollamaSettings;
    }

    public async Task<string> ChatAsync(string model, List<(string role, string content)> messages)
    {
        try
        {
            var chatEndpoint = _ollamaSettings.ChatEndpoint;
            
            var requestPayload = new
            {
                model = model,
                messages = messages.Select(m => new { role = m.role, content = m.content }).ToArray(),
                stream = false,
                options = new
                {
                    temperature = 0.1, // Lower temperature for more consistent formatting
                    top_p = 0.9
                }
            };

            var json = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending chat request to Ollama: {Model}", model);
            _logger.LogDebug("Request payload: {Payload}", json);

            var response = await _httpClient.PostAsync(chatEndpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ollama API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Ollama API error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Ollama raw response content: {Response}", responseContent);

            // Parse the response to extract the message content
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            if (responseObject.TryGetProperty("message", out var messageElement) &&
                messageElement.TryGetProperty("content", out var contentElement))
            {
                var responseText = contentElement.GetString() ?? "";
                _logger.LogInformation("Received response from Ollama: {Length} characters", responseText.Length);
                _logger.LogDebug("Extracted message content: {Content}", responseText);
                return responseText;
            }

            _logger.LogWarning("Unexpected response format from Ollama: {Response}", responseContent);
            return responseContent; // Fallback to raw response
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error communicating with Ollama API");
            _logger.LogError("Request details - Model: {Model}, Messages count: {Count}", model, messages.Count);
            throw;
        }
    }

    public async Task<string> GenerateAsync(string model, string prompt)
    {
        try
        {
            var generateEndpoint = _ollamaSettings.GenerateEndpoint;
            
            var requestPayload = new
            {
                model = model,
                prompt = prompt,
                stream = false
            };

            var json = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending generate request to Ollama: {Model}", model);
            _logger.LogDebug("Request payload: {Payload}", json);

            var response = await _httpClient.PostAsync(generateEndpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ollama API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Ollama API error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Ollama response: {Response}", responseContent);

            // Parse the response to extract the generated text
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            if (responseObject.TryGetProperty("response", out var responseElement))
            {
                var responseText = responseElement.GetString() ?? "";
                _logger.LogInformation("Received response from Ollama: {Length} characters", responseText.Length);
                return responseText;
            }

            _logger.LogWarning("Unexpected response format from Ollama: {Response}", responseContent);
            return responseContent; // Fallback to raw response
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error communicating with Ollama API");
            throw;
        }
    }

    public async Task<List<string>> GetAvailableModelsAsync()
    {
        try
        {
            var modelsEndpoint = _ollamaSettings.ModelsEndpoint;
            _logger.LogInformation("Fetching available models from Ollama: {Endpoint}", modelsEndpoint);

            var response = await _httpClient.GetAsync(modelsEndpoint);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Ollama models API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"Ollama models API error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var models = new List<string>();
            
            if (responseObject.TryGetProperty("models", out var modelsArray) && modelsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var model in modelsArray.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out var nameElement))
                    {
                        var modelName = nameElement.GetString();
                        if (!string.IsNullOrEmpty(modelName))
                        {
                            models.Add(modelName);
                        }
                    }
                }
            }

            _logger.LogInformation("Found {Count} models in Ollama", models.Count);
            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching models from Ollama API");
            throw;
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            _logger.LogInformation("Checking Ollama health at {BaseUrl}", _ollamaSettings.BaseUrl);
            
            var response = await _httpClient.GetAsync(_ollamaSettings.ModelsEndpoint);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Ollama connection successful");
                return true;
            }
            
            _logger.LogWarning("Ollama connection failed with status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Ollama at {BaseUrl}", _ollamaSettings.BaseUrl);
            return false;
        }
    }
}
