using Microsoft.Extensions.Logging;
using Ollama.Domain.Services;
using System.Text.Json;

namespace Ollama.Infrastructure.Services;

/// <summary>
/// Enhanced session logging service that consolidates timestamped files and provides extended tool logging
/// </summary>
public class SessionLogger
{
    private readonly ISessionFileSystem _sessionFileSystem;
    private readonly ILogger<SessionLogger> _logger;
    private readonly Dictionary<string, SessionLogState> _sessionStates = new();

    public SessionLogger(ISessionFileSystem sessionFileSystem, ILogger<SessionLogger> logger)
    {
        _sessionFileSystem = sessionFileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Logs an interaction entry to a consolidated file (no timestamps in filename)
    /// </summary>
    public void LogInteraction(string sessionId, string interactionType, string content, 
        string? additionalInfo = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        var logEntry = $"[{timestamp}] {interactionType.ToUpperInvariant()}\n{content}\n";
        
        if (!string.IsNullOrEmpty(additionalInfo))
        {
            logEntry += $"\nAdditional Info: {additionalInfo}\n";
        }
        
        logEntry += "\n" + new string('=', 80) + "\n\n";
        
        var fileName = $"interactions/{interactionType}_log.txt";
        AppendToFile(sessionId, fileName, logEntry);
    }

    /// <summary>
    /// Logs tool execution with extended parameters and context
    /// </summary>
    public void LogToolExecution(string sessionId, string toolName, Dictionary<string, string> parameters, 
        string response, int iteration, string? context = null, Exception? error = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        
        var toolLog = new
        {
            Timestamp = timestamp,
            Iteration = iteration,
            ToolName = toolName,
            Parameters = parameters,
            Context = context,
            Response = response,
            Error = error?.ToString(),
            Success = error == null
        };

        var logEntry = $"[{timestamp}] TOOL EXECUTION\n";
        logEntry += $"Tool: {toolName}\n";
        logEntry += $"Iteration: {iteration}\n";
        logEntry += $"Parameters:\n{JsonSerializer.Serialize(parameters, new JsonSerializerOptions { WriteIndented = true })}\n";
        
        if (!string.IsNullOrEmpty(context))
        {
            logEntry += $"Context: {context}\n";
        }
        
        if (error != null)
        {
            logEntry += $"ERROR: {error.Message}\n";
            logEntry += $"Stack Trace: {error.StackTrace}\n";
        }
        else
        {
            logEntry += $"Response: {response}\n";
        }
        
        logEntry += "\n" + new string('=', 80) + "\n\n";
        
        // Append to consolidated tool log
        AppendToFile(sessionId, "tools/tool_execution_log.txt", logEntry);
        
        // Also create detailed JSON log for programmatic analysis
        var jsonEntry = JsonSerializer.Serialize(toolLog, new JsonSerializerOptions { WriteIndented = true });
        AppendToFile(sessionId, "tools/tool_execution_detailed.json", jsonEntry + ",\n");
    }

    /// <summary>
    /// Logs thinking processes to a consolidated file
    /// </summary>
    public void LogThinking(string sessionId, string thought, string? result = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        var logEntry = $"[{timestamp}] THINKING\n{thought}\n";
        
        if (!string.IsNullOrEmpty(result))
        {
            logEntry += $"Result: {result}\n";
        }
        
        logEntry += "\n" + new string('=', 80) + "\n\n";
        
        AppendToFile(sessionId, "thinking/thinking_log.txt", logEntry);
    }

    /// <summary>
    /// Logs planning activities to a consolidated file
    /// </summary>
    public void LogPlan(string sessionId, string planContent, string? planType = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        var logEntry = $"[{timestamp}] PLAN";
        
        if (!string.IsNullOrEmpty(planType))
        {
            logEntry += $" ({planType})";
        }
        
        logEntry += $"\n{planContent}\n\n" + new string('=', 80) + "\n\n";
        
        AppendToFile(sessionId, "plans/plans_log.txt", logEntry);
    }

    /// <summary>
    /// Logs action executions to a consolidated file
    /// </summary>
    public void LogAction(string sessionId, string actionContent, string? actionType = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        var logEntry = $"[{timestamp}] ACTION";
        
        if (!string.IsNullOrEmpty(actionType))
        {
            logEntry += $" ({actionType})";
        }
        
        logEntry += $"\n{actionContent}\n\n" + new string('=', 80) + "\n\n";
        
        AppendToFile(sessionId, "actions/actions_log.txt", logEntry);
    }

    /// <summary>
    /// Logs conversation context to a consolidated file
    /// </summary>
    public void LogConversationContext(string sessionId, string context)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        var logEntry = $"[{timestamp}] CONVERSATION CONTEXT\n{context}\n\n" + new string('=', 80) + "\n\n";
        
        AppendToFile(sessionId, "interactions/conversation_context_log.txt", logEntry);
    }

    /// <summary>
    /// Logs session-level information (initial prompt, query, response, etc.)
    /// </summary>
    public void LogSessionInfo(string sessionId, string infoType, string content)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        var logEntry = $"[{timestamp}] {infoType.ToUpperInvariant()}\n{content}\n\n" + new string('=', 80) + "\n\n";
        
        AppendToFile(sessionId, "session_info_log.txt", logEntry);
    }

    /// <summary>
    /// Gets session statistics for debugging
    /// </summary>
    public Dictionary<string, object> GetSessionStats(string sessionId)
    {
        if (!_sessionStates.ContainsKey(sessionId))
        {
            return new Dictionary<string, object>();
        }

        var state = _sessionStates[sessionId];
        return new Dictionary<string, object>
        {
            ["SessionId"] = sessionId,
            ["StartTime"] = state.StartTime,
            ["LastActivity"] = state.LastActivity,
            ["InteractionCount"] = state.InteractionCount,
            ["ToolExecutionCount"] = state.ToolExecutionCount,
            ["ThinkingCount"] = state.ThinkingCount,
            ["FilesCreated"] = state.FilesCreated.ToList()
        };
    }

    private void AppendToFile(string sessionId, string relativePath, string content)
    {
        // Ensure session state tracking
        if (!_sessionStates.ContainsKey(sessionId))
        {
            _sessionStates[sessionId] = new SessionLogState
            {
                SessionId = sessionId,
                StartTime = DateTime.UtcNow
            };
        }

        var state = _sessionStates[sessionId];
        state.LastActivity = DateTime.UtcNow;
        state.FilesCreated.Add(relativePath);

        // Update counters based on file type
        if (relativePath.Contains("interaction"))
            state.InteractionCount++;
        else if (relativePath.Contains("tool"))
            state.ToolExecutionCount++;
        else if (relativePath.Contains("thinking"))
            state.ThinkingCount++;

        try
        {
            // Check if file exists to determine if we need to append or create
            var sessionRoot = _sessionFileSystem.GetSessionRoot(sessionId);
            var fullPath = Path.Combine(sessionRoot, relativePath);
            
            if (File.Exists(fullPath))
            {
                // Append to existing file
                File.AppendAllText(fullPath, content);
            }
            else
            {
                // Create new file with content
                _sessionFileSystem.WriteFile(sessionId, relativePath, content);
            }

            _logger.LogDebug("Session {SessionId}: Appended to log file {File}", sessionId, relativePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Session {SessionId}: Failed to write to log file {File}", sessionId, relativePath);
        }
    }

    private class SessionLogState
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime LastActivity { get; set; }
        public int InteractionCount { get; set; }
        public int ToolExecutionCount { get; set; }
        public int ThinkingCount { get; set; }
        public HashSet<string> FilesCreated { get; set; } = new();
    }
}
