using System.Text.Json.Serialization;

namespace Ollama.Domain.Schemas;

/// <summary>
/// Generic model information schema
/// Represents available models from any LLM provider
/// </summary>
public class ModelInfoSchema
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; } = 0;

    [JsonPropertyName("parameters")]
    public ModelParameters Parameters { get; set; } = new();

    [JsonPropertyName("capabilities")]
    public ModelCapabilities Capabilities { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    [JsonPropertyName("status")]
    public ModelStatus Status { get; set; } = new();
}

/// <summary>
/// Model configuration parameters
/// </summary>
public class ModelParameters
{
    [JsonPropertyName("contextLength")]
    public int ContextLength { get; set; } = 2048;

    [JsonPropertyName("maxTokens")]
    public int MaxTokens { get; set; } = 512;

    [JsonPropertyName("defaultTemperature")]
    public double DefaultTemperature { get; set; } = 0.7;

    [JsonPropertyName("supportedFormats")]
    public List<string> SupportedFormats { get; set; } = new();

    [JsonPropertyName("architecture")]
    public string Architecture { get; set; } = string.Empty;

    [JsonPropertyName("quantization")]
    public string Quantization { get; set; } = string.Empty;
}

/// <summary>
/// Model capabilities and features
/// </summary>
public class ModelCapabilities
{
    [JsonPropertyName("supportsChat")]
    public bool SupportsChat { get; set; } = true;

    [JsonPropertyName("supportsCompletion")]
    public bool SupportsCompletion { get; set; } = true;

    [JsonPropertyName("supportsStreaming")]
    public bool SupportsStreaming { get; set; } = false;

    [JsonPropertyName("supportsFunctionCalling")]
    public bool SupportsFunctionCalling { get; set; } = false;

    [JsonPropertyName("supportsSystemMessages")]
    public bool SupportsSystemMessages { get; set; } = true;

    [JsonPropertyName("supportedLanguages")]
    public List<string> SupportedLanguages { get; set; } = new();

    [JsonPropertyName("specialFeatures")]
    public List<string> SpecialFeatures { get; set; } = new();
}

/// <summary>
/// Model availability and health status
/// </summary>
public class ModelStatus
{
    [JsonPropertyName("available")]
    public bool Available { get; set; } = true;

    [JsonPropertyName("loaded")]
    public bool Loaded { get; set; } = false;

    [JsonPropertyName("lastUsed")]
    public DateTime? LastUsed { get; set; }

    [JsonPropertyName("loadTime")]
    public TimeSpan? LoadTime { get; set; }

    [JsonPropertyName("memoryUsage")]
    public long MemoryUsage { get; set; } = 0;

    [JsonPropertyName("healthStatus")]
    public string HealthStatus { get; set; } = "unknown";
}
