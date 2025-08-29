using System.Text.Json.Serialization;

namespace Ollama.Domain.Schemas;

/// <summary>
/// Unified response wrapper that can contain any type of LLM response
/// Provides a generic container for all response types
/// </summary>
public class UnifiedResponseSchema : BaseLLMResponseSchema
{
    [JsonPropertyName("responseType")]
    public string ResponseType { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public object Data { get; set; } = new();

    [JsonPropertyName("rawResponse")]
    public string RawResponse { get; set; } = string.Empty;

    [JsonPropertyName("processedAt")]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("processingInfo")]
    public ProcessingInfo Processing { get; set; } = new();
}

/// <summary>
/// Information about response processing
/// </summary>
public class ProcessingInfo
{
    [JsonPropertyName("parsingAttempts")]
    public int ParsingAttempts { get; set; } = 1;

    [JsonPropertyName("parsingMethod")]
    public string ParsingMethod { get; set; } = string.Empty;

    [JsonPropertyName("fallbackUsed")]
    public bool FallbackUsed { get; set; } = false;

    [JsonPropertyName("validationPassed")]
    public bool ValidationPassed { get; set; } = true;

    [JsonPropertyName("transformations")]
    public List<string> Transformations { get; set; } = new();

    [JsonPropertyName("processingDuration")]
    public TimeSpan ProcessingDuration { get; set; } = TimeSpan.Zero;

    [JsonPropertyName("warnings")]
    public List<ProcessingWarning> Warnings { get; set; } = new();
}

/// <summary>
/// Warning information during processing
/// </summary>
public class ProcessingWarning
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "warning";

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("context")]
    public Dictionary<string, object> Context { get; set; } = new();
}
