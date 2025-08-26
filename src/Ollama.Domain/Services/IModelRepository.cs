using Ollama.Domain.Models;

namespace Ollama.Domain.Services
{
    /// <summary>
    /// Repository interface for managing Ollama model descriptors
    /// </summary>
    public interface IModelRepository
    {
        /// <summary>
        /// Gets all available model descriptors
        /// </summary>
        /// <returns>Collection of all model descriptors</returns>
        IReadOnlyList<IModelDescriptor> GetAllModels();
        
        /// <summary>
        /// Finds a model by its name
        /// </summary>
        /// <param name="modelName">The model name to search for</param>
        /// <returns>Model descriptor if found, null otherwise</returns>
        IModelDescriptor? FindByName(string modelName);
        
        /// <summary>
        /// Finds models that support specific capabilities
        /// </summary>
        /// <param name="capabilities">Required capabilities</param>
        /// <returns>Models that support all specified capabilities</returns>
        IReadOnlyList<IModelDescriptor> FindByCapabilities(params ModelCapability[] capabilities);
        
        /// <summary>
        /// Finds models available in specific size range
        /// </summary>
        /// <param name="minSize">Minimum size (inclusive)</param>
        /// <param name="maxSize">Maximum size (inclusive)</param>
        /// <returns>Models available within the size range</returns>
        IReadOnlyList<IModelDescriptor> FindBySizeRange(ModelSize? minSize = null, ModelSize? maxSize = null);
        
        /// <summary>
        /// Gets recommended models for specific use cases
        /// </summary>
        /// <param name="useCase">The use case to get recommendations for</param>
        /// <param name="performanceRequirement">Performance requirement level</param>
        /// <returns>Recommended models ordered by relevance</returns>
        IReadOnlyList<IModelDescriptor> GetRecommendedModels(ModelUseCase useCase, PerformanceRequirement performanceRequirement = PerformanceRequirement.Balanced);
    }

    /// <summary>
    /// Use case categories for model recommendations
    /// </summary>
    public enum ModelUseCase
    {
        GeneralChat,
        CodeGeneration,
        ReasoningTasks,
        VisionTasks,
        EmbeddingGeneration,
        ToolUsage,
        ThinkingTasks
    }
}
