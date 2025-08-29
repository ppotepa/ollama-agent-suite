using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using System.Diagnostics;
using System.Text;

namespace Ollama.Infrastructure.Tools
{
    /// <summary>
    /// Executes external command-line tools as a fallback mechanism when other tools fail.
    /// Used primarily for retry scenarios with commands like git clone, curl, wget, etc.
    /// SECURITY: All commands are executed within session boundaries and cannot escape.
    /// </summary>
    public class ExternalCommandExecutor : ITool
    {
        private readonly ISessionFileSystem _sessionFileSystem;

        public string Name => "ExternalCommandExecutor";
        public string Description => "Executes external command-line tools for system operations, primarily as fallback mechanisms (session-isolated)";
        public IEnumerable<string> Capabilities => new[] { "command:execute", "system:external", "fallback:operations" };
        public bool RequiresNetwork => false; // Depends on the command being executed
        public bool RequiresFileSystem => true;

        public ExternalCommandExecutor(ISessionFileSystem sessionFileSystem)
        {
            _sessionFileSystem = sessionFileSystem;
        }

        public Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(context.Parameters.ContainsKey("command"));
        }

        public Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for external commands
        }

        public async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            try
            {
                if (!context.Parameters.TryGetValue("command", out var commandObj) || commandObj is not string command)
                {
                    return new ToolResult
                    {
                        Success = false,
                        Output = "Missing required parameter: command",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }

                // Validate session context
                if (string.IsNullOrEmpty(context.SessionId))
                {
                    return new ToolResult
                    {
                        Success = false,
                        Output = "Session ID is required for external command execution",
                        ExecutionTime = DateTime.Now - startTime
                    };
                }

                // Extract optional parameters
                var requestedWorkingDirectory = context.Parameters.TryGetValue("workingDirectory", out var wdObj) ? wdObj?.ToString() : null;
                var timeoutSeconds = context.Parameters.TryGetValue("timeoutSeconds", out var timeoutObj) 
                    ? (timeoutObj is int timeout ? timeout : 30) 
                    : 30;

                // Validate and get safe working directory
                string workingDirectory;
                if (!string.IsNullOrEmpty(requestedWorkingDirectory))
                {
                    if (!_sessionFileSystem.IsWorkingDirectoryValid(context.SessionId, requestedWorkingDirectory))
                    {
                        return new ToolResult
                        {
                            Success = false,
                            Output = $"Working directory '{requestedWorkingDirectory}' is outside session boundaries",
                            ExecutionTime = DateTime.Now - startTime
                        };
                    }
                    workingDirectory = requestedWorkingDirectory;
                }
                else
                {
                    // Use session-safe working directory
                    workingDirectory = _sessionFileSystem.GetSafeWorkingDirectory(context.SessionId);
                }

                var result = await ExecuteCommandAsync(command, workingDirectory, timeoutSeconds, cancellationToken);
                
                return new ToolResult
                {
                    Success = result.ExitCode == 0,
                    Output = result.Success ? result.Output : result.Error,
                    ErrorMessage = result.Success ? null : $"Command failed with exit code {result.ExitCode}: {result.Error}",
                    ExecutionTime = DateTime.Now - startTime
                };
            }
            catch (Exception ex)
            {
                return new ToolResult
                {
                    Success = false,
                    Output = $"External command execution failed: {ex.Message}",
                    ExecutionTime = DateTime.Now - startTime
                };
            }
        }

        private async Task<(bool Success, int ExitCode, string Output, string Error)> ExecuteCommandAsync(
            string command, string? workingDirectory, int timeoutSeconds, CancellationToken cancellationToken)
        {
            var process = new Process();
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            try
            {
                // Configure process
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Set up output/error handling
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        outputBuilder.AppendLine(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        errorBuilder.AppendLine(e.Data);
                };

                // Start process
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Wait for completion with timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                try
                {
                    await process.WaitForExitAsync(combinedCts.Token);
                }
                catch (OperationCanceledException)
                {
                    try
                    {
                        process.Kill(true);
                    }
                    catch { /* Ignore kill errors */ }

                    return (false, -1, "", "Command timed out or was cancelled");
                }

                var output = outputBuilder.ToString();
                var error = errorBuilder.ToString();
                
                return (process.ExitCode == 0, process.ExitCode, output, error);
            }
            finally
            {
                process?.Dispose();
            }
        }
    }
}
