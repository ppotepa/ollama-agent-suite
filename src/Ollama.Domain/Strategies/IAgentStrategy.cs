namespace Ollama.Domain.Strategies;

/// <summary>
/// Defines how an agent shapes its responses based on a specific strategy
/// </summary>
public interface IAgentStrategy
{
    /// <summary>
    /// Strategy name for identification
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Initial prompt used to shape LLM responses and behavior
    /// This is sent as the system message to establish the agent's behavior
    /// </summary>
    string GetInitialPrompt();
    
    /// <summary>
    /// Process the user query to structure LLM interaction
    /// </summary>
    /// <param name="userQuery">The user's input query</param>
    /// <param name="sessionId">Session identifier for context</param>
    /// <returns>Formatted prompt for the LLM</returns>
    string FormatQueryPrompt(string userQuery, string? sessionId = null);
    
    /// <summary>
    /// Evaluate if task is complete based on the response
    /// </summary>
    /// <param name="response">LLM response to evaluate</param>
    /// <returns>True if the task is complete</returns>
    bool IsTaskComplete(string response);
    
    /// <summary>
    /// Extract the next step to take from the LLM response
    /// </summary>
    /// <param name="response">LLM response to parse</param>
    /// <returns>Description of the next step</returns>
    string GetNextStep(string response);
    
    /// <summary>
    /// Extract tool request if any from the LLM response
    /// </summary>
    /// <param name="response">LLM response to parse</param>
    /// <returns>Tool name and parameters if tool is requested, null otherwise</returns>
    (string toolName, Dictionary<string, string> parameters)? ExtractToolRequest(string response);
    
    /// <summary>
    /// Format the tool response for the next LLM interaction
    /// </summary>
    /// <param name="toolName">Name of the tool that was executed</param>
    /// <param name="toolResponse">Response from the tool</param>
    /// <returns>Formatted message for LLM</returns>
    string FormatToolResponse(string toolName, string toolResponse);

    /// <summary>
    /// Validate and possibly modify LLM response before returning to user
    /// </summary>
    /// <param name="response">Raw LLM response</param>
    /// <param name="sessionId">Session identifier</param>
    /// <returns>Validated/modified response</returns>
    string ValidateResponse(string response, string? sessionId = null);

    /// <summary>
    /// Handle error scenarios according to strategy
    /// </summary>
    /// <param name="error">Error message or exception details</param>
    /// <param name="context">Context where error occurred</param>
    /// <returns>Strategy-appropriate error response</returns>
    string HandleError(string error, string context);
}
