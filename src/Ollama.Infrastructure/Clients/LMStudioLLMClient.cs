using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ollama.Domain.Configuration;
using Ollama.Domain.Contracts;

namespace Ollama.Infrastructure.Clients;

/// <summary>
/// LM Studio LLM client implementation
/// Provides communication with LM Studio OpenAI-compatible API endpoints
/// </summary>
public class LMStudioLLMClient : ILLMClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LMStudioLLMClient> _logger;
    private readonly LMStudioSettings _lmStudioSettings;

    public string ClientType => "LMStudio";

    public LMStudioLLMClient(HttpClient httpClient, ILogger<LMStudioLLMClient> logger, LMStudioSettings lmStudioSettings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _lmStudioSettings = lmStudioSettings;

        // Set up authorization header if API key is provided
        if (!string.IsNullOrEmpty(_lmStudioSettings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _lmStudioSettings.ApiKey);
        }
    }

    public async Task<string> ChatAsync(string model, List<(string role, string content)> messages)
    {
        try
        {
            var chatEndpoint = _lmStudioSettings.ChatEndpoint;
            
            // Convert our message format to OpenAI format
            var openAIMessages = messages.Select(m => new
            {
                role = m.role,
                content = m.content
            }).ToArray();

            var requestPayload = new
            {
                model = model,
                messages = openAIMessages,
                temperature = _lmStudioSettings.Temperature,
                top_p = _lmStudioSettings.TopP,
                max_tokens = _lmStudioSettings.MaxTokens,
                stream = false
            };

            var json = JsonSerializer.Serialize(requestPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending chat request to LM Studio: {Model}", model);
            _logger.LogDebug("Request payload: {Payload}", json);

            var response = await _httpClient.PostAsync(chatEndpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("LM Studio API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"LM Studio API error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("LM Studio raw response content: {Response}", responseContent);

            // Parse OpenAI-compatible response format
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            if (responseObject.TryGetProperty("choices", out var choicesElement) && 
                choicesElement.ValueKind == JsonValueKind.Array &&
                choicesElement.GetArrayLength() > 0)
            {
                var firstChoice = choicesElement[0];
                if (firstChoice.TryGetProperty("message", out var messageElement) &&
                    messageElement.TryGetProperty("content", out var contentElement))
                {
                    var responseText = contentElement.GetString() ?? "";
                    _logger.LogInformation("Received response from LM Studio: {Length} characters", responseText.Length);
                    _logger.LogDebug("Extracted message content: {Content}", responseText);
                    return responseText;
                }
            }

            _logger.LogWarning("Unexpected response format from LM Studio: {Response}", responseContent);
            return responseContent; // Fallback to raw response
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error communicating with LM Studio API");
            _logger.LogError("Request details - Model: {Model}, Messages count: {Count}", model, messages.Count);
            throw;
        }
    }

    public async Task<string> GenerateAsync(string model, string prompt)
    {
        // For LM Studio, we convert the generate call to a chat call with a single user message
        var messages = new List<(string role, string content)>
        {
            ("user", prompt)
        };

        return await ChatAsync(model, messages);
    }

    public async Task<List<string>> GetAvailableModelsAsync()
    {
        try
        {
            var modelsEndpoint = _lmStudioSettings.ModelsEndpoint;
            _logger.LogInformation("Fetching available models from LM Studio: {Endpoint}", modelsEndpoint);

            var response = await _httpClient.GetAsync(modelsEndpoint);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("LM Studio models API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"LM Studio models API error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var models = new List<string>();
            
            if (responseObject.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var model in dataArray.EnumerateArray())
                {
                    if (model.TryGetProperty("id", out var idElement))
                    {
                        var modelId = idElement.GetString();
                        if (!string.IsNullOrEmpty(modelId))
                        {
                            models.Add(modelId);
                        }
                    }
                }
            }

            _logger.LogInformation("Found {Count} models in LM Studio", models.Count);
            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching models from LM Studio API");
            throw;
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            _logger.LogInformation("Checking LM Studio health at {BaseUrl}", _lmStudioSettings.BaseUrl);
            
            var response = await _httpClient.GetAsync(_lmStudioSettings.ModelsEndpoint);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("LM Studio connection successful");
                return true;
            }
            
            _logger.LogWarning("LM Studio connection failed with status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to LM Studio at {BaseUrl}", _lmStudioSettings.BaseUrl);
            return false;
        }
    }
}
