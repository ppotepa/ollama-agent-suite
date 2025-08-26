namespace Ollama.Domain.Configuration;

public class PythonSubsystemSettings
{
    public bool Enabled { get; set; } = true;
    public string Path { get; set; } = "python_subsystem";
    public string Script { get; set; } = "main.py";
    public int Port { get; set; } = 8000;
    public int StartupTimeoutSeconds { get; set; } = 10;
    public int ShutdownTimeoutSeconds { get; set; } = 5;
}
