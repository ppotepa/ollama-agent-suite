using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Clients;

public interface IPythonLlmClient
{
    Task<string> InitializeChatAsync(string model, string? systemPrompt = null);
    Task<string> ProcessInstructionAsync(string model, string instruction, 
                                       string? chatId = null, 
                                       Dictionary<string, object>? parameters = null);
    Task CleanupChatAsync(string chatId);
}

public class PythonLlmClient : IPythonLlmClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PythonLlmClient> _logger;

    public PythonLlmClient(HttpClient httpClient, ILogger<PythonLlmClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> InitializeChatAsync(string model, string? systemPrompt = null)
    {
        try
        {
            var request = new
            {
                model = model,
                system_prompt = systemPrompt
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/chat/init", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to initialize chat: {StatusCode} - {Response}", 
                    response.StatusCode, responseJson);
                throw new HttpRequestException($"Chat initialization failed: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
            
            if (result.TryGetProperty("error", out var errorElement) && 
                !string.IsNullOrEmpty(errorElement.GetString()))
            {
                var error = errorElement.GetString();
                _logger.LogError("Chat initialization error: {Error}", error);
                throw new InvalidOperationException($"Chat initialization failed: {error}");
            }

            if (result.TryGetProperty("chat_id", out var chatIdElement))
            {
                var chatId = chatIdElement.GetString();
                _logger.LogInformation("Chat initialized successfully with ID: {ChatId}", chatId);
                return chatId ?? throw new InvalidOperationException("Chat ID was null");
            }

            throw new InvalidOperationException("No chat_id returned from initialization");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing chat with model {Model}", model);
            throw;
        }
    }

    public async Task<string> ProcessInstructionAsync(string model, string instruction, 
                                                    string? chatId = null, 
                                                    Dictionary<string, object>? parameters = null)
    {
        try
        {
            var request = new
            {
                model = model,
                instruction = instruction,
                chat_id = chatId,
                parameters = parameters ?? new Dictionary<string, object>()
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/process", content);
            var responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to process instruction: {StatusCode} - {Response}", 
                    response.StatusCode, responseJson);
                throw new HttpRequestException($"Instruction processing failed: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

            if (result.TryGetProperty("error", out var errorElement) && 
                !string.IsNullOrEmpty(errorElement.GetString()))
            {
                var error = errorElement.GetString();
                _logger.LogError("Instruction processing error: {Error}", error);
                throw new InvalidOperationException($"Instruction processing failed: {error}");
            }

            if (result.TryGetProperty("result", out var resultElement))
            {
                var resultText = resultElement.GetString();
                _logger.LogInformation("Instruction processed successfully");
                return resultText ?? string.Empty;
            }

            throw new InvalidOperationException("No result returned from instruction processing");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing instruction: {Instruction}", instruction);
            throw;
        }
    }

    public async Task CleanupChatAsync(string chatId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/chat/{chatId}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to cleanup chat {ChatId}: {StatusCode}", 
                    chatId, response.StatusCode);
            }
            else
            {
                _logger.LogInformation("Chat {ChatId} cleaned up successfully", chatId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up chat {ChatId}", chatId);
        }
    }
}
