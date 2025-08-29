using System.Text.Json.Serialization;

namespace Ollama.Domain.Schemas;

/// <summary>
/// Generic base response model that all LLM responses inherit from
/// Provides common structure for both Ollama and LM Studio responses
/// </summary>
public abstract class BaseLLMResponseSchema
{
    [JsonPropertyName("responseId")]
    public string ResponseId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; } = new();

    [JsonPropertyName("metadata")]
    public LLMResponseMetadata Metadata { get; set; } = new();
}

/// <summary>
/// Standard response status information
/// </summary>
public class ResponseStatus
{
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("statusCode")]
    public string StatusCode { get; set; } = "OK";

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Metadata about the LLM response generation
/// </summary>
public class LLMResponseMetadata
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    [JsonPropertyName("tokensUsed")]
    public int TokensUsed { get; set; } = 0;

    [JsonPropertyName("processingTimeMs")]
    public int ProcessingTimeMs { get; set; } = 0;

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("topP")]
    public double? TopP { get; set; }

    [JsonPropertyName("maxTokens")]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("finishReason")]
    public string FinishReason { get; set; } = string.Empty;
}
