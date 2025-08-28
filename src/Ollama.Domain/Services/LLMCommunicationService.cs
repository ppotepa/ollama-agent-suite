using System.Text.Json;
using Microsoft.Extensions.Logging;
using Ollama.Domain.Models.Communication;
using Ollama.Domain.Tools;

namespace Ollama.Domain.Services;

/// <summary>
/// Service responsible for converting between internal data structures and standardized LLM communication schemas
/// </summary>
public interface ILLMCommunicationService
{
    /// <summary>
    /// Convert internal query context to standardized LLM request schema
    /// </summary>
    LLMRequestSchema CreateRequestSchema(string sessionId, string userQuery, IToolRepository toolRepository, 
        string strategy, List<InteractionHistory>? previousInteractions = null);

    /// <summary>
    /// Send request to LLM and receive response
    /// </summary>
    Task<LLMResponseSchema> SendRequestAsync(LLMRequestSchema request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parse LLM response JSON into typed response schema
    /// </summary>
    LLMResponseSchema ParseResponseSchema(string jsonResponse);

    /// <summary>
    /// Validate that the response schema contains all required fields
    /// </summary>
    (bool IsValid, List<string> ValidationErrors) ValidateResponseSchema(LLMResponseSchema response);

    /// <summary>
    /// Convert request schema to JSON string for LLM consumption
    /// </summary>
    string SerializeRequest(LLMRequestSchema request);

    /// <summary>
    /// Extract executable action from response schema
    /// </summary>
    (string toolName, Dictionary<string, object> parameters, string expectedOutcome) ExtractExecutableAction(LLMResponseSchema response);
}

public class LLMCommunicationService : ILLMCommunicationService
{
    private readonly ILogger<LLMCommunicationService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public LLMCommunicationService(ILogger<LLMCommunicationService> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public LLMRequestSchema CreateRequestSchema(string sessionId, string userQuery, IToolRepository toolRepository, 
        string strategy, List<InteractionHistory>? previousInteractions = null)
    {
        var request = new LLMRequestSchema
        {
            SessionId = sessionId,
            UserQuery = userQuery,
            Context = new ConversationContext
            {
                CurrentStep = (previousInteractions?.Count ?? 0) + 1,
                WorkingDirectory = $"cache/{sessionId}",
                SessionStartTime = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            },
            Strategy = CreateStrategyConfiguration(strategy),
            PreviousInteractions = previousInteractions ?? new List<InteractionHistory>(),
            AvailableTools = CreateToolDescriptions(toolRepository),
            Constraints = new RequestConstraints
            {
                MaxResponseTokens = 2000,
                ResponseFormat = "json",
                RequireExecutableSteps = true,
                TimeoutSeconds = 120
            }
        };

        _logger.LogDebug("Created LLM request schema for session {SessionId} with {ToolCount} tools", 
            sessionId, request.AvailableTools.Count);

        return request;
    }

    public Task<LLMResponseSchema> SendRequestAsync(LLMRequestSchema request, CancellationToken cancellationToken = default)
    {
        // Mock implementation - just returns a mock response
        _logger.LogWarning("Using mock LLM communication service - no actual LLM call made");
        
        var mockResponse = new LLMResponseSchema
        {
            SessionId = request.SessionId,
            Status = new ResponseStatus
            {
                Success = true,
                StatusCode = "MOCK",
                Message = "Mock response from LLMCommunicationService"
            },
            NextStep = new ExecutableStep
            {
                StepNumber = 1,
                Action = "Mock action",
                ToolName = "None",
                Parameters = new Dictionary<string, object>(),
                ExpectedOutcome = "Mock outcome"
            },
            Confidence = new ConfidenceMetrics
            {
                OverallConfidence = 0.5,
                AnalysisConfidence = 0.5,
                ActionConfidence = 0.5
            },
            Continuation = new ContinuationInfo
            {
                RequiresUserConfirmation = false,
                IsComplete = true,
                ProgressPercentage = 100
            }
        };

        return Task.FromResult(mockResponse);
    }

    public LLMResponseSchema ParseResponseSchema(string jsonResponse)
    {
        try
        {
            var response = JsonSerializer.Deserialize<LLMResponseSchema>(jsonResponse, _jsonOptions);
            if (response == null)
            {
                _logger.LogWarning("Failed to deserialize LLM response - null result");
                return CreateErrorResponse("Failed to parse response - null result");
            }

            _logger.LogDebug("Successfully parsed LLM response schema with status: {Status}", response.Status.StatusCode);
            return response;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM response JSON: {Response}", jsonResponse);
            return CreateErrorResponse($"JSON parsing error: {ex.Message}");
        }
    }

    public (bool IsValid, List<string> ValidationErrors) ValidateResponseSchema(LLMResponseSchema response)
    {
        var errors = new List<string>();

        // Validate required fields
        if (string.IsNullOrEmpty(response.SessionId))
            errors.Add("SessionId is required");

        if (response.Status == null)
            errors.Add("Status is required");

        if (response.NextStep == null)
            errors.Add("NextStep is required");
        else
        {
            if (string.IsNullOrEmpty(response.NextStep.Action))
                errors.Add("NextStep.Action is required");

            if (string.IsNullOrEmpty(response.NextStep.ToolName))
                errors.Add("NextStep.ToolName is required");
        }

        if (response.Confidence == null)
            errors.Add("Confidence metrics are required");
        else
        {
            if (response.Confidence.OverallConfidence < 0 || response.Confidence.OverallConfidence > 1)
                errors.Add("Confidence values must be between 0 and 1");
        }

        bool isValid = errors.Count == 0;
        
        _logger.LogDebug("Response schema validation: {IsValid}, {ErrorCount} errors", isValid, errors.Count);
        
        return (isValid, errors);
    }

    public string SerializeRequest(LLMRequestSchema request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            _logger.LogDebug("Serialized request schema for session {SessionId}", request.SessionId);
            return json;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to serialize LLM request schema");
            throw new InvalidOperationException("Failed to serialize request schema", ex);
        }
    }

    public (string toolName, Dictionary<string, object> parameters, string expectedOutcome) ExtractExecutableAction(LLMResponseSchema response)
    {
        if (response.NextStep == null)
        {
            _logger.LogWarning("No executable step found in response");
            return (string.Empty, new Dictionary<string, object>(), string.Empty);
        }

        _logger.LogDebug("Extracted executable action: {ToolName} with {ParameterCount} parameters", 
            response.NextStep.ToolName, response.NextStep.Parameters.Count);

        return (response.NextStep.ToolName, response.NextStep.Parameters, response.NextStep.ExpectedOutcome);
    }

    private StrategyConfiguration CreateStrategyConfiguration(string strategy)
    {
        return strategy.ToLowerInvariant() switch
        {
            "pessimistic" => new StrategyConfiguration
            {
                Name = "Pessimistic",
                RiskLevel = "low",
                RequireConfirmation = true,
                MaxStepsPerResponse = 1,
                AnalysisDepth = "thorough"
            },
            "aggressive" => new StrategyConfiguration
            {
                Name = "Aggressive",
                RiskLevel = "high",
                RequireConfirmation = false,
                MaxStepsPerResponse = 3,
                AnalysisDepth = "quick"
            },
            _ => new StrategyConfiguration
            {
                Name = "Balanced",
                RiskLevel = "medium",
                RequireConfirmation = true,
                MaxStepsPerResponse = 2,
                AnalysisDepth = "standard"
            }
        };
    }

    private List<ToolDescription> CreateToolDescriptions(IToolRepository toolRepository)
    {
        var descriptions = new List<ToolDescription>();
        
        foreach (var tool in toolRepository.GetAllTools())
        {
            descriptions.Add(new ToolDescription
            {
                Name = tool.GetType().Name,
                Description = GetToolDescription(tool),
                Parameters = GetToolParameters(tool),
                Examples = GetToolExamples(tool)
            });
        }

        return descriptions;
    }

    private string GetToolDescription(ITool tool)
    {
        return tool.GetType().Name switch
        {
            "MathEvaluator" => "Evaluates mathematical expressions and calculations",
            "GitHubRepositoryDownloader" => "Downloads and analyzes GitHub repositories",
            "FileSystemAnalyzer" => "Analyzes local file system structure and content",
            "CodeAnalyzer" => "Analyzes source code files for patterns and structure",
            _ => "General purpose tool"
        };
    }

    private Dictionary<string, object> GetToolParameters(ITool tool)
    {
        return tool.GetType().Name switch
        {
            "MathEvaluator" => new Dictionary<string, object> { { "expression", "string" } },
            "GitHubRepositoryDownloader" => new Dictionary<string, object> { { "repositoryUrl", "string" }, { "targetDirectory", "string" } },
            "FileSystemAnalyzer" => new Dictionary<string, object> { { "directoryPath", "string" }, { "includeSubdirectories", "boolean" } },
            "CodeAnalyzer" => new Dictionary<string, object> { { "filePath", "string" }, { "analysisType", "string" } },
            _ => new Dictionary<string, object>()
        };
    }

    private List<string> GetToolExamples(ITool tool)
    {
        return tool.GetType().Name switch
        {
            "MathEvaluator" => new List<string> { "2 + 2", "sqrt(16)", "3.14 * 2^2" },
            "GitHubRepositoryDownloader" => new List<string> { "https://github.com/user/repo" },
            "FileSystemAnalyzer" => new List<string> { "/path/to/directory", "C:\\Projects\\MyApp" },
            "CodeAnalyzer" => new List<string> { "Program.cs", "index.js" },
            _ => new List<string>()
        };
    }

    public LLMResponseSchema CreateErrorResponse(string errorMessage)
    {
        return new LLMResponseSchema
        {
            Status = new ResponseStatus
            {
                Success = false,
                StatusCode = "ERROR",
                Message = errorMessage,
                Errors = new List<string> { errorMessage }
            },
            NextStep = new ExecutableStep
            {
                Action = "Error occurred",
                ToolName = "None",
                ExpectedOutcome = "System error - no action available"
            },
            Confidence = new ConfidenceMetrics
            {
                OverallConfidence = 0.0,
                AnalysisConfidence = 0.0,
                ActionConfidence = 0.0
            }
        };
    }
}
