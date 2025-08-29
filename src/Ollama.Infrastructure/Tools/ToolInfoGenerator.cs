using Ollama.Domain.Tools;
using Ollama.Domain.Tools.Attributes;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Ollama.Infrastructure.Tools
{
    /// <summary>
    /// Reflection-based tool information generator that discovers and documents all ITool implementations
    /// </summary>
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
                var toolInfo = ExtractToolInformation(tool);
                sb.AppendLine(FormatToolInformation(toolInfo));
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

        /// <summary>
        /// Extracts comprehensive tool information using reflection and attributes
        /// </summary>
        public static ToolInformation ExtractToolInformation(ITool tool)
        {
            var toolType = tool.GetType();
            var toolDescription = toolType.GetCustomAttribute<ToolDescriptionAttribute>();
            var toolUsage = toolType.GetCustomAttribute<ToolUsageAttribute>();
            var toolCapabilities = toolType.GetCustomAttribute<ToolCapabilitiesAttribute>();
            
            var info = new ToolInformation
            {
                Name = tool.Name,
                Type = toolType,
                Description = toolDescription?.Description ?? tool.Description,
                DetailedDescription = toolDescription?.Usage ?? "",
                Category = toolDescription?.Category ?? "Uncategorized",
                Version = "1.0.0", // Default version since not in attribute
                IsExperimental = false, // Default since not in attribute
                CreatedBy = "Unknown", // Default since not in attribute
                
                // Usage information
                PrimaryUseCase = toolUsage?.PrimaryUseCase ?? "General purpose operations",
                SecondaryUseCases = toolUsage?.SecondaryUseCases?.ToList() ?? new List<string>(),
                ExampleInvocation = toolUsage?.ExampleInvocation ?? $"Use {tool.Name} tool for operations",
                ExpectedOutput = toolUsage?.ExpectedOutput ?? "Operation result",
                SafetyNotes = toolUsage?.SafetyNotes ?? "",
                PerformanceNotes = toolUsage?.PerformanceNotes ?? "",
                
                // System requirements
                RequiresNetwork = toolUsage?.RequiresNetwork ?? tool.RequiresNetwork,
                RequiresFileSystem = toolUsage?.RequiresFileSystem ?? tool.RequiresFileSystem,
                RequiresElevatedPrivileges = toolUsage?.RequiresElevatedPrivileges ?? false,
                
                // Capabilities
                Capabilities = ExtractCapabilities(tool, toolCapabilities),
                FallbackStrategy = toolCapabilities?.FallbackStrategy ?? "No fallback available",
                
                // Parameters
                Parameters = ExtractParameters(toolType, toolUsage),
                
                // Legacy capabilities for backward compatibility
                LegacyCapabilities = tool.Capabilities?.ToList() ?? new List<string>()
            };
            
            return info;
        }

        /// <summary>
        /// Extracts parameter information using reflection on the RunAsync method and usage attributes
        /// </summary>
        private static List<ParameterInformation> ExtractParameters(Type toolType, ToolUsageAttribute? toolUsage)
        {
            var parameters = new List<ParameterInformation>();
            
            // First, add parameters from usage attributes if available
            if (toolUsage != null)
            {
                foreach (var param in toolUsage.RequiredParameters ?? Array.Empty<string>())
                {
                    parameters.Add(new ParameterInformation
                    {
                        Name = param,
                        Type = "string",
                        Description = $"Required parameter: {param}",
                        IsRequired = true,
                        DefaultValue = null
                    });
                }
                
                foreach (var param in toolUsage.OptionalParameters ?? Array.Empty<string>())
                {
                    parameters.Add(new ParameterInformation
                    {
                        Name = param,
                        Type = "string", 
                        Description = $"Optional parameter: {param}",
                        IsRequired = false,
                        DefaultValue = null
                    });
                }
            }
            
            // If no parameters found in attributes, try to extract from method signatures or tool analysis
            if (!parameters.Any())
            {
                parameters.AddRange(AnalyzeToolForParameters(toolType));
            }
            
            return parameters;
        }

        /// <summary>
        /// Analyzes tool implementation to infer parameters from common patterns
        /// </summary>
        private static List<ParameterInformation> AnalyzeToolForParameters(Type toolType)
        {
            var parameters = new List<ParameterInformation>();
            
            // Try to analyze the RunAsync method for parameter usage patterns
            var runMethod = toolType.GetMethod("RunAsync", BindingFlags.Public | BindingFlags.Instance);
            if (runMethod != null)
            {
                // Look for parameter patterns in the method body (this is limited without source analysis)
                // For now, we'll use tool name-based inference as a fallback
                parameters.AddRange(InferParametersFromToolName(toolType.Name));
            }
            
            return parameters;
        }

        /// <summary>
        /// Infers common parameters based on tool naming conventions and types
        /// </summary>
        private static List<ParameterInformation> InferParametersFromToolName(string toolName)
        {
            var parameters = new List<ParameterInformation>();
            
            // File operation tools
            if (toolName.Contains("File"))
            {
                parameters.Add(new ParameterInformation { Name = "path", Type = "string", Description = "File path", IsRequired = true });
                
                if (toolName.Contains("Copy") || toolName.Contains("Move"))
                {
                    parameters.Add(new ParameterInformation { Name = "destination", Type = "string", Description = "Destination path", IsRequired = true });
                }
                
                if (toolName.Contains("Write"))
                {
                    parameters.Add(new ParameterInformation { Name = "content", Type = "string", Description = "Content to write", IsRequired = true });
                }
            }
            
            // Directory operation tools
            if (toolName.Contains("Directory"))
            {
                parameters.Add(new ParameterInformation { Name = "path", Type = "string", Description = "Directory path", IsRequired = true });
                
                if (toolName.Contains("Copy") || toolName.Contains("Move"))
                {
                    parameters.Add(new ParameterInformation { Name = "destination", Type = "string", Description = "Destination path", IsRequired = true });
                }
            }
            
            // Download tools
            if (toolName.Contains("Download") || toolName.Contains("GitHub"))
            {
                parameters.Add(new ParameterInformation { Name = "url", Type = "string", Description = "URL to download", IsRequired = true });
            }
            
            // Math tools
            if (toolName.Contains("Math"))
            {
                parameters.Add(new ParameterInformation { Name = "expression", Type = "string", Description = "Mathematical expression", IsRequired = true });
            }
            
            // Analysis tools
            if (toolName.Contains("Analyzer"))
            {
                parameters.Add(new ParameterInformation { Name = "path", Type = "string", Description = "Path to analyze", IsRequired = false });
                parameters.Add(new ParameterInformation { Name = "includeSubdirectories", Type = "boolean", Description = "Include subdirectories", IsRequired = false });
            }
            
            // Command execution tools
            if (toolName.Contains("Command") || toolName.Contains("Executor"))
            {
                parameters.Add(new ParameterInformation { Name = "command", Type = "string", Description = "Command to execute", IsRequired = true });
                parameters.Add(new ParameterInformation { Name = "workingDirectory", Type = "string", Description = "Working directory", IsRequired = false });
            }
            
            // Navigation tools
            if (toolName.Contains("Navigation") || toolName.Contains("Cursor"))
            {
                parameters.Add(new ParameterInformation { Name = "path", Type = "string", Description = "Target path", IsRequired = false });
            }
            
            // Common optional parameters for most tools
            if (parameters.Any(p => p.Name == "path"))
            {
                parameters.Add(new ParameterInformation { Name = "cd", Type = "string", Description = "Change directory before operation", IsRequired = false });
            }
            
            return parameters;
        }

        /// <summary>
        /// Extracts capabilities from attributes and legacy properties
        /// </summary>
        private static List<string> ExtractCapabilities(ITool tool, ToolCapabilitiesAttribute? toolCapabilities)
        {
            var capabilities = new List<string>();
            
            if (toolCapabilities != null)
            {
                // Extract from enum flags
                var capabilityFlags = Enum.GetValues<ToolCapability>()
                    .Where(cap => cap != ToolCapability.None && toolCapabilities.Capabilities.HasFlag(cap))
                    .Select(cap => cap.ToString())
                    .ToList();
                
                capabilities.AddRange(capabilityFlags);
            }
            
            // Add legacy capabilities as fallback
            if (tool.Capabilities != null)
            {
                capabilities.AddRange(tool.Capabilities);
            }
            
            return capabilities.Distinct().ToList();
        }

        /// <summary>
        /// Formats tool information for display in system prompts
        /// </summary>
        private static string FormatToolInformation(ToolInformation info)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"• {info.Name}:");
            sb.AppendLine($"  - Purpose: {info.Description}");
            
            if (!string.IsNullOrEmpty(info.DetailedDescription) && info.DetailedDescription != info.Description)
            {
                sb.AppendLine($"  - Details: {info.DetailedDescription}");
            }
            
            sb.AppendLine($"  - Category: {info.Category}");
            sb.AppendLine($"  - When to use: {info.PrimaryUseCase}");
            
            if (info.SecondaryUseCases.Any())
            {
                sb.AppendLine($"  - Also for: {string.Join(", ", info.SecondaryUseCases)}");
            }
            
            sb.AppendLine($"  - Session isolation: {(info.RequiresFileSystem ? "All operations within /cache/[sessionId]/" : "No file system access")}");
            sb.AppendLine($"  - Network required: {(info.RequiresNetwork ? "Yes" : "No")}");
            sb.AppendLine($"  - File system required: {(info.RequiresFileSystem ? "Yes (session-limited)" : "No")}");
            
            if (info.RequiresElevatedPrivileges)
            {
                sb.AppendLine("  - Privileges: Elevated privileges required");
            }
            
            if (!string.IsNullOrEmpty(info.SafetyNotes))
            {
                sb.AppendLine($"  - Safety notes: {info.SafetyNotes}");
            }
            
            if (!string.IsNullOrEmpty(info.PerformanceNotes))
            {
                sb.AppendLine($"  - Performance notes: {info.PerformanceNotes}");
            }
            
            if (info.Capabilities.Any())
            {
                sb.AppendLine($"  - Capabilities: {string.Join(", ", info.Capabilities)}");
            }
            
            if (!string.IsNullOrEmpty(info.FallbackStrategy) && info.FallbackStrategy != "No fallback available")
            {
                sb.AppendLine($"  - Fallback strategy: {info.FallbackStrategy}");
            }
            
            if (info.IsExperimental)
            {
                sb.AppendLine("  - Status: ⚠️ Experimental (may have limited stability)");
            }
            
            // Format parameters in the requested format: parameters:[{name:[string]}, ...]
            if (info.Parameters.Any())
            {
                sb.AppendLine("  - Parameters:");
                var parameterList = info.Parameters.Select(p => $"{{name:\"{p.Name}\", type:\"{p.Type}\", required:{p.IsRequired.ToString().ToLower()}, description:\"{p.Description}\"}}");
                sb.AppendLine($"    [{string.Join(", ", parameterList)}]");
                
                // Also show detailed parameter descriptions
                foreach (var param in info.Parameters)
                {
                    var sessionNote = GetSessionParameterNote(param.Name, info);
                    sb.AppendLine($"    * {param.Name} ({param.Type}): {param.Description}{sessionNote}");
                }
            }
            else
            {
                sb.AppendLine("  - Parameters: []");
            }
            
            sb.AppendLine($"  - Usage: \"{info.ExampleInvocation}\"");
            
            if (!string.IsNullOrEmpty(info.ExpectedOutput))
            {
                sb.AppendLine($"  - Expected output: {info.ExpectedOutput}");
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Gets session-specific notes for parameters
        /// </summary>
        private static string GetSessionParameterNote(string paramName, ToolInformation info)
        {
            return paramName.ToLower() switch
            {
                "sessionid" => " (auto-provided by system)",
                "directorypath" or "filepath" or "workingdirectory" when info.RequiresFileSystem 
                    => " (must be within session boundaries)",
                "repourl" or "url" when info.RequiresNetwork => " (downloads to session directory)",
                _ => ""
            };
        }
    }

    /// <summary>
    /// Comprehensive tool information extracted via reflection
    /// </summary>
    public class ToolInformation
    {
        public string Name { get; set; } = "";
        public Type Type { get; set; } = typeof(object);
        public string Description { get; set; } = "";
        public string DetailedDescription { get; set; } = "";
        public string Category { get; set; } = "";
        public string Version { get; set; } = "";
        public bool IsExperimental { get; set; }
        public string CreatedBy { get; set; } = "";
        
        public string PrimaryUseCase { get; set; } = "";
        public List<string> SecondaryUseCases { get; set; } = new();
        public string ExampleInvocation { get; set; } = "";
        public string ExpectedOutput { get; set; } = "";
        public string SafetyNotes { get; set; } = "";
        public string PerformanceNotes { get; set; } = "";
        
        public bool RequiresNetwork { get; set; }
        public bool RequiresFileSystem { get; set; }
        public bool RequiresElevatedPrivileges { get; set; }
        
        public List<string> Capabilities { get; set; } = new();
        public List<string> LegacyCapabilities { get; set; } = new();
        public string FallbackStrategy { get; set; } = "";
        
        public List<ParameterInformation> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Parameter information extracted via reflection
    /// </summary>
    public class ParameterInformation
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
    }
}
