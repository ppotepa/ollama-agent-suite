using Ollama.Domain.Planning;

namespace Ollama.Domain.Services
{
    public interface IModelRegistryService
    {
        Task<List<AvailableModel>> GetAvailableModelsAsync();
        Task<AvailableModel?> GetOptimalModelForTaskAsync(string taskType, List<string> capabilities);
        Task<bool> IsModelAvailableAsync(string modelName);
        Task<bool> PullModelAsync(string modelName);
        string GetDefaultPlanningModel();
        Dictionary<string, List<string>> GetModelCapabilities();
    }
}
