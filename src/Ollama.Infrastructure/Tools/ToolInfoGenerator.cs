using Ollama.Domain.Tools;
using System.Reflection;
using System.Text;

namespace Ollama.Infrastructure.Tools
{
    public static class ToolInfoGenerator
    {
        public static string GenerateToolInformation(IToolRepository toolRepository)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("INTERNAL TOOLS (Session-Isolated, Reflection-Discovered):");
            sb.AppendLine("========================================================");
            sb.AppendLine();
            
            var tools = toolRepository.GetAllTools();
            
            if (!tools.Any())
            {
                sb.AppendLine("⚠️  WARNING: No tools are currently available!");
                sb.AppendLine("If you need tool capabilities, respond with tool: \"MISSING_TOOL\" and specify requirements.");
                sb.AppendLine();
                return sb.ToString();
            }
            
            foreach (var tool in tools)
            {
                var toolType = tool.GetType();
                var parameters = ExtractParametersFromTool(toolType);
                
                sb.AppendLine($"• {tool.Name}:");
                sb.AppendLine($"  - Purpose: {tool.Description}");
                sb.AppendLine($"  - When to use: {GetUsageGuidance(tool)}");
                sb.AppendLine($"  - Session isolation: {(tool.RequiresFileSystem ? "All operations within /cache/[sessionId]/" : "No file system access")}");
                sb.AppendLine($"  - Network required: {(tool.RequiresNetwork ? "Yes (session-contained downloads)" : "No")}");
                sb.AppendLine($"  - File system required: {(tool.RequiresFileSystem ? "Yes (session-limited)" : "No")}");
                
                if (parameters.Any())
                {
                    sb.AppendLine("  - Parameters:");
                    foreach (var param in parameters)
                    {
                        var sessionNote = GetSessionParameterNote(param.Name, tool);
                        sb.AppendLine($"    * {param.Name} ({param.Type}): {param.Description}{sessionNote}");
                    }
                }
                else
                {
                    sb.AppendLine("  - Parameters: None required");
                }
                
                if (tool.Capabilities.Any())
                {
                    var capabilities = tool.Capabilities.ToList();
                    var cursorCapabilities = capabilities.Where(c => c.StartsWith("cursor:")).ToList();
                    
                    if (cursorCapabilities.Any())
                    {
                        sb.AppendLine($"  - Capabilities: {string.Join(", ", capabilities)}");
                        sb.AppendLine($"  - Cursor Support: {GetCursorExplanation(cursorCapabilities)}");
                    }
                    else
                    {
                        sb.AppendLine($"  - Capabilities: {string.Join(", ", capabilities)}");
                    }
                }
                
                sb.AppendLine($"  - Usage: \"Use {tool.Name} tool with parameters to {GetUsageExample(tool)}\"");
                sb.AppendLine();
            }

            sb.AppendLine("MISSING TOOL REPORTING:");
            sb.AppendLine("=======================");
            sb.AppendLine("If current tools are insufficient for the task, respond with:");
            sb.AppendLine("- tool: \"MISSING_TOOL\"");
            sb.AppendLine("- parameters: { \"requiredToolName\": \"ToolName\", \"requiredCapabilities\": [\"cap1\", \"cap2\"], \"reason\": \"why needed\" }");
            sb.AppendLine("- Specify exact capabilities needed and session safety requirements");
            sb.AppendLine();

            return sb.ToString();
        }

        private static string GetSessionParameterNote(string paramName, ITool tool)
        {
            return paramName.ToLower() switch
            {
                "sessionid" => " (auto-provided by system)",
                "directorypath" or "filepath" or "workingdirectory" when tool.RequiresFileSystem 
                    => " (must be within session boundaries)",
                "repourl" => " (downloads to session directory)",
                _ => ""
            };
        }

        private static string GetUsageExample(ITool tool)
        {
            return tool.Name switch
            {
                "MathEvaluator" => "evaluate mathematical expressions",
                "GitHubRepositoryDownloader" => "download GitHub repositories to session",
                "FileSystemAnalyzer" => "analyze session directory structure",
                "CodeAnalyzer" => "analyze code files in session",
                "ExternalCommandExecutor" => "execute commands in session working directory",
                _ => "perform specialized operations"
            };
        }

        private static string GetCursorExplanation(List<string> cursorCapabilities)
        {
            var explanations = cursorCapabilities.Select(cap => cap switch
            {
                "cursor:navigate" => "Can change current directory (like 'cd' command)",
                "cursor:location" => "Shows current directory position (like 'pwd' command)",
                _ => $"Supports {cap} operations"
            });
            
            return string.Join(", ", explanations);
        }

        private static List<ParameterInfo> ExtractParametersFromTool(Type toolType)
        {
            var parameters = new List<ParameterInfo>();
            
            // Look for RunAsync method to understand parameter usage
            var runMethod = toolType.GetMethod("RunAsync");
            if (runMethod == null) return parameters;

            // Read the method body to find context.Parameters.TryGetValue calls
            // For now, we'll use a simple heuristic based on common parameter names
            var toolName = toolType.Name;
            
            // Define known parameters for each tool based on the code analysis
            var knownParameters = GetKnownParameters(toolName);
            parameters.AddRange(knownParameters);

            return parameters;
        }

        private static List<ParameterInfo> GetKnownParameters(string toolName)
        {
            return toolName switch
            {
                "MathEvaluator" => new List<ParameterInfo>
                {
                    new("expression", "string", "Mathematical expression to evaluate (e.g., '2 + 3 * 4')")
                },
                "GitHubRepositoryDownloader" => new List<ParameterInfo>
                {
                    new("repoUrl", "string", "GitHub repository URL (e.g., 'https://github.com/user/repo')")
                },
                "FileSystemAnalyzer" => new List<ParameterInfo>
                {
                    new("path", "string", "Path within session to analyze (defaults to session root if not provided)"),
                    new("includeSubdirectories", "boolean", "Whether to analyze subdirectories (default: true)"),
                    new("minimumFileSize", "number", "Minimum file size in bytes to include (optional)")
                },
                "CodeAnalyzer" => new List<ParameterInfo>
                {
                    new("analysisType", "string", "Type of analysis to perform (uses session file data)")
                },
                "ExternalCommandExecutor" => new List<ParameterInfo>
                {
                    new("command", "string", "Command to execute (e.g., 'dir', 'ls', 'echo hello')"),
                    new("workingDirectory", "string", "Working directory (optional, validated against session boundaries)"),
                    new("timeoutSeconds", "number", "Timeout in seconds (optional, default: 30)")
                },
                // Cursor Navigation Tools
                "CursorNavigation" => new List<ParameterInfo>
                {
                    new("path", "string", "Target directory to navigate to (relative to session root)"),
                    new("cd", "string", "Alternative parameter for directory navigation"),
                    new("showTree", "boolean", "Display directory tree structure (optional)"),
                    new("showFiles", "boolean", "Show files in addition to directories (optional)")
                },
                "PrintWorkingDirectory" => new List<ParameterInfo>
                {
                    new("showDetails", "boolean", "Show additional directory details (optional)"),
                    new("showTree", "boolean", "Display directory tree from current location (optional)")
                },
                // Directory Tools
                "DirectoryList" => new List<ParameterInfo>
                {
                    new("path", "string", "Directory path to list (defaults to current directory)"),
                    new("cd", "string", "Navigate to directory before listing"),
                    new("recursive", "boolean", "List subdirectories recursively (optional)"),
                    new("showHidden", "boolean", "Include hidden files and directories (optional)")
                },
                "DirectoryCreate" => new List<ParameterInfo>
                {
                    new("path", "string", "Directory path to create (required)"),
                    new("cd", "string", "Navigate to directory before creating (optional)")
                },
                "DirectoryDelete" => new List<ParameterInfo>
                {
                    new("path", "string", "Directory path to delete (required)"),
                    new("cd", "string", "Navigate to directory before deleting (optional)"),
                    new("recursive", "boolean", "Delete directory and all contents (optional)")
                },
                "DirectoryMove" => new List<ParameterInfo>
                {
                    new("source", "string", "Source directory path (required)"),
                    new("destination", "string", "Destination directory path (required)"),
                    new("cd", "string", "Navigate to directory before moving (optional)")
                },
                "DirectoryCopy" => new List<ParameterInfo>
                {
                    new("source", "string", "Source directory path (required)"),
                    new("destination", "string", "Destination directory path (required)"),
                    new("cd", "string", "Navigate to directory before copying (optional)"),
                    new("recursive", "boolean", "Copy subdirectories and contents (default: true)")
                },
                // File Tools
                "FileRead" => new List<ParameterInfo>
                {
                    new("path", "string", "File path to read (required)"),
                    new("cd", "string", "Navigate to directory before reading (optional)"),
                    new("encoding", "string", "Text encoding (optional, default: UTF-8)"),
                    new("showLineNumbers", "boolean", "Display line numbers (optional)")
                },
                "FileWrite" => new List<ParameterInfo>
                {
                    new("path", "string", "File path to write (required)"),
                    new("content", "string", "Content to write to file (required)"),
                    new("cd", "string", "Navigate to directory before writing (optional)"),
                    new("encoding", "string", "Text encoding (optional, default: UTF-8)"),
                    new("append", "boolean", "Append to existing file instead of overwriting (optional)")
                },
                "FileCopy" => new List<ParameterInfo>
                {
                    new("source", "string", "Source file path (required)"),
                    new("destination", "string", "Destination file path (required)"),
                    new("cd", "string", "Navigate to directory before copying (optional)"),
                    new("overwrite", "boolean", "Overwrite existing destination file (optional)")
                },
                "FileMove" => new List<ParameterInfo>
                {
                    new("source", "string", "Source file path (required)"),
                    new("destination", "string", "Destination file path (required)"),
                    new("cd", "string", "Navigate to directory before moving (optional)")
                },
                "FileDelete" => new List<ParameterInfo>
                {
                    new("path", "string", "File path to delete (required)"),
                    new("cd", "string", "Navigate to directory before deleting (optional)")
                },
                "FileAttributes" => new List<ParameterInfo>
                {
                    new("path", "string", "File path to examine/modify (required)"),
                    new("cd", "string", "Navigate to directory before operation (optional)"),
                    new("attributes", "string", "File attributes to set (optional, e.g., 'readonly', 'hidden')")
                },
                // Download Tools
                "Download" => new List<ParameterInfo>
                {
                    new("url", "string", "URL to download from (required)"),
                    new("destination", "string", "Destination path within session (optional)"),
                    new("cd", "string", "Navigate to directory before downloading (optional)"),
                    new("source", "string", "Download source type: github, gitlab, http, nuget, npm, pypi, maven, docker (auto-detected)"),
                    new("extract", "boolean", "Extract archives after download (optional)")
                },
                _ => new List<ParameterInfo>()
            };
        }

        private static string GetUsageGuidance(ITool tool)
        {
            return tool.Name switch
            {
                "MathEvaluator" => "For arithmetic calculations, solving mathematical expressions, or any numeric computations",
                "GitHubRepositoryDownloader" => "When user asks to analyze, download, or work with GitHub repositories (downloads to session)",
                "FileSystemAnalyzer" => "When user needs to understand session folder contents, file sizes, or directory structure",
                "CodeAnalyzer" => "When user wants to understand code content, structure, or functionality (works on session data)",
                "ExternalCommandExecutor" => "For system operations within session boundaries (working directory limited to session)",
                _ => "General purpose tool for various operations within session constraints"
            };
        }

        public record ParameterInfo(string Name, string Type, string Description);
    }
}
