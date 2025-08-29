using Ollama.Domain.Strategies;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.IO;
using Ollama.Domain.Tools;

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
        try
        {
            // Try to load from the prompts directory relative to the application
            var promptPath = Path.Combine("prompts", "pessimistic-initial-system-prompt.txt");
            
            if (File.Exists(promptPath))
            {
                return File.ReadAllText(promptPath);
            }
            
            // Fallback: try relative to the solution root
            var fallbackPath = Path.Combine("..", "..", "..", "..", "prompts", "pessimistic-initial-system-prompt.txt");
            if (File.Exists(fallbackPath))
            {
                return File.ReadAllText(fallbackPath);
            }
            
            _logger?.LogWarning("Could not find prompt file at {Path}, using embedded fallback", promptPath);
            
            // Fallback to embedded prompt if file not found
            return GetEmbeddedPrompt();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading initial prompt from file, using embedded fallback");
            return GetEmbeddedPrompt();
        }
    }

    private string GetEmbeddedPrompt()
    {
        return @"You are a helpful AI agent with a systematic approach to problem-solving and retry capabilities.

CRITICAL OUTPUT REQUIREMENT - READ THIS CAREFULLY:
==================================================
YOU MUST RESPOND WITH ONLY A SINGLE JSON OBJECT. NOTHING ELSE.
- NO explanatory text before the JSON
- NO explanatory text after the JSON  
- NO code blocks or markdown formatting
- NO additional commentary
- ONLY raw JSON text that can be directly parsed

Your response must be EXACTLY this JSON structure and NOTHING MORE:
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

RESPONSE FORMAT ENFORCEMENT:
- Start your response with {
- End your response with }
- Include no other characters outside the JSON object
- Do not wrap in ```json``` code blocks
- Do not add any explanations outside the JSON

KEY PRINCIPLES:
1. Always respond with valid JSON only - no other text whatsoever
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

ABSOLUTELY CRITICAL - JSON OUTPUT ONLY:
=====================================
YOUR RESPONSE MUST BE EXACTLY ONE JSON OBJECT AND NOTHING ELSE.
- Do NOT include any text before the opening {{
- Do NOT include any text after the closing }}
- Do NOT use markdown code blocks like ```json```
- Do NOT add explanations or comments outside the JSON
- Do NOT include multiple JSON objects
- The FIRST character of your response must be {{
- The LAST character of your response must be }}

CRITICAL REQUIREMENTS:
1. Respond only in pure JSON format - no other text or formatting
2. Analyze the user's request carefully and choose the most appropriate approach
3. Your 'nextStep' must be a SPECIFIC EXECUTABLE ACTION based on the actual user request
4. Choose tools based on what the user is actually asking for, not predetermined assumptions
5. Consider what could go wrong at each step
6. Use tools only when necessary to answer the user's question
7. Break complex tasks into logical, manageable steps

TOOL SELECTION GUIDANCE:
========================
Analyze the user's request and select the appropriate tool:
- For mathematical problems → Use MathEvaluator
- For GitHub repository tasks → Use GitHubDownloader  
- For file/directory analysis → Use FileSystemAnalyzer
- For reading code files → Use CodeAnalyzer
- For system commands → Use ExternalCommandExecutor
- For simple questions that don't require tools → Provide direct answers

Make your nextStep immediately actionable and relevant to the user's actual request!

FINAL REMINDER: Your response starts with {{ and ends with }}. Nothing else.";
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
            _logger?.LogDebug("Validating LLM response for session {SessionId}. Response length: {Length} chars", 
                sessionId, response?.Length ?? 0);
            _logger?.LogDebug("Raw LLM response content: {Response}", response);
            
            if (string.IsNullOrWhiteSpace(response))
            {
                _logger?.LogError("Empty or null response received for session {SessionId}", sessionId);
                return CreateErrorResponse("Empty response from LLM");
            }
            
            // Extract JSON from potentially malformed response
            var cleanJson = ExtractJsonFromResponse(response);
            if (string.IsNullOrEmpty(cleanJson))
            {
                _logger?.LogError("Could not extract valid JSON from response for session {SessionId}", sessionId);
                return CreateErrorResponse("Response does not contain valid JSON");
            }
            
            // Parse the extracted JSON
            var responseObject = JsonSerializer.Deserialize<JsonElement>(cleanJson);
            
            _logger?.LogDebug("JSON parsing successful for session {SessionId}", sessionId);
            
            // Check required fields for new structure
            var requiredFields = new[] { "taskCompleted", "nextStep", "response" };
            var missingFields = new List<string>();
            
            foreach (var field in requiredFields)
            {
                if (!responseObject.TryGetProperty(field, out _))
                {
                    missingFields.Add(field);
                }
            }
            
            // If we're missing critical fields, try to normalize the response
            if (missingFields.Count > 0)
            {
                _logger?.LogWarning("Response missing some expected fields: {Fields}. Attempting to normalize...", 
                    string.Join(", ", missingFields));
                
                var normalizedResponse = NormalizeResponseFormat(responseObject, sessionId ?? "unknown");
                return JsonSerializer.Serialize(normalizedResponse, new JsonSerializerOptions { WriteIndented = true });
            }
            
            // Extract key information for validation
            var taskCompleted = responseObject.GetProperty("taskCompleted").GetBoolean();
            var nextStep = responseObject.TryGetProperty("nextStep", out var nextStepElement) ? nextStepElement : (JsonElement?)null;
            
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
            var legacyTaskComplete = responseObject.TryGetProperty("taskComplete", out var taskCompleteElement) 
                ? taskCompleteElement.GetBoolean() 
                : taskCompleted;
            var legacyConfidence = responseObject.TryGetProperty("confidence", out var legacyConfElement) 
                ? legacyConfElement.GetDouble() 
                : 0.5;
            
            // Pessimistic validation rules
            if (legacyTaskComplete && legacyConfidence < 0.8)
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
            if (string.IsNullOrEmpty(reasoning) && responseObject.TryGetProperty("reasoning", out var legacyReasoningElement))
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
            ["tool"] = null,
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
                                    nextStepDict["tool"] = nextStepProp.Value.GetString();
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
            return response.Substring(firstBrace, jsonEnd - firstBrace + 1);
        }
        
        return "";
    }
}
