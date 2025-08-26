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
            _logger.LogInformation("🔌 Python subsystem is disabled in configuration");
            return false;
        }

        if (IsRunning)
        {
            _logger.LogInformation("✅ Python subsystem is already running (PID: {ProcessId})", _pythonProcess?.Id);
            return true;
        }

        try
        {
            _logger.LogInformation("🚀 Starting Python subsystem in isolated process...");
            _logger.LogInformation("🔧 Python subsystem path from config: {Path}", _settings.Path);
            
            var pythonPath = FindPythonExecutable();
            var scriptPath = Path.Combine(_settings.Path, _settings.Script);
            
            _logger.LogInformation("📂 Computed script path: {ScriptPath}", scriptPath);
            _logger.LogInformation("📁 Current working directory: {CurrentDirectory}", Directory.GetCurrentDirectory());
            _logger.LogInformation("📄 Script exists: {Exists}", File.Exists(scriptPath));
            
            if (!File.Exists(scriptPath))
            {
                _logger.LogError("❌ Python script not found at {ScriptPath}", scriptPath);
                _logger.LogInformation("📁 Contents of directory {Directory}: {Files}", 
                    Path.GetDirectoryName(scriptPath) ?? "unknown", 
                    Directory.Exists(Path.GetDirectoryName(scriptPath)) 
                        ? string.Join(", ", Directory.GetFiles(Path.GetDirectoryName(scriptPath)!))
                        : "directory does not exist");
                return false;
            }

            _logger.LogInformation("🔍 Python executable: {PythonPath}", pythonPath);
            _logger.LogInformation("📁 Working directory: {WorkingDirectory}", _settings.Path);
            _logger.LogInformation("📄 Script path: {ScriptPath}", scriptPath);

            var processInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"-m uvicorn main:app --host 127.0.0.1 --port {_settings.Port}",
                WorkingDirectory = _settings.Path,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                // Ensure process isolation
                WindowStyle = ProcessWindowStyle.Hidden
            };

            _logger.LogInformation("⚡ Starting isolated Python process: {Command} {Arguments}", 
                processInfo.FileName, processInfo.Arguments);

            _pythonProcess = Process.Start(processInfo);
            
            if (_pythonProcess == null)
            {
                _logger.LogError("❌ Failed to start Python process");
                return false;
            }

            _logger.LogInformation("🎯 Python process started with PID: {ProcessId}", _pythonProcess.Id);

            // Start monitoring stdout/stderr in background
            _ = Task.Run(() => MonitorProcessOutput(_pythonProcess), cancellationToken);

            // Wait for service to be ready
            var timeout = TimeSpan.FromSeconds(_settings.StartupTimeoutSeconds);
            _logger.LogInformation("⏱️ Waiting for service to be ready (timeout: {Timeout}s)...", _settings.StartupTimeoutSeconds);
            
            var started = await WaitForServiceReady(timeout, cancellationToken);
            
            if (started)
            {
                _logger.LogInformation("✅ Python subsystem started successfully on {ServiceUrl} (PID: {ProcessId})", 
                    ServiceUrl, _pythonProcess.Id);
            }
            else
            {
                _logger.LogError("❌ Python subsystem failed to start within timeout");
                await StopAsync(cancellationToken);
            }

            return started;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error starting Python subsystem");
            return false;
        }
    }

    public async Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        if (_pythonProcess == null)
        {
            _logger.LogInformation("🔍 Python subsystem is not running");
            return true;
        }

        try
        {
            _logger.LogInformation("🛑 Stopping Python subsystem (PID: {ProcessId})...", _pythonProcess.Id);
            
            if (!_pythonProcess.HasExited)
            {
                _logger.LogInformation("⏹️ Terminating Python process gracefully...");
                
                // Try graceful shutdown first
                try
                {
                    _pythonProcess.CloseMainWindow();
                    
                    // Wait a bit for graceful shutdown
                    var gracefulTimeout = TimeSpan.FromSeconds(Math.Min(_settings.ShutdownTimeoutSeconds, 5));
                    var exited = _pythonProcess.WaitForExit((int)gracefulTimeout.TotalMilliseconds);
                    
                    if (!exited && !_pythonProcess.HasExited)
                    {
                        _logger.LogWarning("⚠️ Graceful shutdown timeout, forcing process termination...");
                        _pythonProcess.Kill();
                        await _pythonProcess.WaitForExitAsync(cancellationToken);
                    }
                }
                catch (Exception killEx)
                {
                    _logger.LogWarning(killEx, "⚠️ Exception during process termination, forcing kill...");
                    _pythonProcess.Kill();
                    await _pythonProcess.WaitForExitAsync(cancellationToken);
                }
            }

            _pythonProcess.Dispose();
            _pythonProcess = null;
            
            _logger.LogInformation("✅ Python subsystem stopped successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error stopping Python subsystem");
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

    private void MonitorProcessOutput(Process process)
    {
        try
        {
            // Monitor stdout
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        var line = await process.StandardOutput.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line))
                        {
                            _logger.LogInformation("🐍 [STDOUT] {Line}", line);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("STDOUT monitoring ended: {Error}", ex.Message);
                }
            });

            // Monitor stderr
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!process.StandardError.EndOfStream)
                    {
                        var line = await process.StandardError.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line))
                        {
                            _logger.LogWarning("🐍 [STDERR] {Line}", line);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("STDERR monitoring ended: {Error}", ex.Message);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error monitoring Python process output");
        }
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
