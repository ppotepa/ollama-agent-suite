using Microsoft.Extensions.Logging;
using Ollama.Domain.Planning;
using Ollama.Domain.Services;
using Ollama.Infrastructure.Clients;

namespace Ollama.Infrastructure.Services
{
    public class ModelRegistryService : IModelRegistryService
    {
        private readonly IPythonLlmClient _pythonClient;
        private readonly ILogger<ModelRegistryService> _logger;
        
        private readonly Dictionary<string, List<string>> _modelCapabilities = new()
        {
            ["llama3.1:8b-instruct"] = new() { "planning", "reasoning", "analysis", "general" },
            ["llama3.1:8b"] = new() { "general", "conversation", "summary" },
            ["codellama"] = new() { "coding", "programming", "code-analysis", "debugging" },
            ["llama2"] = new() { "general", "conversation" },
            ["mistral"] = new() { "general", "reasoning", "analysis" },
            ["qwen2.5-coder"] = new() { "coding", "programming", "technical" },
            ["deepseek-coder"] = new() { "coding", "programming", "advanced-coding" }
        };

        private readonly Dictionary<string, string> _taskToModel = new()
        {
            ["planning"] = "llama3.1:8b-instruct",
            ["coding"] = "codellama",
            ["math"] = "llama3.1:8b-instruct",
            ["analysis"] = "llama3.1:8b-instruct",
            ["general"] = "llama3.1:8b"
        };

        public ModelRegistryService(
            IPythonLlmClient pythonClient,
            ILogger<ModelRegistryService> logger)
        {
            _pythonClient = pythonClient;
            _logger = logger;
        }

        public async Task<List<AvailableModel>> GetAvailableModelsAsync()
        {
            var models = new List<AvailableModel>();
            
            foreach (var (modelName, capabilities) in _modelCapabilities)
            {
                var isAvailable = await IsModelAvailableAsync(modelName);
                models.Add(new AvailableModel
                {
                    Name = modelName,
                    Description = GetModelDescription(modelName),
                    Capabilities = capabilities,
                    IsAvailable = isAvailable,
                    PullCommand = isAvailable ? null : $"ollama pull {modelName}"
                });
            }

            return models;
        }

        public async Task<AvailableModel?> GetOptimalModelForTaskAsync(string taskType, List<string> capabilities)
        {
            _logger.LogInformation("🔍 Finding optimal model for task: {TaskType} with capabilities: {Capabilities}", 
                taskType, string.Join(", ", capabilities));

            // First try direct task mapping
            if (_taskToModel.TryGetValue(taskType.ToLowerInvariant(), out var preferredModel))
            {
                _logger.LogInformation("📊 Direct task mapping found: {TaskType} → {Model}", taskType, preferredModel);
                
                var isAvailable = await IsModelAvailableAsync(preferredModel);
                if (isAvailable)
                {
                    _logger.LogInformation("✅ Preferred model available: {Model}", preferredModel);
                    return new AvailableModel
                    {
                        Name = preferredModel,
                        Description = GetModelDescription(preferredModel),
                        Capabilities = _modelCapabilities[preferredModel],
                        IsAvailable = true
                    };
                }
                else
                {
                    _logger.LogWarning("❌ Preferred model not available: {Model}", preferredModel);
                }
            }

            // Find best match based on capabilities
            _logger.LogInformation("🔍 Searching for best capability match...");
            
            var bestMatch = _modelCapabilities
                .Where(kv => capabilities.Any(cap => kv.Value.Contains(cap.ToLowerInvariant())))
                .OrderByDescending(kv => kv.Value.Count(cap => capabilities.Contains(cap, StringComparer.OrdinalIgnoreCase)))
                .FirstOrDefault();

            if (bestMatch.Key != null)
            {
                _logger.LogInformation("📊 Best capability match found: {Model} with capabilities: {Capabilities}", 
                    bestMatch.Key, string.Join(", ", bestMatch.Value));
                
                var isAvailable = await IsModelAvailableAsync(bestMatch.Key);
                var result = new AvailableModel
                {
                    Name = bestMatch.Key,
                    Description = GetModelDescription(bestMatch.Key),
                    Capabilities = bestMatch.Value,
                    IsAvailable = isAvailable,
                    PullCommand = isAvailable ? null : $"ollama pull {bestMatch.Key}"
                };

                if (isAvailable)
                {
                    _logger.LogInformation("✅ Best match model available: {Model}", bestMatch.Key);
                }
                else
                {
                    _logger.LogWarning("❌ Best match model not available: {Model}, pull command: {PullCommand}", 
                        bestMatch.Key, result.PullCommand);
                }

                return result;
            }

            _logger.LogWarning("❌ No suitable model found for task: {TaskType} with capabilities: {Capabilities}", 
                taskType, string.Join(", ", capabilities));
            return null;
        }

        public async Task<bool> IsModelAvailableAsync(string modelName)
        {
            _logger.LogInformation("🔍 Checking availability of model: {ModelName}", modelName);
            
            try
            {
                // Try to get model info through Python client
                // This is a simplified check - in reality you'd call Ollama API
                var isAvailable = await Task.FromResult(_modelCapabilities.ContainsKey(modelName));
                
                if (isAvailable)
                {
                    _logger.LogInformation("✅ Model is available: {ModelName}", modelName);
                }
                else
                {
                    _logger.LogInformation("❌ Model not available: {ModelName}. Available models: {AvailableModels}", 
                        modelName, string.Join(", ", _modelCapabilities.Keys));
                }
                
                return isAvailable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking model availability for: {ModelName}", modelName);
                return false;
            }
        }

        public async Task<bool> PullModelAsync(string modelName)
        {
            _logger.LogInformation("📥 Starting model pull: {ModelName}", modelName);
            
            try
            {
                // In a real implementation, this would call the Python service to execute:
                // ollama pull {modelName}
                
                _logger.LogInformation("⏳ Simulating model pull for: {ModelName}", modelName);
                await Task.Delay(1000); // Simulate pull time
                
                _logger.LogInformation("✅ Model pull completed successfully: {ModelName}", modelName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error pulling model: {ModelName}", modelName);
                return false;
            }
        }

        public string GetDefaultPlanningModel()
        {
            return "llama3.1:8b-instruct";
        }

        public Dictionary<string, List<string>> GetModelCapabilities()
        {
            return new Dictionary<string, List<string>>(_modelCapabilities);
        }

        private string GetModelDescription(string modelName)
        {
            return modelName switch
            {
                "llama3.1:8b-instruct" => "Advanced reasoning and planning model with instruction following",
                "llama3.2" => "General purpose conversational model",
                "codellama" => "Specialized model for code generation and analysis",
                "llama2" => "General purpose model for conversations",
                "mistral" => "High-quality reasoning and analysis model",
                "qwen2.5-coder" => "Advanced coding model with multilingual support",
                "deepseek-coder" => "State-of-the-art code generation model",
                _ => "Language model for various tasks"
            };
        }
    }
}
