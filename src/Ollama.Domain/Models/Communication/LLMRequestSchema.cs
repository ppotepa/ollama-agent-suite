using System.Text.Json.Serialization;

namespace Ollama.Domain.Models.Communication;

/// <summary>
/// Schema for requests sent TO the LLM - ensures consistent input format
/// </summary>
public class LLMRequestSchema
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("conversationContext")]
    public ConversationContext Context { get; set; } = new();

    [JsonPropertyName("userQuery")]
    public string UserQuery { get; set; } = string.Empty;

    [JsonPropertyName("availableTools")]
    public List<ToolDescription> AvailableTools { get; set; } = new();

    [JsonPropertyName("strategy")]
    public StrategyConfiguration Strategy { get; set; } = new();

    [JsonPropertyName("previousInteractions")]
    public List<InteractionHistory> PreviousInteractions { get; set; } = new();

    [JsonPropertyName("constraints")]
    public RequestConstraints Constraints { get; set; } = new();
}

public class ConversationContext
{
    [JsonPropertyName("currentStep")]
    public int CurrentStep { get; set; } = 1;

    [JsonPropertyName("totalSteps")]
    public int? TotalSteps { get; set; }

    [JsonPropertyName("workingDirectory")]
    public string WorkingDirectory { get; set; } = string.Empty;

    [JsonPropertyName("sessionStartTime")]
    public DateTime SessionStartTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("lastActivity")]
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
}

public class ToolDescription
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();

    [JsonPropertyName("examples")]
    public List<string> Examples { get; set; } = new();
}

public class StrategyConfiguration
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("riskLevel")]
    public string RiskLevel { get; set; } = "medium";

    [JsonPropertyName("requireConfirmation")]
    public bool RequireConfirmation { get; set; } = true;

    [JsonPropertyName("maxStepsPerResponse")]
    public int MaxStepsPerResponse { get; set; } = 1;

    [JsonPropertyName("analysisDepth")]
    public string AnalysisDepth { get; set; } = "thorough";
}

public class InteractionHistory
{
    [JsonPropertyName("step")]
    public int Step { get; set; }

    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("toolsUsed")]
    public List<string> ToolsUsed { get; set; } = new();

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;
}

public class RequestConstraints
{
    [JsonPropertyName("maxResponseTokens")]
    public int MaxResponseTokens { get; set; } = 2000;

    [JsonPropertyName("responseFormat")]
    public string ResponseFormat { get; set; } = "json";

    [JsonPropertyName("requireExecutableSteps")]
    public bool RequireExecutableSteps { get; set; } = true;

    [JsonPropertyName("allowedToolCategories")]
    public List<string> AllowedToolCategories { get; set; } = new();

    [JsonPropertyName("timeout")]
    public int TimeoutSeconds { get; set; } = 120;
}
