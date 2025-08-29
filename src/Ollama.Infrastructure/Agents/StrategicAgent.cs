using Ollama.Domain.Agents;
using Ollama.Domain.Strategies;
using Ollama.Domain.Services;
using Ollama.Domain.Tools;
using Ollama.Domain.Models.Communication;
using Ollama.Infrastructure.Strategies;
using Ollama.Infrastructure.Clients;
using Ollama.Infrastructure.Tools;
using Ollama.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text;

namespace Ollama.Infrastructure.Agents;

public class ConversationEntry
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? ToolName { get; set; }
    public Dictionary<string, string>? ToolParameters { get; set; }
    public string? RawResponse { get; set; }
    public string? ValidatedResponse { get; set; }
    public bool IsToolResponse { get; set; }
    public int MessageIndex { get; set; }
}

/// <summary>
/// Strategic agent that operates based on a configurable strategy
/// Integrates with session file system for isolated execution environments
/// </summary>
public class StrategicAgent : IAgent
{
    private readonly IAgentStrategy _strategy;
    private readonly ISessionFileSystem _sessionFileSystem;
    private readonly SessionLogger _sessionLogger;
    private readonly IToolRepository _toolRepository;
    private readonly BuiltInOllamaClient _ollamaClient;
    private readonly ILLMCommunicationService _communicationService;
    private readonly ILogger<StrategicAgent> _logger;
    private readonly string _model;
    private readonly ConcurrentDictionary<string, List<ConversationEntry>> _conversations = new();
    private readonly Dictionary<string, string> _availableCommands = new();
    private readonly ExternalCommandDetector _commandDetector;

    public StrategicAgent(
        IAgentStrategy strategy,
        ISessionFileSystem sessionFileSystem,
        SessionLogger sessionLogger,
        IToolRepository toolRepository,
        BuiltInOllamaClient ollamaClient,
        ILLMCommunicationService communicationService,
        ILogger<StrategicAgent> logger,
        string model = "llama3.1:8b")
    {
        _strategy = strategy;
        _sessionFileSystem = sessionFileSystem;
        _sessionLogger = sessionLogger;
        _toolRepository = toolRepository;
        _ollamaClient = ollamaClient;
        _communicationService = communicationService;
        _logger = logger;
        _model = model;
        
        // Initialize external command detector with a simple logger
        _commandDetector = new ExternalCommandDetector();
        
        // Detect available commands on startup (fire and forget - don't block)
        Task.Run(async () =>
        {
            try
            {
                var commands = await _commandDetector.DetectAvailableCommandsAsync();
                lock (_availableCommands)
                {
                    _availableCommands.Clear();
                    foreach (var cmd in commands)
                    {
                        _availableCommands[cmd.Key] = cmd.Value;
                    }
                }
                _logger.LogInformation("External command detection completed. Found {Count} commands", commands.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to detect external commands");
            }
        });
    }

    public string Answer(string prompt, string? sessionId = null)
    {
        return AnswerAsync(prompt, sessionId).GetAwaiter().GetResult();
    }

    public async Task<string> AnswerAsync(string prompt, string? sessionId = null)
    {
        try
        {
            // Ensure we have a session ID
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                _logger.LogDebug("Created new session ID for Answer: {SessionId}", sessionId);
            }

            // Initialize session file system
            var sessionRoot = _sessionFileSystem.GetSessionRoot(sessionId);
            var currentDir = _sessionFileSystem.GetCurrentDirectory(sessionId);
            
            _logger.LogInformation("Session {SessionId}: Starting with strategy {Strategy} in directory {Directory}", 
                sessionId, _strategy.Name, currentDir);

            // Initialize conversation if needed
            if (!_conversations.ContainsKey(sessionId))
            {
                string initialPrompt;
                if (_strategy is Ollama.Infrastructure.Strategies.PessimisticAgentStrategy pessimistic)
                {
                    initialPrompt = await pessimistic.GetInitialPromptWithDynamicToolsAsync(_toolRepository);
                }
                else
                {
                    initialPrompt = _strategy.GetInitialPrompt();
                }
                _conversations[sessionId] = new List<ConversationEntry>
                {
                    new ConversationEntry 
                    { 
                        Role = "system", 
                        Content = initialPrompt, 
                        Timestamp = DateTime.UtcNow, 
                        MessageIndex = 0 
                    }
                };
                
                // Log the initial system prompt for debugging and transparency
                _sessionLogger.LogSessionInfo(sessionId, "initial_system_prompt", 
                    $"Initial System Prompt from {_strategy.Name} strategy:\n{initialPrompt}");
                
                _logger.LogDebug("Session {SessionId}: Initialized conversation with strategy prompt", sessionId);
                _logger.LogDebug("Session {SessionId}: Initial system prompt length: {Length}", sessionId, initialPrompt.Length);
            }

            // Create a session context file to track this interaction
            _sessionFileSystem.WriteFile(sessionId, "session_context.json", CreateSessionContext(sessionId, prompt));

            // Format user query according to strategy
            var formattedPrompt = _strategy.FormatQueryPrompt(prompt, sessionId);
            AddConversationEntry(sessionId, "user", formattedPrompt);

            // Log the interaction
            _sessionLogger.LogInteraction(sessionId, "query", 
                $"User Query: {prompt}\nFormatted Prompt: {formattedPrompt}");

            // Call actual LLM using BuiltInOllamaClient
            var llmResponse = await CallLLMAsync(formattedPrompt, sessionId);
            
            _logger.LogDebug("Session {SessionId}: Raw LLM response length: {Length} chars", sessionId, llmResponse?.Length ?? 0);
            _logger.LogDebug("Session {SessionId}: Raw LLM response content: {Response}", sessionId, llmResponse);

            // Validate response according to strategy
            var validatedResponse = _strategy.ValidateResponse(llmResponse ?? "", sessionId);
            
            // Add assistant response to conversation with both raw and validated content
            AddConversationEntry(sessionId, "assistant", validatedResponse ?? "", 
                rawResponse: llmResponse, validatedResponse: validatedResponse);
            
            _logger.LogDebug("Session {SessionId}: Validated response length: {Length} chars", sessionId, validatedResponse?.Length ?? 0);
            _logger.LogDebug("Session {SessionId}: Validated response content: {Response}", sessionId, validatedResponse);
            
            // Log the response
            _sessionLogger.LogInteraction(sessionId, "response", 
                $"Raw LLM Response: {llmResponse}\nValidated Response: {validatedResponse}");

            // Iterative execution loop - "joggle back and forth" until task is complete
            var maxIterations = 10; // Prevent infinite loops
            var iteration = 0;
            var isComplete = false;
            
            while (!isComplete && iteration < maxIterations)
            {
                iteration++;
                _logger.LogInformation("Session {SessionId}: Starting iteration {Iteration}", sessionId, iteration);
                
                // Check if we need to use any tools
                var toolRequest = _strategy.ExtractToolRequest(validatedResponse ?? "");
                if (toolRequest.HasValue)
                {
                    var (toolName, parameters) = toolRequest.Value;
                    _logger.LogInformation("Session {SessionId}: Tool requested: {Tool} with parameters: {Parameters}", 
                        sessionId, toolName, string.Join(", ", parameters.Select(p => $"{p.Key}={p.Value}")));

                    // Execute the tool within the session context
                    var toolResponse = ExecuteTool(toolName, parameters, sessionId);
                    
                    // Log tool execution with extended details
                    _sessionLogger.LogToolExecution(sessionId, toolName, parameters, toolResponse, iteration, 
                        context: $"Executing tool during iteration {iteration} of conversation flow");
                    
                    // Format tool response for LLM
                    var formattedToolResponse = _strategy.FormatToolResponse(toolName, toolResponse);
                    AddConversationEntry(sessionId, "user", formattedToolResponse, 
                        toolName: toolName, toolParameters: parameters, isToolResponse: true);
                    
                    // Continue conversation - get next response after tool execution
                    var continuationResponse = ContinueConversation(sessionId, toolResponse);
                    validatedResponse = continuationResponse;
                }

                // Check if task is complete after this iteration
                isComplete = _strategy.IsTaskComplete(validatedResponse ?? "");
                _logger.LogInformation("Session {SessionId}: Iteration {Iteration} - Task complete: {IsComplete}", 
                    sessionId, iteration, isComplete);

                if (!isComplete)
                {
                    var nextStep = _strategy.GetNextStep(validatedResponse ?? "");
                    _logger.LogInformation("Session {SessionId}: Iteration {Iteration} - Next step: {NextStep}", 
                        sessionId, iteration, nextStep);
                    
                    _sessionFileSystem.WriteFile(sessionId, "next_steps.txt", 
                        $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} [Iteration {iteration}]: {nextStep}\n");
                        
                    // If we haven't used a tool in this iteration but task is not complete,
                    // we need to get another LLM response to continue the conversation
                    if (!toolRequest.HasValue)
                    {
                        var continuePrompt = "Please continue working on the task. " + nextStep;
                        var nextLlmResponse = await CallLLMAsync(continuePrompt, sessionId);
                        validatedResponse = _strategy.ValidateResponse(nextLlmResponse ?? "", sessionId);
                        AddConversationEntry(sessionId, "assistant", validatedResponse ?? "", 
                            rawResponse: nextLlmResponse, validatedResponse: validatedResponse);
                    }
                }
                
                // Save state after each iteration
                SaveConversationState(sessionId);
            }
            
            if (iteration >= maxIterations)
            {
                _logger.LogWarning("Session {SessionId}: Reached maximum iterations ({MaxIterations}) without task completion", 
                    sessionId, maxIterations);
                _sessionFileSystem.WriteFile(sessionId, "max_iterations_reached.txt", 
                    $"Reached maximum iterations ({maxIterations}) at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\nLast response: {validatedResponse}\n");
            }

            // Generate final session summary
            GenerateSessionSummary(sessionId);

            return validatedResponse ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session {SessionId}: Error in Answer method", sessionId);
            _logger.LogError("Exception details for session {SessionId}: {ExceptionType} - {Message}", 
                sessionId, ex.GetType().Name, ex.Message);
            
            // Save conversation state even on error to preserve what we have
            if (!string.IsNullOrEmpty(sessionId))
            {
                SaveConversationState(sessionId);
            }
            
            return _strategy.HandleError(ex.Message, "Answer");
        }
    }

    public string Think(string prompt)
    {
        return Think(prompt, null);
    }

    public string Think(string prompt, string? sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
            }

            _logger.LogInformation("Session {SessionId}: Thinking about: {Prompt}", sessionId, prompt);
            
            // Create thinking session directory
            _sessionFileSystem.CreateDirectory(sessionId, "thinking");
            
            // Format thinking prompt
            var thinkingPrompt = $"Think step by step about this problem using the {_strategy.Name} approach: {prompt}";
            
            // Log thinking process
            _sessionLogger.LogThinking(sessionId, $"Thinking Prompt: {thinkingPrompt}");
            
            // TODO: Process thinking without storing in main conversation history
            var thinkingResponse = SimulateThinking(thinkingPrompt, sessionId);
            
            _sessionLogger.LogThinking(sessionId, $"Thinking Prompt: {thinkingPrompt}", 
                result: $"Thinking Result: {thinkingResponse}");
            
            return thinkingResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session {SessionId}: Error in Think method", sessionId);
            return _strategy.HandleError(ex.Message, "Think");
        }
    }

    public object Plan(string prompt)
    {
        return Plan(prompt, null);
    }

    public object Plan(string prompt, string? sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
            }

            _logger.LogInformation("Session {SessionId}: Planning for: {Prompt}", sessionId, prompt);
            
            // Create planning session directory
            _sessionFileSystem.CreateDirectory(sessionId, "plans");
            
            // Format planning prompt
            var planningPrompt = $"Create a detailed step-by-step plan using the {_strategy.Name} approach for: {prompt}";
            
            // TODO: Process planning
            var planningResponse = SimulatePlanning(planningPrompt, sessionId);
            
            // Save plan using consolidated logging
            _sessionLogger.LogPlan(sessionId, planningResponse, "detailed_step_plan");
            
            return new
            {
                SessionId = sessionId,
                Strategy = _strategy.Name,
                Plan = planningResponse,
                PlanFile = "plans/plans_log.txt",
                CreatedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session {SessionId}: Error in Plan method", sessionId);
            return new
            {
                Error = _strategy.HandleError(ex.Message, "Plan"),
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    public object Act(string instruction)
    {
        try
        {
            var sessionId = Guid.NewGuid().ToString();
            
            _logger.LogInformation("Session {SessionId}: Acting on: {Instruction}", sessionId, instruction);
            
            // Create action session directory
            _sessionFileSystem.CreateDirectory(sessionId, "actions");
            
            // Format action prompt
            var actionPrompt = $"Execute this instruction using the {_strategy.Name} approach: {instruction}";
            
            // TODO: Process action
            var actionResponse = SimulateAction(actionPrompt, sessionId);
            
            // Save action result using consolidated logging
            _sessionLogger.LogAction(sessionId, actionResponse, "instruction_execution");
            
            return new
            {
                SessionId = sessionId,
                Strategy = _strategy.Name,
                Result = actionResponse,
                ActionFile = "actions/actions_log.txt",
                ExecutedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Act method: {Error}", ex.Message);
            return new
            {
                Error = _strategy.HandleError(ex.Message, "Act"),
                ExecutedAt = DateTime.UtcNow
            };
        }
    }

    // Private helper methods

    private string CreateSessionContext(string sessionId, string prompt)
    {
        var context = new
        {
            SessionId = sessionId,
            Strategy = _strategy.Name,
            InitialPrompt = prompt,
            SessionRoot = _sessionFileSystem.GetSessionRoot(sessionId),
            CurrentDirectory = _sessionFileSystem.GetCurrentDirectory(sessionId),
            CreatedAt = DateTime.UtcNow
        };
        
        return System.Text.Json.JsonSerializer.Serialize(context, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    private void AddConversationEntry(string sessionId, string role, string content, 
        string? toolName = null, Dictionary<string, string>? toolParameters = null, 
        string? rawResponse = null, string? validatedResponse = null, bool isToolResponse = false)
    {
        var entry = new ConversationEntry
        {
            Role = role,
            Content = content,
            Timestamp = DateTime.UtcNow,
            ToolName = toolName,
            ToolParameters = toolParameters,
            RawResponse = rawResponse,
            ValidatedResponse = validatedResponse,
            IsToolResponse = isToolResponse,
            MessageIndex = _conversations[sessionId].Count
        };
        
        _conversations[sessionId].Add(entry);
        
        // Save conversation after each message for comprehensive logging
        SaveConversationState(sessionId);
        
        _logger.LogDebug("Session {SessionId}: Added conversation entry #{Index} - {Role}: {ContentLength} chars", 
            sessionId, entry.MessageIndex, role, content.Length);
    }

    private void SaveConversationState(string sessionId)
    {
        try
        {
            if (_conversations.TryGetValue(sessionId, out var conversation))
            {
                // Create comprehensive conversation metadata
                var conversationData = new
                {
                    SessionId = sessionId,
                    Strategy = _strategy.Name,
                    Model = _model,
                    StartTime = conversation.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
                    LastUpdate = DateTime.UtcNow,
                    MessageCount = conversation.Count,
                    UserMessages = conversation.Count(c => c.Role == "user"),
                    AssistantMessages = conversation.Count(c => c.Role == "assistant"),
                    ToolMessages = conversation.Count(c => c.IsToolResponse),
                    ToolsUsed = conversation.Where(c => !string.IsNullOrEmpty(c.ToolName))
                                           .Select(c => c.ToolName)
                                           .Distinct()
                                           .ToList(),
                    TotalContentLength = conversation.Sum(c => c.Content?.Length ?? 0),
                    Messages = conversation
                };

                var conversationJson = System.Text.Json.JsonSerializer.Serialize(conversationData, 
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                
                _sessionFileSystem.WriteFile(sessionId, "conversation_history.json", conversationJson);
                
                _logger.LogDebug("Session {SessionId}: Saved conversation state - {MessageCount} messages, {ContentLength} chars total", 
                    sessionId, conversation.Count, conversationData.TotalContentLength);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session {SessionId}: Failed to save conversation state", sessionId);
        }
    }

    private void GenerateSessionSummary(string sessionId)
    {
        try
        {
            if (_conversations.TryGetValue(sessionId, out var conversation))
            {
                var summary = new
                {
                    SessionId = sessionId,
                    CompletedAt = DateTime.UtcNow,
                    Strategy = _strategy.Name,
                    Model = _model,
                    SessionDuration = conversation.LastOrDefault()?.Timestamp.Subtract(conversation.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow).TotalMinutes ?? 0,
                    Statistics = new
                    {
                        TotalMessages = conversation.Count,
                        UserMessages = conversation.Count(c => c.Role == "user"),
                        AssistantMessages = conversation.Count(c => c.Role == "assistant"),
                        SystemMessages = conversation.Count(c => c.Role == "system"),
                        ToolExecutions = conversation.Count(c => c.IsToolResponse),
                        UniqueToolsUsed = conversation.Where(c => !string.IsNullOrEmpty(c.ToolName))
                                                   .Select(c => c.ToolName)
                                                   .Distinct()
                                                   .Count(),
                        TotalContentLength = conversation.Sum(c => c.Content?.Length ?? 0),
                        AverageMessageLength = conversation.Count > 0 ? conversation.Average(c => c.Content?.Length ?? 0) : 0
                    },
                    ToolsUsed = conversation.Where(c => !string.IsNullOrEmpty(c.ToolName))
                                           .GroupBy(c => c.ToolName)
                                           .Select(g => new { Tool = g.Key, UsageCount = g.Count() })
                                           .ToList(),
                    MessageTimeline = conversation.Select(c => new
                    {
                        Index = c.MessageIndex,
                        Timestamp = c.Timestamp,
                        Role = c.Role,
                        ContentLength = c.Content?.Length ?? 0,
                        ToolName = c.ToolName,
                        IsToolResponse = c.IsToolResponse
                    }).ToList()
                };

                var summaryJson = System.Text.Json.JsonSerializer.Serialize(summary, 
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                
                _sessionFileSystem.WriteFile(sessionId, "session_summary.json", summaryJson);
                
                _logger.LogInformation("Session {SessionId}: Generated comprehensive session summary - {TotalMessages} messages, {Duration:F2} minutes", 
                    sessionId, summary.Statistics.TotalMessages, summary.SessionDuration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session {SessionId}: Failed to generate session summary", sessionId);
        }
    }

    private string ExecuteTool(string toolName, Dictionary<string, string> parameters, string sessionId)
    {
        try
        {
            _logger.LogInformation("Session {SessionId}: Executing tool {Tool} with enhanced retry mechanism", 
                sessionId, toolName);
            
            // Handle MISSING_TOOL requests with reflection-based tool discovery
            if (toolName.Equals("MISSING_TOOL", StringComparison.OrdinalIgnoreCase))
            {
                return HandleMissingToolRequest(sessionId, parameters);
            }
            
            // Get the tool from the repository
            var tool = _toolRepository.GetToolByName(toolName);
            if (tool == null)
            {
                var errorMessage = $"Tool '{toolName}' not found in repository";
                _logger.LogError("Session {SessionId}: {Error}", sessionId, errorMessage);
                return errorMessage;
            }

            // Create execution context for the tool with enhanced retry parameters
            var context = new ToolContext
            {
                WorkingDirectory = _sessionFileSystem.GetCurrentDirectory(sessionId),
                SessionId = sessionId,
                RetryAttempt = 0
            };

            // Add parameters to context
            foreach (var param in parameters)
            {
                context.Parameters[param.Key] = param.Value;
            }

            // Execute the tool with enhanced retry mechanism
            var task = tool.RunWithRetryAsync(context, maxRetries: 10);
            task.Wait(); // TODO: Make this async properly
            var result = task.Result;
            
            // Log comprehensive tool execution results
            _sessionLogger.LogToolExecution(sessionId, toolName, parameters, 
                result.Success ? "Success" : result.ErrorMessage ?? "Unknown error", 
                result.TotalAttempts, 
                context: $"Tool executed using method: {result.MethodUsed}, Total attempts: {result.TotalAttempts}", 
                error: result.Success ? null : new Exception(result.ErrorMessage ?? "Tool execution failed"));
            
            if (result.Success)
            {
                _logger.LogInformation("Session {SessionId}: Tool {Tool} executed successfully using method {Method} after {Attempts} attempts", 
                    sessionId, toolName, result.MethodUsed, result.TotalAttempts);
                return result.Output?.ToString() ?? "Tool executed successfully";
            }
            else
            {
                var errorMessage = $"Tool {toolName} failed after {result.TotalAttempts} attempts using all available methods. Final error: {result.ErrorMessage}";
                _logger.LogError("Session {SessionId}: {Error}", sessionId, errorMessage);
                return errorMessage;
            }
        }
        catch (Exception ex)
        {
            var errorMessage = $"Error executing tool {toolName}: {ex.Message}";
            _logger.LogError(ex, "Session {SessionId}: {Error}", sessionId, errorMessage);
            
            // Log tool execution exception with extended details
            _sessionLogger.LogToolExecution(sessionId, toolName, parameters, 
                ex.Message, 0, 
                context: $"Tool execution threw exception during initialization", 
                error: ex);
            
            return errorMessage;
        }
    }

    private async Task<string> CallLLMAsync(string prompt, string sessionId)
    {
        try
        {
            // Get the conversation history for this session
            var conversation = _conversations[sessionId];
            
            // Log the full conversation context being sent to LLM
            var conversationLog = string.Join("\n---\n", conversation.Select(msg => $"{msg.Role.ToUpper()}:\n{msg.Content}"));
            _sessionLogger.LogConversationContext(sessionId, 
                $"Full Conversation Context sent to LLM:\n{conversationLog}");
            
            _logger.LogInformation("Session {SessionId}: Calling LLM with model {Model}", sessionId, _model);
            _logger.LogDebug("Session {SessionId}: Sending {MessageCount} messages to LLM", sessionId, conversation.Count);
            
            // Convert ConversationEntry to tuple format for Ollama client
            var ollamaConversation = conversation.Select(msg => (msg.Role, msg.Content)).ToList();
            
            // Call the Ollama API with the full conversation history
            var response = await _ollamaClient.ChatAsync(_model, ollamaConversation);
            
            _logger.LogInformation("Session {SessionId}: Received LLM response ({Length} chars)", 
                sessionId, response.Length);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session {SessionId}: Error calling LLM", sessionId);
            
            // Fallback to simulation in case of error
            _logger.LogWarning("Session {SessionId}: Falling back to simulation due to LLM error", sessionId);
            return SimulateLLMResponse(prompt, sessionId);
        }
    }

    private string SimulateLLMResponse(string prompt, string sessionId)
    {
        // TODO: Replace with actual LLM client integration
        // This is a simulation for demonstration purposes
        
        if (_strategy is PessimisticAgentStrategy)
        {
            // Detect if this is a tool completion message
            if (prompt.ToLowerInvariant().Contains("tool execution completed"))
            {
                return @"{
  ""reasoning"": ""The tool has been executed successfully. I can see from the results that the repository has been downloaded. Now I need to analyze its structure to find the biggest files."",
  ""taskComplete"": false,
  ""nextStep"": ""Use FileSystemAnalyzer tool to analyze the downloaded repository and find the biggest files"",
  ""requiresTool"": true,
  ""tool"": ""FileSystemAnalyzer"",
  ""parameters"": {
    ""path"": ""./downloaded_repo"",
    ""operation"": ""find_largest_files"",
    ""count"": ""10""
  },
  ""confidence"": 0.8,
  ""assumptions"": [""Repository was downloaded successfully"", ""Repository contains multiple files""],
  ""risks"": [""Download may have failed"", ""Repository might be empty""],
  ""response"": ""Great! I've successfully downloaded the repository. Now I'll analyze its structure to find the biggest files.""
}";
            }
            
            // Detect if this is a GitHub repository analysis request
            if (prompt.ToLowerInvariant().Contains("github.com") && prompt.ToLowerInvariant().Contains("download"))
            {
                var githubUrl = ExtractGitHubUrl(prompt);
                return @"{
  ""reasoning"": ""The user wants me to download and analyze a GitHub repository. I need to start by downloading the repository first before I can analyze it. This is a complex task that requires multiple steps."",
  ""taskComplete"": false,
  ""nextStep"": ""Use GitHubDownloader tool to download the repository from " + githubUrl + @""",
  ""requiresTool"": true,
  ""tool"": ""GitHubDownloader"",
  ""parameters"": {
    ""url"": """ + githubUrl + @""",
    ""destination"": ""./downloaded_repo""
  },
  ""confidence"": 0.8,
  ""assumptions"": [""GitHub repository is publicly accessible"", ""Repository contains analyzable code files""],
  ""risks"": [""Repository might be private"", ""Network connection issues"", ""Large repository size""],
  ""response"": ""I'll start by downloading the GitHub repository so I can analyze its structure and contents.""
}";
            }
            
            // Default response for other queries
            return @"{
  ""reasoning"": ""This appears to be a user query that needs careful analysis. I should break this down into smaller steps to ensure accuracy."",
  ""taskComplete"": false,
  ""nextStep"": ""Analyze the user's requirements and determine if additional information or tools are needed"",
  ""requiresTool"": false,
  ""tool"": null,
  ""parameters"": {},
  ""confidence"": 0.6,
  ""assumptions"": [""User expects a thorough response"", ""Question may have multiple parts""],
  ""risks"": [""Misunderstanding user intent"", ""Incomplete analysis""],
  ""response"": ""I'm analyzing your request carefully. Let me break this down systematically to ensure I provide an accurate and complete response.""
}";
        }
        
        return "I'm processing your request systematically to ensure accuracy.";
    }

    private string ExtractGitHubUrl(string prompt)
    {
        // Simple regex to extract GitHub URL from the prompt
        var match = System.Text.RegularExpressions.Regex.Match(prompt, @"https://github\.com/[^\s,]+");
        return match.Success ? match.Value : "https://github.com/user/repo";
    }

    private string ContinueConversation(string sessionId, string toolResponse)
    {
        try
        {
            // Create a message about what was accomplished
            var accomplishmentMessage = $"Tool execution completed. Result: {toolResponse}. What should I do next?";
            
            // Get the next response from actual LLM
            var nextResponse = CallLLMAsync(accomplishmentMessage, sessionId).GetAwaiter().GetResult();
            AddConversationEntry(sessionId, "assistant", nextResponse);
            
            // Log the continuation
            _sessionLogger.LogInteraction(sessionId, "continuation", 
                $"Tool Response: {toolResponse}\nNext LLM Response: {nextResponse}");
            
            return _strategy.ValidateResponse(nextResponse, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session {SessionId}: Error in conversation continuation", sessionId);
            return _strategy.HandleError(ex.Message, "ContinueConversation");
        }
    }

    private string SimulateThinking(string prompt, string sessionId)
    {
        // TODO: Replace with actual LLM client integration
        return $"Thinking about: {prompt} using {_strategy.Name} strategy in session {sessionId}";
    }

    private string SimulatePlanning(string prompt, string sessionId)
    {
        // TODO: Replace with actual LLM client integration
        var plan = new
        {
            Strategy = _strategy.Name,
            SessionId = sessionId,
            Steps = new[]
            {
                "1. Analyze requirements thoroughly",
                "2. Identify potential risks and assumptions",
                "3. Break down into smaller verifiable steps",
                "4. Execute each step with validation",
                "5. Verify final results before completion"
            },
            Prompt = prompt,
            CreatedAt = DateTime.UtcNow
        };
        
        return System.Text.Json.JsonSerializer.Serialize(plan, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    private string SimulateAction(string instruction, string sessionId)
    {
        // TODO: Replace with actual LLM client integration
        var action = new
        {
            Strategy = _strategy.Name,
            SessionId = sessionId,
            Instruction = instruction,
            Status = "Executed",
            Result = $"Action completed using {_strategy.Name} strategy",
            ExecutedAt = DateTime.UtcNow
        };
        
        return System.Text.Json.JsonSerializer.Serialize(action, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// New schema-based communication method
    /// </summary>
    public async Task<string> AnswerWithSchemaAsync(string prompt, string? sessionId = null)
    {
        try
        {
            // Ensure we have a session ID
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                _logger.LogDebug("Created new session ID for schema-based Answer: {SessionId}", sessionId);
            }

            // Initialize session file system
            var sessionRoot = _sessionFileSystem.GetSessionRoot(sessionId);
            var currentDir = _sessionFileSystem.GetCurrentDirectory(sessionId);
            
            _logger.LogInformation("Session {SessionId}: Starting schema-based interaction with strategy {Strategy} in directory {Directory}", 
                sessionId, _strategy.Name, currentDir);

            // Get previous interactions for this session
            var previousInteractions = GetPreviousInteractionsFromHistory(sessionId);

            // Create structured request schema
            var requestSchema = _communicationService.CreateRequestSchema(
                sessionId, 
                prompt, 
                _toolRepository, 
                _strategy.Name, 
                previousInteractions);

            // Serialize request for logging
            var requestJson = _communicationService.SerializeRequest(requestSchema);
            _sessionLogger.LogInteraction(sessionId, "request_schema", requestJson);

            // Call LLM with structured request
            var llmResponseJson = await CallLLMWithSchemaAsync(requestSchema, sessionId);
            
            // Parse response using communication service
            var responseSchema = _communicationService.ParseResponseSchema(llmResponseJson);
            
            // Validate response schema
            var (isValid, validationErrors) = _communicationService.ValidateResponseSchema(responseSchema);
            if (!isValid)
            {
                _logger.LogWarning("Session {SessionId}: Response validation failed: {Errors}", 
                    sessionId, string.Join(", ", validationErrors));
                    
                return $"Error: Invalid response format. Validation errors: {string.Join(", ", validationErrors)}";
            }

            // Log the structured response
            _sessionLogger.LogInteraction(sessionId, "response_schema", llmResponseJson);

            // Extract and execute the next step if available
            var (toolName, parameters, expectedOutcome) = _communicationService.ExtractExecutableAction(responseSchema);
            
            string finalResponse = responseSchema.Analysis.Summary;
            
            if (!string.IsNullOrEmpty(toolName) && toolName != "None")
            {
                try
                {
                    _logger.LogInformation("Session {SessionId}: Executing tool {ToolName} with expected outcome: {Outcome}", 
                        sessionId, toolName, expectedOutcome);
                        
                    var toolResponse = ExecuteToolFromSchema(sessionId, toolName, parameters);
                    
                    // Update conversation history
                    var interaction = new InteractionHistory
                    {
                        Step = previousInteractions.Count + 1,
                        Query = prompt,
                        Response = llmResponseJson,
                        ToolsUsed = new List<string> { toolName },
                        Timestamp = DateTime.UtcNow,
                        Success = true
                    };
                    
                    SaveInteractionToHistory(sessionId, interaction);
                    
                    finalResponse = $"{responseSchema.Analysis.Summary}\n\nTool Execution Result:\n{toolResponse}";
                    
                    // Check if we need continuation
                    if (!responseSchema.Continuation.IsComplete && responseSchema.Continuation.RequiresUserConfirmation)
                    {
                        finalResponse += $"\n\nNext Expected Input: {responseSchema.Continuation.NextExpectedInput}";
                        finalResponse += $"\nProgress: {responseSchema.Continuation.ProgressPercentage}%";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Session {SessionId}: Tool execution failed for {ToolName}", sessionId, toolName);
                    finalResponse = $"{responseSchema.Analysis.Summary}\n\nTool execution failed: {ex.Message}";
                }
            }

            return finalResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session {SessionId}: AnswerWithSchemaAsync failed", sessionId);
            return $"Error processing request: {ex.Message}";
        }
    }

    private async Task<string> CallLLMWithSchemaAsync(LLMRequestSchema requestSchema, string sessionId)
    {
        try
        {
            // Convert schema to messages format for Ollama
            var systemPrompt = CreateSystemPromptFromSchema(requestSchema);
            var userPrompt = CreateUserPromptFromSchema(requestSchema);
            
            var messages = new List<(string role, string content)>
            {
                ("system", systemPrompt),
                ("user", userPrompt)
            };

            // Log the initial prompts for debugging and transparency
            _sessionFileSystem.WriteFile(sessionId, $"interactions/{DateTime.UtcNow:yyyyMMdd_HHmmss}_system_prompt.txt", 
                $"System Prompt:\n{systemPrompt}\n");
            
            _sessionFileSystem.WriteFile(sessionId, $"interactions/{DateTime.UtcNow:yyyyMMdd_HHmmss}_user_prompt.txt", 
                $"User Prompt:\n{userPrompt}\n");

            _logger.LogInformation("Session {SessionId}: Calling LLM with schema-based request", sessionId);
            _logger.LogDebug("Session {SessionId}: System prompt length: {SystemLength}, User prompt length: {UserLength}", 
                sessionId, systemPrompt.Length, userPrompt.Length);

            var response = await _ollamaClient.ChatAsync(_model, messages);
            
            _logger.LogDebug("Session {SessionId}: Received LLM response with {Length} characters", 
                sessionId, response.Length);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session {SessionId}: CallLLMWithSchemaAsync failed", sessionId);
            throw;
        }
    }

    private string CreateSystemPromptFromSchema(LLMRequestSchema requestSchema)
    {
        var systemPrompt = $@"You are an AI assistant operating with the {requestSchema.Strategy.Name} strategy.

RESPONSE FORMAT REQUIREMENT:
You MUST respond with a valid JSON object matching this exact schema:

{{
  ""sessionId"": ""{requestSchema.SessionId}"",
  ""status"": {{
    ""success"": true,
    ""statusCode"": ""OK"",
    ""message"": ""Response message""
  }},
  ""analysis"": {{
    ""summary"": ""Brief summary of your analysis"",
    ""keyFindings"": [""finding1"", ""finding2""],
    ""riskAssessment"": {{
      ""riskLevel"": ""low|medium|high"",
      ""identifiedRisks"": [""risk1"", ""risk2""]
    }}
  }},
  ""nextStep"": {{
    ""stepNumber"": 1,
    ""action"": ""Specific executable action"",
    ""toolName"": ""ToolName or None"",
    ""parameters"": {{}},
    ""expectedOutcome"": ""What this step should achieve""
  }},
  ""confidence"": {{
    ""overallConfidence"": 0.75,
    ""uncertaintyFactors"": [""factor1"", ""factor2""]
  }},
  ""continuation"": {{
    ""requiresUserConfirmation"": true,
    ""isComplete"": false,
    ""progressPercentage"": 10
  }}
}}

AVAILABLE TOOLS:
{string.Join("\n", requestSchema.AvailableTools.Select(t => $"- {t.Name}: {t.Description}"))}

STRATEGY REQUIREMENTS:
- Risk Level: {requestSchema.Strategy.RiskLevel}
- Require Confirmation: {requestSchema.Strategy.RequireConfirmation}
- Max Steps Per Response: {requestSchema.Strategy.MaxStepsPerResponse}
- Analysis Depth: {requestSchema.Strategy.AnalysisDepth}";

        return systemPrompt;
    }

    private string CreateUserPromptFromSchema(LLMRequestSchema requestSchema)
    {
        var userPrompt = $@"User Query: {requestSchema.UserQuery}

Context:
- Session ID: {requestSchema.SessionId}
- Current Step: {requestSchema.Context.CurrentStep}
- Working Directory: {requestSchema.Context.WorkingDirectory}

Previous Interactions: {requestSchema.PreviousInteractions.Count}

Please analyze this request and provide your response in the required JSON schema format.";

        return userPrompt;
    }

    private string ExecuteToolFromSchema(string sessionId, string toolName, Dictionary<string, object> parameters)
    {
        // Convert parameters dictionary to the format expected by ExecuteTool
        var parametersDict = parameters.ToDictionary(
            p => p.Key, 
            p => p.Value?.ToString() ?? "");
        
        return ExecuteTool(toolName, parametersDict, sessionId);
    }

    private List<InteractionHistory> GetPreviousInteractionsFromHistory(string sessionId)
    {
        // Load from file system if available
        var historyFile = Path.Combine(_sessionFileSystem.GetSessionRoot(sessionId), "interaction_history.json");
        
        if (File.Exists(historyFile))
        {
            try
            {
                var json = File.ReadAllText(historyFile);
                return JsonSerializer.Deserialize<List<InteractionHistory>>(json) ?? new List<InteractionHistory>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load interaction history for session {SessionId}", sessionId);
            }
        }
        
        return new List<InteractionHistory>();
    }

    private void SaveInteractionToHistory(string sessionId, InteractionHistory interaction)
    {
        try
        {
            var historyFile = Path.Combine(_sessionFileSystem.GetSessionRoot(sessionId), "interaction_history.json");
            var interactions = GetPreviousInteractionsFromHistory(sessionId);
            interactions.Add(interaction);
            
            var json = JsonSerializer.Serialize(interactions, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(historyFile, json);
            
            _logger.LogDebug("Session {SessionId}: Saved interaction history with {Count} entries", 
                sessionId, interactions.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save interaction history for session {SessionId}", sessionId);
        }
    }

    private string HandleMissingToolRequest(string sessionId, Dictionary<string, string> parameters)
    {
        try
        {
            _logger.LogInformation("Session {SessionId}: Processing MISSING_TOOL request with reflection", sessionId);

            // Extract required capabilities from parameters
            var requiredCapabilities = new List<string>();
            var requiredToolName = "UnknownTool";
            var reason = "Tool capability requested";
            var sessionSafetyRequirements = "";

            if (parameters.TryGetValue("requiredCapabilities", out var capabilitiesStr))
            {
                // Parse string representation of array, e.g., ["folder:create", "directory:analyze"]
                capabilitiesStr = capabilitiesStr.Trim('[', ']');
                requiredCapabilities = capabilitiesStr.Split(',')
                    .Select(s => s.Trim().Trim('"'))
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            if (parameters.TryGetValue("requiredToolName", out var toolNameStr))
            {
                requiredToolName = toolNameStr ?? "UnknownTool";
            }

            if (parameters.TryGetValue("reason", out var reasonStr))
            {
                reason = reasonStr ?? "Tool capability requested";
            }

            if (parameters.TryGetValue("sessionSafetyRequirements", out var safetyStr))
            {
                sessionSafetyRequirements = safetyStr ?? "";
            }

            // Use reflection to find tools with matching capabilities
            var matchingTools = new List<(ITool tool, List<string> matchedCapabilities)>();
            
            foreach (var capability in requiredCapabilities)
            {
                var toolsWithCapability = _toolRepository.FindToolsByCapability(capability);
                foreach (var tool in toolsWithCapability)
                {
                    var existing = matchingTools.FirstOrDefault(t => t.tool.Name == tool.Name);
                    if (existing.tool != null)
                    {
                        existing.matchedCapabilities.Add(capability);
                    }
                    else
                    {
                        matchingTools.Add((tool, new List<string> { capability }));
                    }
                }
            }

            // Build response with discovered tools
            var response = new StringBuilder();
            response.AppendLine($"MISSING TOOL ANALYSIS - Reflection-Based Discovery:");
            response.AppendLine($"==================================================");
            response.AppendLine($"Requested Tool: {requiredToolName}");
            response.AppendLine($"Required Capabilities: {string.Join(", ", requiredCapabilities)}");
            response.AppendLine($"Reason: {reason}");
            response.AppendLine($"Session Safety: {sessionSafetyRequirements}");
            response.AppendLine();

            if (matchingTools.Any())
            {
                response.AppendLine("✅ COMPATIBLE TOOLS FOUND via reflection:");
                response.AppendLine("========================================");
                
                foreach (var (tool, matchedCapabilities) in matchingTools)
                {
                    response.AppendLine($"• {tool.Name}:");
                    response.AppendLine($"  - Description: {tool.Description}");
                    response.AppendLine($"  - Matched Capabilities: {string.Join(", ", matchedCapabilities)}");
                    response.AppendLine($"  - All Capabilities: {string.Join(", ", tool.Capabilities)}");
                    response.AppendLine($"  - Session Safe: {(tool.RequiresFileSystem ? "Yes (session-isolated)" : "Yes (no file system)")}");
                    response.AppendLine($"  - Usage: Use '{tool.Name}' tool to access these capabilities");
                    response.AppendLine();
                }

                response.AppendLine("RECOMMENDATION:");
                response.AppendLine("===============");
                
                if (requiredCapabilities.Count == 1)
                {
                    var bestMatch = matchingTools
                        .OrderByDescending(t => t.matchedCapabilities.Count)
                        .First();
                    response.AppendLine($"Use '{bestMatch.tool.Name}' tool for '{requiredCapabilities[0]}' capability.");
                }
                else
                {
                    response.AppendLine("Multiple tools needed for all capabilities:");
                    foreach (var capability in requiredCapabilities)
                    {
                        var toolForCapability = matchingTools
                            .FirstOrDefault(t => t.matchedCapabilities.Contains(capability));
                        if (toolForCapability.tool != null)
                        {
                            response.AppendLine($"- Use '{toolForCapability.tool.Name}' for '{capability}'");
                        }
                        else
                        {
                            response.AppendLine($"- ⚠️ No tool found for '{capability}'");
                        }
                    }
                }
            }
            else
            {
                response.AppendLine("❌ NO COMPATIBLE TOOLS FOUND:");
                response.AppendLine("==============================");
                response.AppendLine("The requested capabilities are not available in the current tool repository.");
                response.AppendLine("Available tools and their capabilities:");
                
                var allTools = _toolRepository.GetAllTools();
                foreach (var tool in allTools)
                {
                    response.AppendLine($"• {tool.Name}: {string.Join(", ", tool.Capabilities)}");
                }
                
                response.AppendLine();
                response.AppendLine("SUGGESTION: The requested functionality may need to be implemented as a new tool.");
            }

            var result = response.ToString();
            _logger.LogInformation("Session {SessionId}: MISSING_TOOL analysis completed, found {Count} compatible tools", 
                sessionId, matchingTools.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session {SessionId}: Error processing MISSING_TOOL request", sessionId);
            return $"Error processing MISSING_TOOL request: {ex.Message}";
        }
    }
}
