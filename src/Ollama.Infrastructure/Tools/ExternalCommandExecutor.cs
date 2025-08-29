using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Ollama.Infrastructure.Tools
{
    /// <summary>
    /// Executes external command-line tools as a fallback mechanism when other tools fail.
    /// Used primarily for retry scenarios with commands like git clone, curl, wget, etc.
    /// SECURITY: All commands are executed within session boundaries and cannot escape.
    /// </summary>
    public class ExternalCommandExecutor : AbstractTool
    {
        private readonly ISessionFileSystem _sessionFileSystem;

        public override string Name => "ExternalCommandExecutor";
        public override string Description => "Executes external command-line tools for system operations, primarily as fallback mechanisms (session-isolated)";
        public override IEnumerable<string> Capabilities => new[] { "command:execute", "system:external", "fallback:operations" };
        public override bool RequiresNetwork => false; // Depends on the command being executed
        public override bool RequiresFileSystem => true;

        public ExternalCommandExecutor(ISessionScope sessionScope, ILogger<ExternalCommandExecutor> logger, ISessionFileSystem sessionFileSystem)
            : base(sessionScope, logger)
        {
            _sessionFileSystem = sessionFileSystem;
        }

        public override Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(context.Parameters.ContainsKey("command"));
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // No cost for external commands
        }

        public override IEnumerable<string> GetAlternativeMethods()
        {
            return new[] { "powershell_execution", "direct_binary_execution", "batch_file_execution", "shell_retry" };
        }

        protected override async Task<ToolResult> RunAlternativeMethodAsync(ToolContext context, string methodName, CancellationToken cancellationToken = default)
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
                        ExecutionTime = DateTime.Now - startTime,
                        MethodUsed = methodName
                    };
                }

                if (string.IsNullOrEmpty(context.SessionId))
                {
                    return new ToolResult
                    {
                        Success = false,
                        Output = "Session ID is required for external command execution",
                        ExecutionTime = DateTime.Now - startTime,
                        MethodUsed = methodName
                    };
                }

                var requestedWorkingDirectory = context.Parameters.TryGetValue("workingDirectory", out var wdObj) ? wdObj?.ToString() : null;
                var timeoutSeconds = context.Parameters.TryGetValue("timeoutSeconds", out var timeoutObj) 
                    ? (timeoutObj is int timeout ? timeout : 30) 
                    : 30;

                string workingDirectory;
                if (!string.IsNullOrEmpty(requestedWorkingDirectory))
                {
                    if (!_sessionFileSystem.IsWorkingDirectoryValid(context.SessionId, requestedWorkingDirectory))
                    {
                        return new ToolResult
                        {
                            Success = false,
                            Output = $"Working directory '{requestedWorkingDirectory}' is outside session boundaries",
                            ExecutionTime = DateTime.Now - startTime,
                            MethodUsed = methodName
                        };
                    }
                    workingDirectory = requestedWorkingDirectory;
                }
                else
                {
                    workingDirectory = _sessionFileSystem.GetSafeWorkingDirectory(context.SessionId);
                }

                var result = methodName switch
                {
                    "powershell_execution" => await ExecuteWithPowerShell(command, workingDirectory, timeoutSeconds, cancellationToken),
                    "direct_binary_execution" => await ExecuteDirectBinary(command, workingDirectory, timeoutSeconds, cancellationToken),
                    "batch_file_execution" => await ExecuteWithBatchFile(command, workingDirectory, timeoutSeconds, cancellationToken),
                    "shell_retry" => await ExecuteWithShellRetry(command, workingDirectory, timeoutSeconds, cancellationToken),
                    _ => throw new NotSupportedException($"Alternative method '{methodName}' is not supported")
                };

                return new ToolResult
                {
                    Success = result.ExitCode == 0,
                    Output = result.Success ? result.Output : result.Error,
                    ErrorMessage = result.Success ? null : $"Command failed with exit code {result.ExitCode}: {result.Error}",
                    ExecutionTime = DateTime.Now - startTime,
                    MethodUsed = methodName
                };
            }
            catch (Exception ex)
            {
                return new ToolResult
                {
                    Success = false,
                    Output = $"External command execution failed with method {methodName}: {ex.Message}",
                    ExecutionTime = DateTime.Now - startTime,
                    MethodUsed = methodName
                };
            }
        }

        public override async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
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

        #region Alternative Execution Methods

        /// <summary>
        /// Alternative method 1: Execute using PowerShell instead of CMD
        /// </summary>
        private async Task<(bool Success, int ExitCode, string Output, string Error)> ExecuteWithPowerShell(
            string command, string workingDirectory, int timeoutSeconds, CancellationToken cancellationToken)
        {
            var process = new Process();
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            try
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Set-Location '{workingDirectory}'; {command}\"",
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                return await ExecuteProcessWithTimeout(process, outputBuilder, errorBuilder, timeoutSeconds, cancellationToken);
            }
            finally
            {
                process?.Dispose();
            }
        }

        /// <summary>
        /// Alternative method 2: Execute binary directly without shell wrapper
        /// </summary>
        private async Task<(bool Success, int ExitCode, string Output, string Error)> ExecuteDirectBinary(
            string command, string workingDirectory, int timeoutSeconds, CancellationToken cancellationToken)
        {
            var process = new Process();
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            try
            {
                // Parse command to separate executable and arguments
                var parts = command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var executable = parts[0];
                var arguments = parts.Length > 1 ? parts[1] : "";

                process.StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                return await ExecuteProcessWithTimeout(process, outputBuilder, errorBuilder, timeoutSeconds, cancellationToken);
            }
            catch (Exception ex)
            {
                return (false, -1, "", $"Direct binary execution failed: {ex.Message}");
            }
            finally
            {
                process?.Dispose();
            }
        }

        /// <summary>
        /// Alternative method 3: Create temporary batch file and execute it
        /// </summary>
        private async Task<(bool Success, int ExitCode, string Output, string Error)> ExecuteWithBatchFile(
            string command, string workingDirectory, int timeoutSeconds, CancellationToken cancellationToken)
        {
            var process = new Process();
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            var batchFilePath = Path.Combine(workingDirectory, $"temp_command_{Guid.NewGuid():N}.bat");

            try
            {
                // Create temporary batch file
                var batchContent = $"@echo off\ncd /d \"{workingDirectory}\"\n{command}";
                await System.IO.File.WriteAllTextAsync(batchFilePath, batchContent, cancellationToken);

                process.StartInfo = new ProcessStartInfo
                {
                    FileName = batchFilePath,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                return await ExecuteProcessWithTimeout(process, outputBuilder, errorBuilder, timeoutSeconds, cancellationToken);
            }
            finally
            {
                // Clean up temporary file
                try
                {
                    if (System.IO.File.Exists(batchFilePath))
                        System.IO.File.Delete(batchFilePath);
                }
                catch { /* Ignore cleanup errors */ }
                
                process?.Dispose();
            }
        }

        /// <summary>
        /// Alternative method 4: Execute with retry mechanism and shell variations
        /// </summary>
        private async Task<(bool Success, int ExitCode, string Output, string Error)> ExecuteWithShellRetry(
            string command, string workingDirectory, int timeoutSeconds, CancellationToken cancellationToken)
        {
            const int maxRetries = 3;
            var shells = new[] { "cmd.exe", "powershell.exe" };
            var delay = TimeSpan.FromMilliseconds(100);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                foreach (var shell in shells)
                {
                    var process = new Process();
                    var outputBuilder = new StringBuilder();
                    var errorBuilder = new StringBuilder();

                    try
                    {
                        var arguments = shell.Contains("powershell") 
                            ? $"-NoProfile -ExecutionPolicy Bypass -Command \"Set-Location '{workingDirectory}'; {command}\""
                            : $"/c {command}";

                        process.StartInfo = new ProcessStartInfo
                        {
                            FileName = shell,
                            Arguments = arguments,
                            WorkingDirectory = workingDirectory,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        var result = await ExecuteProcessWithTimeout(process, outputBuilder, errorBuilder, timeoutSeconds, cancellationToken);
                        
                        if (result.Success || attempt == maxRetries)
                        {
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (attempt == maxRetries)
                            return (false, -1, "", $"Retry execution failed: {ex.Message}");
                    }
                    finally
                    {
                        process?.Dispose();
                    }
                }

                if (attempt < maxRetries)
                {
                    await Task.Delay(delay, cancellationToken);
                    delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
                }
            }

            return (false, -1, "", "All retry attempts failed");
        }

        /// <summary>
        /// Common method to execute process with timeout handling
        /// </summary>
        private async Task<(bool Success, int ExitCode, string Output, string Error)> ExecuteProcessWithTimeout(
            Process process, StringBuilder outputBuilder, StringBuilder errorBuilder, int timeoutSeconds, CancellationToken cancellationToken)
        {
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

        #endregion
    }
}
