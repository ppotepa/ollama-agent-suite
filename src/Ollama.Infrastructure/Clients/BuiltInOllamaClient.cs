using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ollama.Domain.Configuration;

namespace Ollama.Infrastructure.Clients;

public class BuiltInOllamaClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BuiltInOllamaClient> _logger;
    private readonly OllamaSettings _ollamaSettings;

    public BuiltInOllamaClient(HttpClient httpClient, ILogger<BuiltInOllamaClient> logger, OllamaSettings ollamaSettings)
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
                stream = false
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
            _logger.LogDebug("Ollama response: {Response}", responseContent);

            // Parse the response to extract the message content
            var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
            
            if (responseObject.TryGetProperty("message", out var messageElement) &&
                messageElement.TryGetProperty("content", out var contentElement))
            {
                var responseText = contentElement.GetString() ?? "";
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
}
