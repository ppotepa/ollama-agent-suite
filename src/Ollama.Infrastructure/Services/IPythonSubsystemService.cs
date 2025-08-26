namespace Ollama.Infrastructure.Services;

public interface IPythonSubsystemService
{
    Task<bool> StartAsync(CancellationToken cancellationToken = default);
    Task<bool> StopAsync(CancellationToken cancellationToken = default);
    bool IsRunning { get; }
    string ServiceUrl { get; }
}
