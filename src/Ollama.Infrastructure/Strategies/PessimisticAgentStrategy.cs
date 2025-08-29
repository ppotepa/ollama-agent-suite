using Ollama.Domain.Strategies;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using System.IO;
using Ollama.Domain.Tools;
using Ollama.Infrastructure.Prompts;
using Ollama.Domain.Configuration;

namespace Ollama.Infrastructure.Strategies;

/// <summary>
/// Pessimistic strategy that enforces careful step-by-step execution with verification
/// Always assumes the worst case and requires explicit validation before proceeding
/// </summary>
public class PessimisticAgentStrategy : IAgentStrategy
{
    private readonly ILogger<PessimisticAgentStrategy>? _logger;
    private readonly PromptService _promptService;
    private readonly PromptConfiguration _promptConfiguration;

    public string Name => "Pessimistic";

    public PessimisticAgentStrategy(
        ILogger<PessimisticAgentStrategy>? logger = null,
        PromptService? promptService = null,
        PromptConfiguration? promptConfiguration = null)
    {
        _logger = logger;
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
        _promptConfiguration = promptConfiguration ?? throw new ArgumentNullException(nameof(promptConfiguration));
    }

    public string GetInitialPrompt()
    {
        try
        {
            // Use the new PromptService to load and process the prompt template
            // Note: This is a synchronous method, so we need to handle the async call
            var promptTask = _promptService.GetProcessedPromptAsync(
                _promptConfiguration.PessimisticPromptFileName, 
                "initial-load");
            
            return promptTask.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading prompt template from PromptService");
            throw new InvalidOperationException("Failed to load pessimistic prompt template. Ensure the prompt file exists and is properly configured.", ex);
        }
    }

    public string FormatQueryPrompt(string userQuery, string? sessionId = null)
    {
        _logger?.LogDebug("FormatQueryPrompt called with userQuery: '{UserQuery}', sessionId: '{SessionId}'", 
            userQuery ?? "NULL", sessionId ?? "NULL");
            
        // Simple format without external template - the user query is passed directly to the LLM
        // The pessimistic system prompt already contains all necessary instructions for JSON-only responses
        var sessionInfo = !string.IsNullOrEmpty(sessionId) ? $" (Session: {sessionId})" : "";
        var formattedQuery = $"User Query: {userQuery ?? ""}{sessionInfo}";
        
        _logger?.LogDebug("Formatted query length: {Length}", formattedQuery.Length);
        return formattedQuery;
    }

    public bool IsTaskComplete(string response)
    {
        try
        {
            var responseObject = JsonSerializer.Deserialize<JsonElement>(response);
            
            // Check for taskCompleted (new primary field)
            if (responseObject.TryGetProperty("taskCompleted", out var taskCompletedElement))
            {
                var isCompleted = taskCompletedElement.GetBoolean();
                
                // If task is completed, verify nextStep is null (completion logic)
                if (isCompleted)
                {
                    if (responseObject.TryGetProperty("nextStep", out var nextStepElement))
                    {
                        if (nextStepElement.ValueKind == JsonValueKind.Null)
                        {
                            return true; // Task completed and no next step - truly done
                        }
                        else
                        {
                            // Task marked completed but has next step - more work to do
                            _logger?.LogInformation("Task marked completed but nextStep present - continuing");
                            return false;
                        }
                    }
                    else
                    {
                        // No nextStep property - assume completed
                        return true;
                    }
                }
                
                return false; // taskCompleted is false
            }
            
            // Fallback to old taskComplete field for compatibility
            if (responseObject.TryGetProperty("taskComplete", out var taskCompleteElement))
            {
                var isComplete = taskCompleteElement.GetBoolean();
                
                // Pessimistic validation - also check confidence level if available
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
                // Handle new object structure
                if (nextStepElement.ValueKind == JsonValueKind.Object)
                {
                    if (nextStepElement.TryGetProperty("reasoning", out var reasoningElement))
                    {
                        return reasoningElement.GetString() ?? "Continue with careful analysis";
                    }
                    return "Proceed with the planned next step";
                }
                // Handle legacy string structure for backwards compatibility
                else if (nextStepElement.ValueKind == JsonValueKind.String)
                {
                    return nextStepElement.GetString() ?? "Continue with careful analysis";
                }
                // Handle null (task completed)
                else if (nextStepElement.ValueKind == JsonValueKind.Null)
                {
                    return "Task completed - no further steps needed";
                }
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
            
            // Check if nextStep contains tool information (new structure)
            if (responseObject.TryGetProperty("nextStep", out var nextStepElement) && 
                nextStepElement.ValueKind == JsonValueKind.Object)
            {
                if (nextStepElement.TryGetProperty("requiresTool", out var requiresToolElement) && 
                    requiresToolElement.GetBoolean())
                {
                    if (nextStepElement.TryGetProperty("tool", out var toolElement))
                    {
                        string toolName = toolElement.GetString() ?? string.Empty;
                        var parameters = new Dictionary<string, string>();
                        
                        // Get parameters from nextStep.parameters
                        if (nextStepElement.TryGetProperty("parameters", out var paramsElement))
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
                        
                        return (toolName, parameters);
                    }
                    else
                    {
                        _logger?.LogWarning("Tool required in nextStep but no tool specified");
                        return null;
                    }
                }
                
                return null; // nextStep present but requiresTool is false
            }
            
            // Fallback to legacy structure for backwards compatibility
            if (!responseObject.TryGetProperty("requiresTool", out var legacyRequiresToolElement) || 
                !legacyRequiresToolElement.GetBoolean())
            {
                return null;
            }
                
            if (!responseObject.TryGetProperty("tool", out var legacyToolElement))
            {
                _logger?.LogWarning("Tool required but no tool specified");
                return null;
            }
            
            string legacyToolName;
            var legacyParameters = new Dictionary<string, string>();
            
            // Handle both flat and nested tool formats
            if (legacyToolElement.ValueKind == JsonValueKind.String)
            {
                // Flat format: "tool": "ToolName"
                legacyToolName = legacyToolElement.GetString() ?? string.Empty;
                
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
                            legacyParameters.Add(param.Name, value);
                        }
                    }
                }
            }
            else if (legacyToolElement.ValueKind == JsonValueKind.Object)
            {
                // Nested format: "tool": {"name": "ToolName", "parameters": {...}}
                if (legacyToolElement.TryGetProperty("name", out var nameElement))
                {
                    legacyToolName = nameElement.GetString() ?? string.Empty;
                }
                else
                {
                    _logger?.LogWarning("Tool object provided but no 'name' property found");
                    return null;
                }
                
                // Get parameters from nested "parameters" property
                if (legacyToolElement.TryGetProperty("parameters", out var nestedParamsElement))
                {
                    foreach (var param in nestedParamsElement.EnumerateObject())
                    {
                        var value = param.Value.ValueKind == JsonValueKind.String 
                            ? param.Value.GetString() 
                            : param.Value.ToString();
                        
                        if (value != null)
                        {
                            legacyParameters.Add(param.Name, value);
                        }
                    }
                }
            }
            else
            {
                _logger?.LogWarning("Tool property has unexpected format: {ValueKind}", legacyToolElement.ValueKind);
                return null;
            }
                
            if (string.IsNullOrEmpty(legacyToolName))
            {
                _logger?.LogWarning("Tool required but tool name is empty");
                return null;
            }
            
            // Pessimistic validation - ensure we have the minimum required info
            if (legacyParameters.Count == 0)
            {
                _logger?.LogWarning("Tool {Tool} requested but no parameters provided", legacyToolName);
            }
            
            return (legacyToolName, legacyParameters);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error extracting tool request from response");
            return null; // Pessimistic default
        }
    }

    public string FormatToolResponse(string toolName, string toolResponse)
    {
        try
        {
            // Parse the tool response to extract useful information
            var isSuccessful = !toolResponse.ToLowerInvariant().Contains("failed") && 
                               !toolResponse.ToLowerInvariant().Contains("error") &&
                               !toolResponse.ToLowerInvariant().Contains("exhausted");
            
            if (isSuccessful)
            {
                return FormatSuccessfulToolResponse(toolName, toolResponse);
            }
            else
            {
                return FormatFailedToolResponse(toolName, toolResponse);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error formatting tool response for {Tool}", toolName);
            return FormatGenericToolResponse(toolName, toolResponse);
        }
    }
    
    private string FormatSuccessfulToolResponse(string toolName, string toolResponse)
    {
        var responseInfo = ExtractToolResponseInformation(toolResponse);
        
        return $@"✅ Tool '{toolName}' executed successfully!

EXECUTION SUMMARY:
==================
Status: SUCCESS
Tool: {toolName}
Working Directory: {responseInfo.WorkingDirectory ?? "session root"}
Execution Time: {responseInfo.ExecutionTime ?? "completed"}

RESULTS:
========
{toolResponse}

NEXT ACTION GUIDANCE:
====================
The tool executed successfully and provided results above. You now have:
1. ✅ Access to the results in your current working directory: {responseInfo.WorkingDirectory ?? "session root"}
2. ✅ Data is ready for analysis or further processing
3. ✅ You can now use other tools to work with this data

KEY CONTEXT FOR LLM:
- Current cursor position: {responseInfo.WorkingDirectory ?? "session root"}
- Tool execution completed successfully
- Results are available for immediate use
- No retry needed - proceed with next step

AVAILABLE OPTIONS:
- Use FileSystemAnalyzer to explore downloaded/created content
- Use PrintWorkingDirectory to see current location
- Use other tools to process the results
- Continue with your analysis based on successful results

IMPORTANT: The tool completed successfully. Use the results above to continue your task.";
    }
    
    private string FormatFailedToolResponse(string toolName, string toolResponse)
    {
        return $@"❌ Tool '{toolName}' execution encountered issues.

EXECUTION SUMMARY:
==================
Status: FAILED
Tool: {toolName}
Issue: Tool execution did not complete successfully

DETAILED RESULT:
===============
{toolResponse}

NEXT ACTION GUIDANCE:
====================
The tool execution failed. You should:
1. ❌ Analyze the error details above
2. ❌ Consider alternative approaches
3. ❌ Try different tool parameters
4. ❌ Use fallback strategies

RETRY STRATEGIES AVAILABLE:
- For GitHubDownloader: Try ExternalCommandExecutor with git clone
- For FileSystemAnalyzer: Try DirectoryList or PrintWorkingDirectory  
- For download failures: Try different URLs or manual approaches
- General: Break the task into smaller steps

IMPORTANT: Tool failed - implement fallback strategy before proceeding.";
    }
    
    private string FormatGenericToolResponse(string toolName, string toolResponse)
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
    
    private (string? WorkingDirectory, string? ExecutionTime) ExtractToolResponseInformation(string toolResponse)
    {
        string? workingDirectory = null;
        string? executionTime = null;
        
        try
        {
            // Extract working directory information
            var lines = toolResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var lowerLine = line.ToLowerInvariant();
                
                // Look for working directory indicators
                if (lowerLine.Contains("working directory:") || lowerLine.Contains("current directory:") || 
                    lowerLine.Contains("extracted to:") || lowerLine.Contains("downloaded to:") ||
                    lowerLine.Contains("created directory:") || lowerLine.Contains("target:"))
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        workingDirectory = parts[1].Trim();
                    }
                }
                
                // Look for execution time
                if (lowerLine.Contains("duration:") || lowerLine.Contains("execution time:") ||
                    lowerLine.Contains("completed in"))
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        executionTime = parts[1].Trim();
                    }
                }
            }
            
            // If no specific working directory found, try to extract paths
            if (string.IsNullOrEmpty(workingDirectory))
            {
                // Look for path-like patterns
                foreach (var line in lines)
                {
                    if (line.Contains("\\") || line.Contains("/"))
                    {
                        // This might be a path
                        var potentialPath = line.Trim();
                        if (potentialPath.Contains("cache") || potentialPath.Contains("session"))
                        {
                            workingDirectory = potentialPath;
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error extracting tool response information");
        }
        
        return (workingDirectory, executionTime);
    }

    public string ValidateResponse(string response, string? sessionId = null)
    {
        try
        {
            _logger?.LogDebug("Validating LLM response for session {SessionId}. Response length: {Length} chars", 
                sessionId, response?.Length ?? 0);
            _logger?.LogDebug("Raw LLM response content: {Response}", response);
            
            if (string.IsNullOrWhiteSpace(response))
            {
                _logger?.LogError("Empty or null response received for session {SessionId}", sessionId);
                return CreateErrorResponse("Empty response from LLM");
            }
            
            // Try multiple parsing strategies with fallbacks
            var responseObject = TryParseWithFallbacks(response, sessionId);
            if (responseObject == null || !responseObject.HasValue)
            {
                _logger?.LogError("All parsing strategies failed for session {SessionId}", sessionId);
                return CreateErrorResponse("Could not parse response in any supported format");
            }

            var responseObjectValue = responseObject.Value;
            _logger?.LogDebug("JSON parsing successful for session {SessionId}", sessionId);
            
            if (responseObject == null)
            {
                _logger?.LogError("Failed to parse response for session {SessionId}", sessionId);
                return CreateErrorResponse("Could not parse response in any supported format");
            }
            
            var actualResponseObject = responseObject.Value;
            
            // Check required fields for new structure
            var requiredFields = new[] { "taskCompleted", "nextStep", "response" };
            var missingFields = new List<string>();
            
            foreach (var field in requiredFields)
            {
                if (!responseObjectValue.TryGetProperty(field, out _))
                {
                    missingFields.Add(field);
                }
            }
            
            // If we're missing critical fields, try to normalize the response
            if (missingFields.Count > 0)
            {
                _logger?.LogWarning("Response missing some expected fields: {Fields}. Attempting to normalize...", 
                    string.Join(", ", missingFields));
                
                var normalizedResponse = NormalizeResponseFormat(responseObjectValue, sessionId ?? "unknown");
                return JsonSerializer.Serialize(normalizedResponse, new JsonSerializerOptions { WriteIndented = true });
            }
            
            // Extract key information for validation
            var taskCompleted = responseObjectValue.GetProperty("taskCompleted").GetBoolean();
            var nextStep = responseObjectValue.TryGetProperty("nextStep", out var nextStepElement) ? nextStepElement : (JsonElement?)null;
            
            // Validate nextStep structure if present
            if (nextStep.HasValue && nextStep.Value.ValueKind == JsonValueKind.Object)
            {
                var confidence = nextStep.Value.TryGetProperty("confidence", out var confElement) 
                    ? confElement.GetDouble() 
                    : 0.5; // Default confidence
                
                if (confidence < 0.1 || confidence > 1.0)
                {
                    _logger?.LogWarning("Invalid confidence level: {Confidence}. Should be between 0.1 and 1.0", confidence);
                    return "Invalid confidence level in nextStep. Please provide confidence between 0.1 and 1.0.";
                }
            }
            
            // Legacy field validation for backwards compatibility
            var legacyTaskComplete = responseObjectValue.TryGetProperty("taskComplete", out var taskCompleteElement) 
                ? taskCompleteElement.GetBoolean() 
                : taskCompleted;
            var legacyConfidence = responseObjectValue.TryGetProperty("confidence", out var legacyConfElement) 
                ? legacyConfElement.GetDouble() 
                : 0.5;
            
            // Pessimistic validation rules - only require confidence for ongoing tasks
            // If task is complete (taskCompleted: true, nextStep: null), confidence is not required
            bool isTaskComplete = taskCompleted && (!nextStep.HasValue || nextStep.Value.ValueKind == JsonValueKind.Null);
            if (legacyTaskComplete && !isTaskComplete && legacyConfidence < 0.8)
            {
                _logger?.LogWarning("Task marked complete but confidence insufficient: {Confidence}", legacyConfidence);
                return CreateErrorResponse("Task completion requires higher confidence level (>= 0.8)");
            }
            
            // Extract reasoning from nextStep for validation
            string reasoning = "";
            if (nextStep.HasValue && nextStep.Value.ValueKind == JsonValueKind.Object)
            {
                if (nextStep.Value.TryGetProperty("reasoning", out var reasoningElement))
                {
                    reasoning = reasoningElement.GetString() ?? "";
                }
            }
            
            // Legacy reasoning validation (for backwards compatibility)
            if (string.IsNullOrEmpty(reasoning) && responseObjectValue.TryGetProperty("reasoning", out var legacyReasoningElement))
            {
                reasoning = legacyReasoningElement.GetString() ?? "";
            }
            
            if (!string.IsNullOrEmpty(reasoning) && reasoning.Length < 30)
            {
                _logger?.LogWarning("Reasoning too brief: {Length} characters", reasoning.Length);
                return CreateErrorResponse("Reasoning must be more detailed and thorough");
            }
            
            return response; // Response is valid
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "JSON parsing failed for session {SessionId}. Response length: {Length} chars", 
                sessionId, response?.Length ?? 0);
            _logger?.LogError("Invalid JSON response content: {Response}", response);
            _logger?.LogError("JSON parsing error details: Path: {Path}, LineNumber: {LineNumber}, BytePosition: {BytePosition}", 
                ex.Path, ex.LineNumber, ex.BytePositionInLine);
            
            // Try to extract any valid JSON from the beginning
            if (!string.IsNullOrEmpty(response))
            {
                var firstBrace = response.IndexOf('{');
                var lastBrace = response.LastIndexOf('}');
                if (firstBrace >= 0 && lastBrace > firstBrace)
                {
                    var possibleJson = response.Substring(firstBrace, lastBrace - firstBrace + 1);
                    _logger?.LogError("Possible JSON content extracted: {PossibleJson}", possibleJson);
                }
            }
            
            return CreateErrorResponse($"Response must be valid JSON format. Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error validating response for session {SessionId}", sessionId);
            _logger?.LogError("Response content that caused error: {Response}", response);
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

    // Returns the initial prompt with appended external tool info
    public Task<string> GetInitialPromptWithExternalToolsAsync()
    {
        var basePrompt = GetInitialPrompt();
        try
        {
            // Skip external command detection for now to avoid hanging
            // var detector = new Ollama.Infrastructure.Tools.ExternalCommandDetector();
            // var commands = await detector.DetectAvailableCommandsAsync();
            // var toolInfo = detector.GetAvailableCommandsDescription();
            // if (!string.IsNullOrWhiteSpace(toolInfo))
            // {
            //     basePrompt += "\n\n" + toolInfo.Trim();
            // }
            
            // Just add a simple note about external tools
            basePrompt += "\n\nNote: External command tools may be available but detection is temporarily disabled.";
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not append external tool info to initial prompt");
        }
        return Task.FromResult(basePrompt);
    }

    // Returns the initial prompt with dynamically generated tool information
    public Task<string> GetInitialPromptWithDynamicToolsAsync(IToolRepository toolRepository)
    {
        var basePrompt = GetInitialPrompt();
        try
        {
            // Generate tool information using reflection
            var toolInfo = Ollama.Infrastructure.Tools.ToolInfoGenerator.GenerateToolInformation(toolRepository);
            if (!string.IsNullOrWhiteSpace(toolInfo))
            {
                // Replace the placeholder with dynamic tool information
                var placeholder = "[REFLECTION.TOOLS]";
                if (basePrompt.Contains(placeholder))
                {
                    basePrompt = basePrompt.Replace(placeholder, toolInfo.Trim());
                }
                else
                {
                    // Fallback: if placeholder not found, try to replace the section
                    var toolSectionStart = basePrompt.IndexOf("AVAILABLE TOOLS AND THEIR USAGE:");
                    if (toolSectionStart >= 0)
                    {
                        var toolSectionEnd = basePrompt.IndexOf("DECISION MAKING:", toolSectionStart);
                        if (toolSectionEnd >= 0)
                        {
                            var beforeTools = basePrompt.Substring(0, toolSectionStart);
                            var afterTools = basePrompt.Substring(toolSectionEnd);
                            basePrompt = beforeTools + "AVAILABLE TOOLS AND THEIR USAGE:\n================================\n" + toolInfo + "\n" + afterTools;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not generate dynamic tool information");
        }
        return Task.FromResult(basePrompt);
    }
    
    private Dictionary<string, object> NormalizeResponseFormat(JsonElement responseObject, string sessionId)
    {
        var normalizedData = new Dictionary<string, object>();
        
        // Default values for new structure
        normalizedData["taskCompleted"] = false;
        normalizedData["nextStep"] = new Dictionary<string, object>
        {
            ["reasoning"] = "Processing user request",
            ["requiresTool"] = false,
            ["tool"] = "",
            ["parameters"] = new Dictionary<string, string>(),
            ["confidence"] = 0.5,
            ["assumptions"] = new List<string>(),
            ["risks"] = new List<string>()
        };
        normalizedData["response"] = "Processing...";
        
        // Legacy compatibility fields
        normalizedData["taskComplete"] = false; // Legacy field
        normalizedData["stepCompleted"] = false; // Legacy field
        
        // Extract values from response
        string reasoning = "Processing user request";
        bool taskCompleted = false;
        bool requiresTool = false;
        string? tool = null;
        var parameters = new Dictionary<string, string>();
        double confidence = 0.5;
        var assumptions = new List<string>();
        var risks = new List<string>();
        string response = "Processing...";
        
        // Map from various LLM response formats to our standard format
        foreach (var property in responseObject.EnumerateObject())
        {
            var propertyName = property.Name.ToLowerInvariant().Replace(" ", "").Replace("_", "");
            switch (propertyName)
            {
                case "reasoning":
                case "thought":
                case "analysis":
                    reasoning = property.Value.GetString() ?? "Processing user request";
                    break;
                    
                case "taskcomplete":
                case "taskCompleted":
                case "complete":
                case "finished":
                case "done":
                    taskCompleted = property.Value.GetBoolean();
                    normalizedData["taskComplete"] = taskCompleted; // Legacy compatibility
                    break;
                    
                case "stepcompleted":
                case "stepfinished":
                case "stepdone":
                    normalizedData["stepCompleted"] = property.Value.GetBoolean(); // Legacy compatibility
                    break;
                    
                case "nextstep":
                case "nextaction":
                case "action":
                case "step":
                    // Handle both string and object formats for nextStep
                    if (property.Value.ValueKind == JsonValueKind.Object)
                    {
                        // Already in new format - copy it
                        var nextStepDict = new Dictionary<string, object>();
                        foreach (var nextStepProp in property.Value.EnumerateObject())
                        {
                            switch (nextStepProp.Name.ToLowerInvariant())
                            {
                                case "reasoning":
                                    nextStepDict["reasoning"] = nextStepProp.Value.GetString() ?? reasoning;
                                    break;
                                case "requirestool":
                                    nextStepDict["requiresTool"] = nextStepProp.Value.GetBoolean();
                                    break;
                                case "tool":
                                    nextStepDict["tool"] = nextStepProp.Value.GetString() ?? "";
                                    break;
                                case "parameters":
                                    var paramsDict = new Dictionary<string, string>();
                                    foreach (var param in nextStepProp.Value.EnumerateObject())
                                    {
                                        paramsDict[param.Name] = param.Value.GetString() ?? "";
                                    }
                                    nextStepDict["parameters"] = paramsDict;
                                    break;
                                case "confidence":
                                    nextStepDict["confidence"] = nextStepProp.Value.GetDouble();
                                    break;
                                case "assumptions":
                                    var assumptionsList = new List<string>();
                                    foreach (var assumption in nextStepProp.Value.EnumerateArray())
                                    {
                                        assumptionsList.Add(assumption.GetString() ?? "");
                                    }
                                    nextStepDict["assumptions"] = assumptionsList;
                                    break;
                                case "risks":
                                    var risksList = new List<string>();
                                    foreach (var risk in nextStepProp.Value.EnumerateArray())
                                    {
                                        risksList.Add(risk.GetString() ?? "");
                                    }
                                    nextStepDict["risks"] = risksList;
                                    break;
                            }
                        }
                        normalizedData["nextStep"] = nextStepDict;
                    }
                    else
                    {
                        // Legacy string format - put in reasoning
                        reasoning = property.Value.GetString() ?? "Continue with analysis";
                    }
                    break;
                    
                case "requirestool":
                case "needstool":
                case "usetool":
                case "tool_required":
                    requiresTool = property.Value.GetBoolean();
                    break;
                    
                case "tool":
                case "toolname":
                case "tool_name":
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        tool = property.Value.GetString();
                    }
                    break;
                    
                case "parameters":
                case "params":
                case "args":
                case "arguments":
                    if (property.Value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var param in property.Value.EnumerateObject())
                        {
                            parameters[param.Name] = param.Value.ToString();
                        }
                    }
                    break;
                    
                case "confidence":
                case "certainty":
                case "probability":
                    if (property.Value.ValueKind == JsonValueKind.Number)
                    {
                        confidence = property.Value.GetDouble();
                    }
                    break;
                    
                case "assumptions":
                case "assumption":
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        assumptions.Clear();
                        foreach (var item in property.Value.EnumerateArray())
                        {
                            assumptions.Add(item.GetString() ?? "");
                        }
                    }
                    break;
                    
                case "risks":
                case "risk":
                case "concerns":
                case "issues":
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        risks.Clear();
                        foreach (var item in property.Value.EnumerateArray())
                        {
                            risks.Add(item.GetString() ?? "");
                        }
                    }
                    break;
                    
                case "response":
                case "answer":
                case "result":
                case "message":
                case "output":
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        response = property.Value.GetString() ?? "Processing...";
                    }
                    else if (property.Value.ValueKind == JsonValueKind.Object)
                    {
                        // If response is an object, extract a meaningful string from it
                        if (property.Value.TryGetProperty("Message", out var messageElement) && messageElement.ValueKind == JsonValueKind.String)
                        {
                            response = messageElement.GetString() ?? "Processing...";
                        }
                        else if (property.Value.TryGetProperty("message", out var messageLowerElement) && messageLowerElement.ValueKind == JsonValueKind.String)
                        {
                            response = messageLowerElement.GetString() ?? "Processing...";
                        }
                        else
                        {
                            response = property.Value.ToString();
                        }
                    }
                    else
                    {
                        response = property.Value.ToString();
                    }
                    break;
                    
                case "userquery":
                case "query":
                case "request":
                    // LLM sometimes includes the user query - we can ignore this
                    break;
                    
                default:
                    // Log unknown fields but don't fail
                    _logger?.LogDebug("Unknown field in LLM response: {Field}", property.Name);
                    break;
            }
        }
        
        // Update the values in normalizedData
        normalizedData["taskCompleted"] = taskCompleted;
        normalizedData["response"] = response;
        
        // Update nextStep with extracted values
        var existingNextStep = (Dictionary<string, object>)normalizedData["nextStep"];
        existingNextStep["reasoning"] = reasoning;
        existingNextStep["requiresTool"] = requiresTool;
        existingNextStep["tool"] = tool ?? "";
        existingNextStep["parameters"] = parameters;
        existingNextStep["confidence"] = confidence;
        existingNextStep["assumptions"] = assumptions;
        existingNextStep["risks"] = risks;
        
        // Auto-detect tool usage from reasoning if not explicitly set
        if (!requiresTool && !string.IsNullOrEmpty(reasoning))
        {
            var reasoningLower = reasoning.ToLowerInvariant();
            if (reasoningLower.Contains("use ") && reasoningLower.Contains("tool"))
            {
                normalizedData["requiresTool"] = true;
                
                // Try to extract tool name from reasoning
                if (string.IsNullOrEmpty(tool))
                {
                    if (reasoningLower.Contains("gitdownloader") || reasoningLower.Contains("github"))
                        existingNextStep["tool"] = "GitHubDownloader";
                    else if (reasoningLower.Contains("filesystemanalyzer") || reasoningLower.Contains("filesystem"))
                        existingNextStep["tool"] = "FileSystemAnalyzer";
                    else if (reasoningLower.Contains("codeanalyzer") || reasoningLower.Contains("code"))
                        existingNextStep["tool"] = "CodeAnalyzer";
                    else if (reasoningLower.Contains("mathevaluator") || reasoningLower.Contains("math"))
                        existingNextStep["tool"] = "MathEvaluator";
                    else if (reasoningLower.Contains("externalcommand") || reasoningLower.Contains("command"))
                        existingNextStep["tool"] = "ExternalCommandExecutor";
                }
            }
        }
        
        return normalizedData;
    }
    
    private JsonElement? TryParseWithFallbacks(string response, string? sessionId)
    {
        _logger?.LogDebug("Attempting to parse response with fallback strategies for session {SessionId}", sessionId);
        
        // Strategy 1: Standard JSON extraction and parsing
        try
        {
            var cleanJson = ExtractJsonFromResponse(response);
            if (!string.IsNullOrEmpty(cleanJson))
            {
                var normalizedJson = NormalizeJsonForParsing(cleanJson);
                var responseObject = JsonSerializer.Deserialize<JsonElement>(normalizedJson);
                _logger?.LogDebug("Strategy 1 (Standard JSON) succeeded for session {SessionId}", sessionId);
                return responseObject;
            }
        }
        catch (JsonException ex)
        {
            _logger?.LogWarning("Strategy 1 (Standard JSON) failed for session {SessionId}: {Error}", sessionId, ex.Message);
        }
        
        // Strategy 2: YAML-like format parsing
        try
        {
            var yamlParsed = TryParseYamlLikeFormat(response);
            if (yamlParsed != null)
            {
                _logger?.LogDebug("Strategy 2 (YAML-like) succeeded for session {SessionId}", sessionId);
                return yamlParsed;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Strategy 2 (YAML-like) failed for session {SessionId}: {Error}", sessionId, ex.Message);
        }
        
        // Strategy 3: Key-value pair parsing
        try
        {
            var kvParsed = TryParseKeyValueFormat(response);
            if (kvParsed != null)
            {
                _logger?.LogDebug("Strategy 3 (Key-Value) succeeded for session {SessionId}", sessionId);
                return kvParsed;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Strategy 3 (Key-Value) failed for session {SessionId}: {Error}", sessionId, ex.Message);
        }
        
        // Strategy 4: Markdown-style parsing
        try
        {
            var markdownParsed = TryParseMarkdownFormat(response);
            if (markdownParsed != null)
            {
                _logger?.LogDebug("Strategy 4 (Markdown) succeeded for session {SessionId}", sessionId);
                return markdownParsed;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Strategy 4 (Markdown) failed for session {SessionId}: {Error}", sessionId, ex.Message);
        }
        
        // Strategy 5: Plain text interpretation
        try
        {
            var textParsed = TryParseAsPlainText(response);
            if (textParsed != null)
            {
                _logger?.LogDebug("Strategy 5 (Plain Text) succeeded for session {SessionId}", sessionId);
                return textParsed;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning("Strategy 5 (Plain Text) failed for session {SessionId}: {Error}", sessionId, ex.Message);
        }
        
        _logger?.LogError("All parsing strategies failed for session {SessionId}", sessionId);
        return null;
    }
    
    private JsonElement? TryParseYamlLikeFormat(string response)
    {
        // Look for YAML-like format:
        // taskCompleted: true
        // response: |
        //   Here's your code:
        //   ```csharp
        //   // code here
        //   ```
        
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<string, object>();
        
        string? currentKey = null;
        var currentValue = new List<string>();
        bool inMultilineValue = false;
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (trimmedLine.Contains(':') && !inMultilineValue)
            {
                // Process previous key-value if exists
                if (currentKey != null)
                {
                    result[currentKey] = currentValue.Count == 1 ? currentValue[0] : string.Join("\n", currentValue);
                }
                
                var parts = trimmedLine.Split(':', 2);
                currentKey = parts[0].Trim();
                var value = parts.Length > 1 ? parts[1].Trim() : "";
                
                if (value == "|" || value == ">")
                {
                    inMultilineValue = true;
                    currentValue = new List<string>();
                }
                else
                {
                    currentValue = new List<string> { value };
                    inMultilineValue = false;
                }
            }
            else if (inMultilineValue)
            {
                currentValue.Add(line);
            }
        }
        
        // Process last key-value
        if (currentKey != null)
        {
            result[currentKey] = currentValue.Count == 1 ? currentValue[0] : string.Join("\n", currentValue);
        }
        
        if (result.Count > 0)
        {
            // Convert to JsonElement
            var json = JsonSerializer.Serialize(result);
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        
        return null;
    }
    
    private JsonElement? TryParseKeyValueFormat(string response)
    {
        // Look for simple key-value pairs:
        // Task Completed: true
        // Response: Here's your code...
        // Tool Required: false
        
        var result = new Dictionary<string, object>();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.Contains(':'))
            {
                var parts = trimmedLine.Split(':', 2);
                if (parts.Length == 2)
                {
                    var key = NormalizeKeyName(parts[0].Trim());
                    var value = parts[1].Trim();
                    
                    // Try to parse boolean values
                    if (bool.TryParse(value, out var boolValue))
                    {
                        result[key] = boolValue;
                    }
                    else
                    {
                        result[key] = value;
                    }
                }
            }
        }
        
        if (result.Count > 0)
        {
            // Ensure required fields exist
            if (!result.ContainsKey("taskCompleted"))
                result["taskCompleted"] = false;
            if (!result.ContainsKey("response"))
                result["response"] = response;
            
            var json = JsonSerializer.Serialize(result);
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        
        return null;
    }
    
    private JsonElement? TryParseMarkdownFormat(string response)
    {
        // Look for markdown-style headers and content:
        // ## Task Status: Complete
        // ## Response
        // Here's your code...
        
        var result = new Dictionary<string, object>();
        var lines = response.Split('\n');
        
        string? currentSection = null;
        var currentContent = new List<string>();
        
        foreach (var line in lines)
        {
            if (line.StartsWith("##") || line.StartsWith("#"))
            {
                // Process previous section
                if (currentSection != null && currentContent.Count > 0)
                {
                    var content = string.Join("\n", currentContent).Trim();
                    result[currentSection] = content;
                }
                
                // Start new section
                currentSection = NormalizeKeyName(line.TrimStart('#').Trim());
                currentContent = new List<string>();
            }
            else if (currentSection != null)
            {
                currentContent.Add(line);
            }
        }
        
        // Process last section
        if (currentSection != null && currentContent.Count > 0)
        {
            var content = string.Join("\n", currentContent).Trim();
            result[currentSection] = content;
        }
        
        if (result.Count > 0)
        {
            // Ensure required fields exist
            if (!result.ContainsKey("taskCompleted"))
                result["taskCompleted"] = DetectTaskCompletion(response);
            if (!result.ContainsKey("response"))
                result["response"] = response;
            
            var json = JsonSerializer.Serialize(result);
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
        
        return null;
    }
    
    private JsonElement? TryParseAsPlainText(string response)
    {
        // Last resort: treat as plain text response
        var result = new Dictionary<string, object>
        {
            ["taskCompleted"] = DetectTaskCompletion(response),
            ["response"] = response,
            ["nextStep"] = null!
        };
        
        var json = JsonSerializer.Serialize(result);
        return JsonSerializer.Deserialize<JsonElement>(json);
    }
    
    private string NormalizeKeyName(string key)
    {
        // Convert various key formats to standard JSON property names
        var normalized = key.ToLowerInvariant()
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "");
        
        return normalized switch
        {
            "taskcompleted" or "taskstatus" or "completed" or "done" => "taskCompleted",
            "nextstep" or "next" or "step" => "nextStep",
            "response" or "answer" or "result" => "response",
            "tool" or "toolname" => "tool",
            "parameters" or "params" or "arguments" => "parameters",
            "requirestool" or "toolrequired" or "usetool" => "requiresTool",
            _ => normalized
        };
    }
    
    private bool DetectTaskCompletion(string response)
    {
        // Heuristics to detect if task is completed based on response content
        var lowercaseResponse = response.ToLowerInvariant();
        
        var completionIndicators = new[]
        {
            "task completed", "task complete", "done", "finished", "complete",
            "here's your", "here is your", "created successfully", "generated successfully"
        };
        
        var incompletionIndicators = new[]
        {
            "need to", "requires", "next step", "continue", "more information needed",
            "please provide", "clarification needed"
        };
        
        // Check for explicit completion indicators
        if (completionIndicators.Any(indicator => lowercaseResponse.Contains(indicator)))
        {
            return true;
        }
        
        // Check for incompletion indicators
        if (incompletionIndicators.Any(indicator => lowercaseResponse.Contains(indicator)))
        {
            return false;
        }
        
        // If response contains code or detailed solution, likely completed
        if (lowercaseResponse.Contains("```") || lowercaseResponse.Contains("namespace") || 
            lowercaseResponse.Contains("class") || lowercaseResponse.Contains("function"))
        {
            return true;
        }
        
        // Default to incomplete for safety
        return false;
    }
    
    private string ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return "";
        
        // Find the first { and look for the matching }
        var firstBrace = response.IndexOf('{');
        if (firstBrace < 0)
            return "";
        
        int braceCount = 0;
        bool inString = false;
        bool escaped = false;
        int jsonEnd = -1;
        
        for (int i = firstBrace; i < response.Length; i++)
        {
            char c = response[i];
            
            if (escaped)
            {
                escaped = false;
                continue;
            }
            
            if (c == '\\')
            {
                escaped = true;
                continue;
            }
            
            if (c == '"')
            {
                inString = !inString;
                continue;
            }
            
            if (!inString)
            {
                if (c == '{')
                    braceCount++;
                else if (c == '}')
                {
                    braceCount--;
                    if (braceCount == 0)
                    {
                        jsonEnd = i;
                        break;
                    }
                }
            }
        }
        
        // If we found a complete JSON object, extract it
        if (jsonEnd > firstBrace)
        {
            var rawJson = response.Substring(firstBrace, jsonEnd - firstBrace + 1);
            // Remove C++-style comments that break JSON parsing
            return RemoveJsonComments(rawJson);
        }
        
        return "";
    }
    
    private string RemoveJsonComments(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;
        
        var lines = json.Split('\n');
        var cleanedLines = new List<string>();
        
        foreach (var line in lines)
        {
            var cleanLine = line;
            var commentIndex = line.IndexOf("//");
            
            // Only remove comments that are not inside string literals
            if (commentIndex >= 0)
            {
                // Count quotes before the comment to ensure we're not inside a string
                var quotesBeforeComment = 0;
                var escaped = false;
                
                for (int i = 0; i < commentIndex; i++)
                {
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }
                    
                    if (line[i] == '\\')
                    {
                        escaped = true;
                        continue;
                    }
                    
                    if (line[i] == '"')
                    {
                        quotesBeforeComment++;
                    }
                }
                
                // If even number of quotes, we're not inside a string, so remove the comment
                if (quotesBeforeComment % 2 == 0)
                {
                    cleanLine = line.Substring(0, commentIndex).TrimEnd();
                }
            }
            
            cleanedLines.Add(cleanLine);
        }
        
        return string.Join('\n', cleanedLines);
    }
    
    private string NormalizeJsonForParsing(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;
        
        // Fix common JSON formatting issues that break parsing
        // This handles unescaped newlines and carriage returns in string values
        
        var result = new StringBuilder();
        bool inString = false;
        bool escaped = false;
        
        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];
            
            if (escaped)
            {
                // Keep escaped characters as-is
                result.Append(c);
                escaped = false;
                continue;
            }
            
            if (c == '\\')
            {
                escaped = true;
                result.Append(c);
                continue;
            }
            
            if (c == '"')
            {
                inString = !inString;
                result.Append(c);
                continue;
            }
            
            if (inString)
            {
                // Inside a string - escape problematic characters
                switch (c)
                {
                    case '\r':
                        result.Append("\\r");
                        break;
                    case '\n':
                        result.Append("\\n");
                        break;
                    case '\t':
                        result.Append("\\t");
                        break;
                    default:
                        result.Append(c);
                        break;
                }
            }
            else
            {
                // Outside string - keep as-is
                result.Append(c);
            }
        }
        
        return result.ToString();
    }
}
