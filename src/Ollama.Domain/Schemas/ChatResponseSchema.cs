using System.Text.Json.Serialization;

namespace Ollama.Domain.Schemas;

/// <summary>
/// Generic chat response schema that works with both Ollama and LM Studio
/// Represents structured chat completion responses
/// </summary>
public class ChatResponseSchema : BaseLLMResponseSchema
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = "assistant";

    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [JsonPropertyName("context")]
    public ChatContext Context { get; set; } = new();

    [JsonPropertyName("usage")]
    public TokenUsage Usage { get; set; } = new();
}

/// <summary>
/// Chat-specific context information
/// </summary>
public class ChatContext
{
    [JsonPropertyName("messageCount")]
    public int MessageCount { get; set; } = 0;

    [JsonPropertyName("totalTokens")]
    public int TotalTokens { get; set; } = 0;

    [JsonPropertyName("systemPrompt")]
    public string SystemPrompt { get; set; } = string.Empty;

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("contextLength")]
    public int ContextLength { get; set; } = 0;
}

/// <summary>
/// Token usage statistics
/// </summary>
public class TokenUsage
{
    [JsonPropertyName("promptTokens")]
    public int PromptTokens { get; set; } = 0;

    [JsonPropertyName("completionTokens")]
    public int CompletionTokens { get; set; } = 0;

    [JsonPropertyName("totalTokens")]
    public int TotalTokens { get; set; } = 0;
}
