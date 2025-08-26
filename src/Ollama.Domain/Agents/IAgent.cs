namespace Ollama.Domain.Agents;

public interface IAgent
{
    string Answer(string prompt);
    string Think(string prompt);
    object Plan(string prompt);
    object Act(string instruction);
}

public interface ICommandExecutorPort
{
    CommandResult Run(string command, string? workingDirectory = null);
}

public record CommandResult(bool Success, string StdOut = "", string StdErr = "");
