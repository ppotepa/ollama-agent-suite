using System.Text.Json.Serialization;

namespace Ollama.Domain.Schemas;

/// <summary>
/// Generic generation response schema for single prompt completions
/// Works with both Ollama and LM Studio generation endpoints
/// </summary>
public class GenerationResponseSchema : BaseLLMResponseSchema
{
    [JsonPropertyName("generated_text")]
    public string GeneratedText { get; set; } = string.Empty;

    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [JsonPropertyName("completion")]
    public CompletionDetails Completion { get; set; } = new();

    [JsonPropertyName("usage")]
    public GenerationUsage Usage { get; set; } = new();
}

/// <summary>
/// Details about the text completion
/// </summary>
public class CompletionDetails
{
    [JsonPropertyName("stopReason")]
    public string StopReason { get; set; } = string.Empty;

    [JsonPropertyName("done")]
    public bool Done { get; set; } = true;

    [JsonPropertyName("created")]
    public long Created { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [JsonPropertyName("evalCount")]
    public int EvalCount { get; set; } = 0;

    [JsonPropertyName("evalDuration")]
    public long EvalDuration { get; set; } = 0;

    [JsonPropertyName("loadDuration")]
    public long LoadDuration { get; set; } = 0;

    [JsonPropertyName("promptEvalCount")]
    public int PromptEvalCount { get; set; } = 0;

    [JsonPropertyName("promptEvalDuration")]
    public long PromptEvalDuration { get; set; } = 0;

    [JsonPropertyName("totalDuration")]
    public long TotalDuration { get; set; } = 0;
}

/// <summary>
/// Token usage for generation requests
/// </summary>
public class GenerationUsage
{
    [JsonPropertyName("promptTokens")]
    public int PromptTokens { get; set; } = 0;

    [JsonPropertyName("completionTokens")]
    public int CompletionTokens { get; set; } = 0;

    [JsonPropertyName("totalTokens")]
    public int TotalTokens { get; set; } = 0;

    [JsonPropertyName("tokensPerSecond")]
    public double TokensPerSecond { get; set; } = 0.0;
}
