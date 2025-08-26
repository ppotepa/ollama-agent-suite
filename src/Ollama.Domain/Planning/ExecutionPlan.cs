using System.Text.Json.Serialization;

namespace Ollama.Domain.Planning
{
    public class ExecutionPlan
    {
        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = string.Empty;

        [JsonPropertyName("steps")]
        public List<ExecutionStep> Steps { get; set; } = new();

        [JsonPropertyName("final_response_template")]
        public string FinalResponseTemplate { get; set; } = string.Empty;

        [JsonPropertyName("is_complete")]
        public bool IsComplete { get; set; } = false;
    }

    public class ExecutionStep
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("tool")]
        public string? Tool { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("agent_type")]
        public string? AgentType { get; set; }

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; } = new();

        [JsonPropertyName("purpose")]
        public string Purpose { get; set; } = string.Empty;

        [JsonPropertyName("expected_output")]
        public string ExpectedOutput { get; set; } = string.Empty;

        [JsonPropertyName("enhanced_prompt")]
        public string? EnhancedPrompt { get; set; }

        [JsonPropertyName("fallback")]
        public ExecutionStep? Fallback { get; set; }

        [JsonPropertyName("dependencies")]
        public List<int> Dependencies { get; set; } = new();
    }

    public class ExecutionContext
    {
        public string SessionId { get; set; } = string.Empty;
        public string OriginalQuery { get; set; } = string.Empty;
        public List<ExecutionPlan> PlanHistory { get; set; } = new();
        public Dictionary<string, object> SharedState { get; set; } = new();
        public List<ExecutionResult> Results { get; set; } = new();
        public int CurrentStep { get; set; } = 0;
    }

    public class ExecutionResult
    {
        public int StepId { get; set; }
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public enum AgentType
    {
        Planning,
        Coding,
        Math,
        Research,
        General
    }

    public class AvailableModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new();
        public bool IsAvailable { get; set; }
        public string? PullCommand { get; set; }
    }
}
