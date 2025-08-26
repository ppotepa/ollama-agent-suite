using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ollama.Domain.Configuration;

namespace Ollama.Infrastructure.Configuration
{
    public interface IConfigurationChecker
    {
        Task<bool> ValidateOllamaConnectionAsync();
        Task<List<string>> GetAvailableModelsAsync();
        Task<bool> IsModelAvailableAsync(string modelName);
    }

    public class ConfigurationChecker : IConfigurationChecker
    {
        private readonly HttpClient _httpClient;
        private readonly OllamaSettings _ollamaSettings;
        private readonly ILogger<ConfigurationChecker> _logger;

        public ConfigurationChecker(
            HttpClient httpClient,
            IOptions<OllamaSettings> ollamaSettings,
            ILogger<ConfigurationChecker> logger)
        {
            _httpClient = httpClient;
            _ollamaSettings = ollamaSettings.Value;
            _logger = logger;
        }

        public async Task<bool> ValidateOllamaConnectionAsync()
        {
            try
            {
                _logger.LogInformation("Validating Ollama connection to {BaseUrl}", _ollamaSettings.BaseUrl);
                
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

        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_ollamaSettings.ModelsEndpoint);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to retrieve models. Status: {StatusCode}", response.StatusCode);
                    return new List<string>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var modelsResponse = JsonSerializer.Deserialize<ModelsResponse>(content);
                
                return modelsResponse?.Models?.Select(m => m.Name).ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available models");
                return new List<string>();
            }
        }

        public async Task<bool> IsModelAvailableAsync(string modelName)
        {
            var availableModels = await GetAvailableModelsAsync();
            return availableModels.Any(m => m.Equals(modelName, StringComparison.OrdinalIgnoreCase));
        }

        private class ModelsResponse
        {
            public List<ModelInfo> Models { get; set; } = new();
        }

        private class ModelInfo
        {
            public string Name { get; set; } = string.Empty;
            public DateTime Modified_at { get; set; }
            public long Size { get; set; }
        }
    }
}
