using System.Text.Json.Serialization;

namespace Ollama.Domain.Schemas;

/// <summary>
/// Streaming response schema for real-time LLM interactions
/// Supports both Ollama and LM Studio streaming capabilities
/// </summary>
public class StreamingResponseSchema : BaseLLMResponseSchema
{
    [JsonPropertyName("chunk")]
    public StreamChunk Chunk { get; set; } = new();

    [JsonPropertyName("stream")]
    public StreamInfo Stream { get; set; } = new();

    [JsonPropertyName("done")]
    public bool Done { get; set; } = false;

    [JsonPropertyName("finalResponse")]
    public string FinalResponse { get; set; } = string.Empty;
}

/// <summary>
/// Individual stream chunk data
/// </summary>
public class StreamChunk
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("delta")]
    public string Delta { get; set; } = string.Empty;

    [JsonPropertyName("index")]
    public int Index { get; set; } = 0;

    [JsonPropertyName("role")]
    public string Role { get; set; } = "assistant";

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("usage")]
    public ChunkUsage? Usage { get; set; }
}

/// <summary>
/// Stream session information
/// </summary>
public class StreamInfo
{
    [JsonPropertyName("streamId")]
    public string StreamId { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; set; }

    [JsonPropertyName("totalChunks")]
    public int TotalChunks { get; set; } = 0;

    [JsonPropertyName("bytesTransferred")]
    public long BytesTransferred { get; set; } = 0;

    [JsonPropertyName("averageChunkSize")]
    public double AverageChunkSize { get; set; } = 0.0;

    [JsonPropertyName("tokensPerSecond")]
    public double TokensPerSecond { get; set; } = 0.0;

    [JsonPropertyName("connectionStatus")]
    public string ConnectionStatus { get; set; } = "active";
}

/// <summary>
/// Token usage for streaming chunks
/// </summary>
public class ChunkUsage
{
    [JsonPropertyName("promptTokens")]
    public int PromptTokens { get; set; } = 0;

    [JsonPropertyName("completionTokens")]
    public int CompletionTokens { get; set; } = 0;

    [JsonPropertyName("totalTokens")]
    public int TotalTokens { get; set; } = 0;

    [JsonPropertyName("tokensInChunk")]
    public int TokensInChunk { get; set; } = 0;
}
