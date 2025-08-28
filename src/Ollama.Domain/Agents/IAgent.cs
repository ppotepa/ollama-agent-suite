namespace Ollama.Domain.Agents;

public interface IAgent
{
    string Answer(string prompt, string? sessionId = null);
    string Think(string prompt);
    string Think(string prompt, string? sessionId);
    object Plan(string prompt);
    object Plan(string prompt, string? sessionId);
    object Act(string instruction);
}

public interface ICommandExecutorPort
{
    CommandResult Run(string command, string? workingDirectory = null);
}

public record CommandResult(bool Success, string StdOut = "", string StdErr = "");
