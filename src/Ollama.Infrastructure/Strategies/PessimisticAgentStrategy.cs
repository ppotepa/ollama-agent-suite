using Ollama.Domain.Strategies;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Strategies;

/// <summary>
/// Pessimistic strategy that enforces careful step-by-step execution with verification
/// Always assumes the worst case and requires explicit validation before proceeding
/// </summary>
public class PessimisticAgentStrategy : IAgentStrategy
{
    private readonly ILogger<PessimisticAgentStrategy>? _logger;

    public string Name => "Pessimistic";

    public PessimisticAgentStrategy(ILogger<PessimisticAgentStrategy>? logger = null)
    {
        _logger = logger;
    }

    public string GetInitialPrompt()
    {
        return @"You are a helpful AI agent with a systematic approach to problem-solving and retry capabilities.

IMPORTANT: You must respond in JSON format only. No other text outside the JSON object.

Your response must be a valid JSON object with this exact structure:
{
  ""reasoning"": ""Your detailed step-by-step reasoning process"",
  ""taskComplete"": false,
  ""nextStep"": ""Specific action to take next"",
  ""requiresTool"": false,
  ""tool"": null,
  ""parameters"": {},
  ""confidence"": 0.0,
  ""assumptions"": [],
  ""risks"": [],
  ""response"": ""Your response to the user""
}

KEY PRINCIPLES:
1. Always respond with valid JSON only
2. Take direct, actionable steps
3. When tools fail, automatically try different approaches (retry limit: 10 attempts)
4. Break complex tasks into concrete steps
5. Provide specific, executable actions

RETRY STRATEGIES:
When a tool fails, you have multiple alternative approaches:
- GitHubDownloader fails → Try ExternalCommandExecutor with: ""git clone <repo_url>""
- Git clone fails → Try: ""powershell -Command 'Invoke-WebRequest <archive_url> -OutFile repo.zip'""
- PowerShell fails → Try: ""curl -L <archive_url> -o repo.zip""
- Download fails → Try: ""wget <archive_url>""

ACTION REQUIREMENTS:
The 'nextStep' field should contain direct actions like:
- ""Use GitHubDownloader tool to download repository https://github.com/user/repo""
- ""Use FileSystemAnalyzer tool to analyze directory ./downloads/repo""
- ""Use CodeAnalyzer tool to read file ./downloads/repo/Program.cs""
- ""Use MathEvaluator tool to calculate expression: 15 * 8 + 42""
- ""Use ExternalCommandExecutor tool to run: git clone https://github.com/user/repo""

TOOL PARAMETERS:
GitHubDownloader: ""repoUrl"", ""sessionId""
FileSystemAnalyzer: ""directoryPath"", ""includeSubdirectories"", ""minimumFileSize""
CodeAnalyzer: ""filePath"", ""analysisType""
MathEvaluator: ""expression""
ExternalCommandExecutor: ""command"", ""workingDirectory"" (optional), ""timeoutSeconds"" (optional)

COMPLETION CRITERIA:
Only set taskComplete to true when you have fully answered the user's question with verified results.

Remember: Respond only with valid JSON. No additional text.";
    }

    public string FormatQueryPrompt(string userQuery, string? sessionId = null)
    {
        var sessionInfo = !string.IsNullOrEmpty(sessionId) ? $" (Session: {sessionId})" : "";
        
        return $@"Process the following request with maximum caution and verification{sessionInfo}:

User Query: {userQuery}

CRITICAL REQUIREMENTS:
1. Respond only in JSON format
2. Break this down into the smallest possible verified steps
3. Your 'nextStep' must be a SPECIFIC EXECUTABLE ACTION, not a description
4. If you need to download a repository, use GitHubDownloader tool
5. If you need to analyze file sizes, use FileSystemAnalyzer tool  
6. If you need to read code content, use CodeAnalyzer tool
7. Consider what could go wrong at each step
8. Use tools when you need to verify information
9. Never assume anything works without verification

FIRST STEP GUIDANCE:
- For repository analysis: Start with ""Use GitHubDownloader tool to download the repository from [URL]""
- For file analysis: Start with ""Use FileSystemAnalyzer tool to analyze the downloaded repository structure""
- For code reading: Start with ""Use CodeAnalyzer tool to read and analyze [specific file]""

Make your nextStep immediately actionable!";
    }

    public bool IsTaskComplete(string response)
    {
        try
        {
            var responseObject = JsonSerializer.Deserialize<JsonElement>(response);
            
            if (responseObject.TryGetProperty("taskComplete", out var taskCompleteElement))
            {
                var isComplete = taskCompleteElement.GetBoolean();
                
                // Pessimistic validation - also check confidence level
                if (isComplete && responseObject.TryGetProperty("confidence", out var confidenceElement))
                {
                    var confidence = confidenceElement.GetDouble();
                    if (confidence < 0.8)
                    {
                        _logger?.LogWarning("Task marked complete but confidence too low: {Confidence}", confidence);
                        return false; // Override completion if confidence is too low
                    }
                }
                
                return isComplete;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error parsing task completion status from response");
            return false; // Pessimistic default
        }
    }

    public string GetNextStep(string response)
    {
        try
        {
            var responseObject = JsonSerializer.Deserialize<JsonElement>(response);
            
            if (responseObject.TryGetProperty("nextStep", out var nextStepElement))
            {
                return nextStepElement.GetString() ?? "Continue with careful analysis";
            }
            
            return "Analyze the problem more thoroughly";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error extracting next step from response");
            return "Re-evaluate the approach due to parsing error";
        }
    }

    public (string toolName, Dictionary<string, string> parameters)? ExtractToolRequest(string response)
    {
        try
        {
            var responseObject = JsonSerializer.Deserialize<JsonElement>(response);
            
            if (!responseObject.TryGetProperty("requiresTool", out var requiresToolElement) || 
                !requiresToolElement.GetBoolean())
            {
                return null;
            }
                
            if (!responseObject.TryGetProperty("tool", out var toolElement))
            {
                _logger?.LogWarning("Tool required but no tool specified");
                return null;
            }
            
            string toolName;
            var parameters = new Dictionary<string, string>();
            
            // Handle both flat and nested tool formats
            if (toolElement.ValueKind == JsonValueKind.String)
            {
                // Flat format: "tool": "ToolName"
                toolName = toolElement.GetString() ?? string.Empty;
                
                // Get parameters from top-level "parameters" property
                if (responseObject.TryGetProperty("parameters", out var paramsElement))
                {
                    foreach (var param in paramsElement.EnumerateObject())
                    {
                        var value = param.Value.ValueKind == JsonValueKind.String 
                            ? param.Value.GetString() 
                            : param.Value.ToString();
                        
                        if (value != null)
                        {
                            parameters.Add(param.Name, value);
                        }
                    }
                }
            }
            else if (toolElement.ValueKind == JsonValueKind.Object)
            {
                // Nested format: "tool": {"name": "ToolName", "parameters": {...}}
                if (toolElement.TryGetProperty("name", out var nameElement))
                {
                    toolName = nameElement.GetString() ?? string.Empty;
                }
                else
                {
                    _logger?.LogWarning("Tool object provided but no 'name' property found");
                    return null;
                }
                
                // Get parameters from nested "parameters" property
                if (toolElement.TryGetProperty("parameters", out var nestedParamsElement))
                {
                    foreach (var param in nestedParamsElement.EnumerateObject())
                    {
                        var value = param.Value.ValueKind == JsonValueKind.String 
                            ? param.Value.GetString() 
                            : param.Value.ToString();
                        
                        if (value != null)
                        {
                            parameters.Add(param.Name, value);
                        }
                    }
                }
            }
            else
            {
                _logger?.LogWarning("Tool property has unexpected format: {ValueKind}", toolElement.ValueKind);
                return null;
            }
                
            if (string.IsNullOrEmpty(toolName))
            {
                _logger?.LogWarning("Tool required but tool name is empty");
                return null;
            }
            
            // Pessimistic validation - ensure we have the minimum required info
            if (parameters.Count == 0)
            {
                _logger?.LogWarning("Tool {Tool} requested but no parameters provided", toolName);
            }
            
            return (toolName, parameters);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error extracting tool request from response");
            return null; // Pessimistic default
        }
    }

    public string FormatToolResponse(string toolName, string toolResponse)
    {
        return $@"Tool '{toolName}' execution completed. 

TOOL RESULT:
{toolResponse}

Now analyze this result carefully:
1. Does this result answer the user's question?
2. Are there any errors or inconsistencies in the result?
3. Do you need additional tools to verify this information?
4. What are the implications of this result?
5. Are there any edge cases or risks to consider?

Continue processing with extreme caution. Verify everything before drawing conclusions.";
    }

    public string ValidateResponse(string response, string? sessionId = null)
    {
        try
        {
            // First, ensure it's valid JSON
            var responseObject = JsonSerializer.Deserialize<JsonElement>(response);
            
            // Check required fields
            var requiredFields = new[] { "reasoning", "taskComplete", "nextStep", "requiresTool", "response" };
            var missingFields = new List<string>();
            
            foreach (var field in requiredFields)
            {
                if (!responseObject.TryGetProperty(field, out _))
                {
                    missingFields.Add(field);
                }
            }
            
            if (missingFields.Count > 0)
            {
                _logger?.LogError("Response missing required fields: {Fields}", string.Join(", ", missingFields));
                return CreateErrorResponse($"Invalid response format. Missing fields: {string.Join(", ", missingFields)}");
            }
            
            // Extract key information for validation
            var reasoning = responseObject.GetProperty("reasoning").GetString() ?? "";
            var taskComplete = responseObject.GetProperty("taskComplete").GetBoolean();
            var confidence = responseObject.TryGetProperty("confidence", out var confElement) 
                ? confElement.GetDouble() 
                : 0.5;
            
            // Pessimistic validation rules
            if (taskComplete && confidence < 0.8)
            {
                _logger?.LogWarning("Task marked complete but confidence insufficient: {Confidence}", confidence);
                return CreateErrorResponse("Task completion requires higher confidence level (>= 0.8)");
            }
            
            if (reasoning.Length < 30)
            {
                _logger?.LogWarning("Reasoning too brief: {Length} characters", reasoning.Length);
                return CreateErrorResponse("Reasoning must be more detailed and thorough");
            }
            
            return response; // Response is valid
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "Response is not valid JSON");
            return CreateErrorResponse($"Response must be valid JSON format. Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error validating response");
            return CreateErrorResponse($"Validation error: {ex.Message}");
        }
    }

    public string HandleError(string error, string context)
    {
        _logger?.LogError("Error in {Context}: {Error}", context, error);
        
        return CreateErrorResponse($"Error in {context}: {error}. Reassessing approach with maximum caution.");
    }

    private string CreateErrorResponse(string errorMessage)
    {
        var errorResponse = new
        {
            reasoning = $"Error encountered: {errorMessage}. Need to reassess the situation.",
            taskComplete = false,
            nextStep = "Analyze the error and determine corrective action",
            requiresTool = false,
            tool = (string?)null,
            parameters = new Dictionary<string, string>(),
            confidence = 0.1,
            assumptions = new[] { "Previous approach had issues" },
            risks = new[] { "May need to change strategy", "Could require different tools" },
            response = $"I encountered an issue: {errorMessage}. Let me reconsider the approach."
        };
        
        return JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { WriteIndented = true });
    }
}
