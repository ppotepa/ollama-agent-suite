using System.Text.Json.Serialization;

namespace Ollama.Domain.Models.Communication;

/// <summary>
/// Schema for responses received FROM the LLM - ensures consistent output parsing
/// </summary>
public class LLMResponseSchema
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public ResponseStatus Status { get; set; } = new();

    [JsonPropertyName("analysis")]
    public AnalysisResult Analysis { get; set; } = new();

    [JsonPropertyName("nextStep")]
    public ExecutableStep NextStep { get; set; } = new();

    [JsonPropertyName("reasoning")]
    public ReasoningProcess Reasoning { get; set; } = new();

    [JsonPropertyName("confidence")]
    public ConfidenceMetrics Confidence { get; set; } = new();

    [JsonPropertyName("continuation")]
    public ContinuationInfo Continuation { get; set; } = new();

    [JsonPropertyName("metadata")]
    public ResponseMetadata Metadata { get; set; } = new();
}

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

public class AnalysisResult
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("keyFindings")]
    public List<string> KeyFindings { get; set; } = new();

    [JsonPropertyName("identifiedPatterns")]
    public List<string> IdentifiedPatterns { get; set; } = new();

    [JsonPropertyName("riskAssessment")]
    public RiskAssessment RiskAssessment { get; set; } = new();

    [JsonPropertyName("recommendations")]
    public List<string> Recommendations { get; set; } = new();
}

public class ExecutableStep
{
    [JsonPropertyName("stepNumber")]
    public int StepNumber { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("toolName")]
    public string ToolName { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new();

    [JsonPropertyName("expectedOutcome")]
    public string ExpectedOutcome { get; set; } = string.Empty;

    [JsonPropertyName("validationCriteria")]
    public List<string> ValidationCriteria { get; set; } = new();

    [JsonPropertyName("rollbackPlan")]
    public string RollbackPlan { get; set; } = string.Empty;

    [JsonPropertyName("estimatedDuration")]
    public int EstimatedDurationSeconds { get; set; }
}

public class ReasoningProcess
{
    [JsonPropertyName("approach")]
    public string Approach { get; set; } = string.Empty;

    [JsonPropertyName("alternativesConsidered")]
    public List<string> AlternativesConsidered { get; set; } = new();

    [JsonPropertyName("decisionFactors")]
    public List<string> DecisionFactors { get; set; } = new();

    [JsonPropertyName("assumptions")]
    public List<string> Assumptions { get; set; } = new();

    [JsonPropertyName("riskMitigation")]
    public List<string> RiskMitigation { get; set; } = new();
}

public class ConfidenceMetrics
{
    [JsonPropertyName("overallConfidence")]
    public double OverallConfidence { get; set; } = 0.75;

    [JsonPropertyName("analysisConfidence")]
    public double AnalysisConfidence { get; set; } = 0.75;

    [JsonPropertyName("actionConfidence")]
    public double ActionConfidence { get; set; } = 0.75;

    [JsonPropertyName("uncertaintyFactors")]
    public List<string> UncertaintyFactors { get; set; } = new();

    [JsonPropertyName("confidenceJustification")]
    public string ConfidenceJustification { get; set; } = string.Empty;
}

public class ContinuationInfo
{
    [JsonPropertyName("requiresUserConfirmation")]
    public bool RequiresUserConfirmation { get; set; } = true;

    [JsonPropertyName("nextExpectedInput")]
    public string NextExpectedInput { get; set; } = string.Empty;

    [JsonPropertyName("isComplete")]
    public bool IsComplete { get; set; } = false;

    [JsonPropertyName("progressPercentage")]
    public int ProgressPercentage { get; set; } = 0;

    [JsonPropertyName("estimatedRemainingSteps")]
    public int EstimatedRemainingSteps { get; set; } = 0;
}

public class ResponseMetadata
{
    [JsonPropertyName("responseTime")]
    public DateTime ResponseTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modelUsed")]
    public string ModelUsed { get; set; } = string.Empty;

    [JsonPropertyName("tokensUsed")]
    public int TokensUsed { get; set; } = 0;

    [JsonPropertyName("processingDuration")]
    public int ProcessingDurationMs { get; set; } = 0;

    [JsonPropertyName("strategyApplied")]
    public string StrategyApplied { get; set; } = string.Empty;
}

public class RiskAssessment
{
    [JsonPropertyName("riskLevel")]
    public string RiskLevel { get; set; } = "medium";

    [JsonPropertyName("identifiedRisks")]
    public List<string> IdentifiedRisks { get; set; } = new();

    [JsonPropertyName("mitigationStrategies")]
    public List<string> MitigationStrategies { get; set; } = new();

    [JsonPropertyName("impactAssessment")]
    public string ImpactAssessment { get; set; } = string.Empty;
}
