using System.ComponentModel.DataAnnotations;

namespace Ollama.Domain.Contracts;

/// <summary>
/// Common interface for LLM clients (Ollama, LM Studio, etc.)
/// Provides a unified API for different LLM backends
/// </summary>
public interface ILLMClient
{
    /// <summary>
    /// Send a chat request with conversation context
    /// </summary>
    /// <param name="model">The model to use for generation</param>
    /// <param name="messages">List of conversation messages (role, content)</param>
    /// <returns>Generated response text</returns>
    Task<string> ChatAsync(string model, List<(string role, string content)> messages);

    /// <summary>
    /// Send a single generation request without conversation context
    /// </summary>
    /// <param name="model">The model to use for generation</param>
    /// <param name="prompt">The prompt text</param>
    /// <returns>Generated response text</returns>
    Task<string> GenerateAsync(string model, string prompt);

    /// <summary>
    /// Get list of available models from the LLM backend
    /// </summary>
    /// <returns>List of available model names</returns>
    Task<List<string>> GetAvailableModelsAsync();

    /// <summary>
    /// Check if the LLM backend is reachable and operational
    /// </summary>
    /// <returns>True if backend is healthy, false otherwise</returns>
    Task<bool> IsHealthyAsync();

    /// <summary>
    /// The type of LLM client (Ollama, LMStudio, etc.)
    /// </summary>
    string ClientType { get; }
}
