using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http;
using Ollama.Domain.Configuration;

namespace Ollama.Infrastructure.Services;

public class PythonSubsystemService : IPythonSubsystemService, IDisposable
{
    private readonly ILogger<PythonSubsystemService> _logger;
    private readonly PythonSubsystemSettings _settings;
    private readonly HttpClient _httpClient;
    private Process? _pythonProcess;
    private bool _disposed = false;

    public PythonSubsystemService(
        ILogger<PythonSubsystemService> logger,
        IOptions<PythonSubsystemSettings> settings,
        HttpClient httpClient)
    {
        _logger = logger;
        _settings = settings.Value;
        _httpClient = httpClient;
    }

    public bool IsRunning => 
        _pythonProcess != null && 
        !_pythonProcess.HasExited;

    public string ServiceUrl => $"http://localhost:{_settings.Port}";

    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Python subsystem is disabled in configuration");
            return false;
        }

        if (IsRunning)
        {
            _logger.LogInformation("Python subsystem is already running");
            return true;
        }

        try
        {
            var pythonPath = FindPythonExecutable();
            var scriptPath = Path.Combine(_settings.Path, _settings.Script);
            
            if (!File.Exists(scriptPath))
            {
                _logger.LogError("Python script not found at {ScriptPath}", scriptPath);
                return false;
            }

            var processInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"-m uvicorn main:app --host 127.0.0.1 --port {_settings.Port}",
                WorkingDirectory = _settings.Path,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _logger.LogInformation("Starting Python subsystem: {Command} {Arguments}", 
                processInfo.FileName, processInfo.Arguments);

            _pythonProcess = Process.Start(processInfo);
            
            if (_pythonProcess == null)
            {
                _logger.LogError("Failed to start Python process");
                return false;
            }

            // Wait for service to be ready
            var timeout = TimeSpan.FromSeconds(_settings.StartupTimeoutSeconds);
            var started = await WaitForServiceReady(timeout, cancellationToken);
            
            if (started)
            {
                _logger.LogInformation("Python subsystem started successfully on {ServiceUrl}", ServiceUrl);
            }
            else
            {
                _logger.LogError("Python subsystem failed to start within timeout");
                await StopAsync(cancellationToken);
            }

            return started;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Python subsystem");
            return false;
        }
    }

    public async Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        if (_pythonProcess == null)
        {
            return true;
        }

        try
        {
            _logger.LogInformation("Stopping Python subsystem...");
            
            if (!_pythonProcess.HasExited)
            {
                _pythonProcess.Kill();
                await _pythonProcess.WaitForExitAsync(cancellationToken);
            }

            _pythonProcess.Dispose();
            _pythonProcess = null;
            
            _logger.LogInformation("Python subsystem stopped");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Python subsystem");
            return false;
        }
    }

    private async Task<bool> WaitForServiceReady(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var endTime = DateTime.UtcNow.Add(timeout);
        
        while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ServiceUrl}/docs", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // Service not ready yet, continue waiting
            }

            await Task.Delay(500, cancellationToken);
        }

        return false;
    }

    private string FindPythonExecutable()
    {
        var pythonCommands = new[] { "python", "python3", "py" };
        
        foreach (var command in pythonCommands)
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });

                if (process != null)
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        return command;
                    }
                }
            }
            catch
            {
                continue;
            }
        }

        throw new InvalidOperationException("Python executable not found. Please ensure Python is installed and in PATH.");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopAsync().Wait();
            _disposed = true;
        }
    }
}
