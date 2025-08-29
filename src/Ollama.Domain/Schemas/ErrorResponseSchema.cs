using System.Text.Json.Serialization;

namespace Ollama.Domain.Schemas;

/// <summary>
/// Generic error response schema for all LLM providers
/// Provides consistent error handling across Ollama and LM Studio
/// </summary>
public class ErrorResponseSchema : BaseLLMResponseSchema
{
    [JsonPropertyName("error")]
    public ErrorDetails Error { get; set; } = new();

    [JsonPropertyName("context")]
    public ErrorContext Context { get; set; } = new();

    [JsonPropertyName("recovery")]
    public RecoveryInfo Recovery { get; set; } = new();
}

/// <summary>
/// Detailed error information
/// </summary>
public class ErrorDetails
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public Dictionary<string, object> Details { get; set; } = new();

    [JsonPropertyName("innerError")]
    public ErrorDetails? InnerError { get; set; }

    [JsonPropertyName("stackTrace")]
    public string StackTrace { get; set; } = string.Empty;
}

/// <summary>
/// Context where the error occurred
/// </summary>
public class ErrorContext
{
    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("request")]
    public Dictionary<string, object> Request { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}

/// <summary>
/// Information about error recovery options
/// </summary>
public class RecoveryInfo
{
    [JsonPropertyName("retryable")]
    public bool Retryable { get; set; } = false;

    [JsonPropertyName("maxRetries")]
    public int MaxRetries { get; set; } = 3;

    [JsonPropertyName("retryDelayMs")]
    public int RetryDelayMs { get; set; } = 1000;

    [JsonPropertyName("fallbackAvailable")]
    public bool FallbackAvailable { get; set; } = false;

    [JsonPropertyName("suggestions")]
    public List<string> Suggestions { get; set; } = new();

    [JsonPropertyName("documentationUrl")]
    public string DocumentationUrl { get; set; } = string.Empty;
}
