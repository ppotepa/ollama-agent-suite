using System.Diagnostics;

namespace Ollama.Infrastructure.Tools;

public class ExternalCommandDetector
{
    private readonly Dictionary<string, string> _availableCommands = new();

    public ExternalCommandDetector()
    {
    }

    public async Task<Dictionary<string, string>> DetectAvailableCommandsAsync()
    {
        var commandsToCheck = new Dictionary<string, string[]>
        {
            // Git commands
            ["git"] = new[] { "git --version" },
            
            // Download tools
            ["curl"] = new[] { "curl --version" },
            ["wget"] = new[] { "wget --version" },
            ["Invoke-WebRequest"] = new[] { "powershell -Command \"Get-Command Invoke-WebRequest\"" },
            
            // Archive tools
            ["7z"] = new[] { "7z" },
            ["tar"] = new[] { "tar --version" },
            ["unzip"] = new[] { "unzip -v" },
            ["Expand-Archive"] = new[] { "powershell -Command \"Get-Command Expand-Archive\"" },
            
            // Python tools
            ["python"] = new[] { "python --version", "python3 --version" },
            ["pip"] = new[] { "pip --version", "pip3 --version" },
            
            // Node.js tools
            ["node"] = new[] { "node --version" },
            ["npm"] = new[] { "npm --version" },
            
            // System tools
            ["mkdir"] = new[] { "mkdir" },
            ["cd"] = new[] { "cd" },
            ["dir"] = new[] { "dir" },
            ["ls"] = new[] { "ls --version" },
            ["powershell"] = new[] { "powershell -Command \"$PSVersionTable.PSVersion\"" },
            ["cmd"] = new[] { "cmd /c ver" }
        };

        var tasks = commandsToCheck.Select(async kvp =>
        {
            var commandName = kvp.Key;
            var testCommands = kvp.Value;
            
            foreach (var testCommand in testCommands)
            {
                try
                {
                    var available = await IsCommandAvailableAsync(testCommand);
                    if (available)
                    {
                        _availableCommands[commandName] = testCommand;
                        break;
                    }
                }
                catch
                {
                    // Ignore errors during detection
                }
            }
        });

        await Task.WhenAll(tasks);
        return new Dictionary<string, string>(_availableCommands);
    }

    private async Task<bool> IsCommandAvailableAsync(string command)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd" : "/bin/bash",
                Arguments = OperatingSystem.IsWindows() ? $"/c {command}" : $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public string GetAvailableCommandsDescription()
    {
        if (!_availableCommands.Any())
            return "No external commands detected.";

        var description = "Available external commands:\n";
        
        if (_availableCommands.ContainsKey("git"))
            description += "- Git: Repository cloning, version control operations\n";
        if (_availableCommands.ContainsKey("curl"))
            description += "- Curl: HTTP downloads, API requests\n";
        if (_availableCommands.ContainsKey("wget"))
            description += "- Wget: File downloads\n";
        if (_availableCommands.ContainsKey("Invoke-WebRequest"))
            description += "- PowerShell Invoke-WebRequest: HTTP downloads\n";
        if (_availableCommands.ContainsKey("python"))
            description += "- Python: Script execution, package management\n";
        if (_availableCommands.ContainsKey("powershell"))
            description += "- PowerShell: Windows scripting, system operations\n";
        if (_availableCommands.ContainsKey("7z") || _availableCommands.ContainsKey("tar") || _availableCommands.ContainsKey("unzip"))
            description += "- Archive tools: Extract compressed files\n";

        return description;
    }
}
