using System.Text.Json;
using Ollama.Domain.Models.Communication;

namespace Ollama.Interface.Cli;

/// <summary>
/// Simple demo showing the schema-based communication
/// </summary>
public static class SchemaDemo
{
    public static void DemonstrateSchemas()
    {
        Console.WriteLine("=== LLM Communication Schema Demo ===\n");

        // Create a sample request schema
        var requestSchema = new LLMRequestSchema
        {
            SessionId = "demo-session-001",
            UserQuery = "analyze the GitHub repository https://github.com/microsoft/vscode and provide executable steps to understand its structure",
            Context = new ConversationContext
            {
                CurrentStep = 1,
                WorkingDirectory = "cache/demo-session-001",
                SessionStartTime = DateTime.UtcNow
            },
            Strategy = new StrategyConfiguration
            {
                Name = "Pessimistic",
                RiskLevel = "low",
                RequireConfirmation = true,
                MaxStepsPerResponse = 1,
                AnalysisDepth = "thorough"
            },
            AvailableTools = new List<ToolDescription>
            {
                new ToolDescription
                {
                    Name = "GitHubRepositoryDownloader",
                    Description = "Downloads and analyzes GitHub repositories",
                    Parameters = new Dictionary<string, object>
                    {
                        { "repositoryUrl", "string" },
                        { "targetDirectory", "string" }
                    },
                    Examples = new List<string> { "https://github.com/user/repo" }
                },
                new ToolDescription
                {
                    Name = "FileSystemAnalyzer", 
                    Description = "Analyzes local file system structure and content",
                    Parameters = new Dictionary<string, object>
                    {
                        { "directoryPath", "string" },
                        { "includeSubdirectories", "boolean" }
                    },
                    Examples = new List<string> { "/path/to/directory" }
                }
            },
            Constraints = new RequestConstraints
            {
                ResponseFormat = "json",
                RequireExecutableSteps = true,
                MaxResponseTokens = 2000
            }
        };

        // Serialize the request
        var requestJson = JsonSerializer.Serialize(requestSchema, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Console.WriteLine("📤 Request Schema (TO LLM):");
        Console.WriteLine(requestJson);
        Console.WriteLine("\n" + new string('=', 80) + "\n");

        // Create a sample response schema  
        var responseSchema = new LLMResponseSchema
        {
            SessionId = "demo-session-001",
            Status = new ResponseStatus
            {
                Success = true,
                StatusCode = "OK",
                Message = "Analysis completed successfully"
            },
            Analysis = new AnalysisResult
            {
                Summary = "The Microsoft VSCode repository is a large TypeScript-based project with extensive tooling and extension ecosystem.",
                KeyFindings = new List<string>
                {
                    "Main language: TypeScript (73.2%)",
                    "Build system: npm with custom scripts",
                    "Modular extension architecture",
                    "Extensive testing infrastructure"
                },
                RiskAssessment = new RiskAssessment
                {
                    RiskLevel = "low",
                    IdentifiedRisks = new List<string>
                    {
                        "Large codebase may take time to download",
                        "Complex build process may require specific Node.js version"
                    },
                    MitigationStrategies = new List<string>
                    {
                        "Download to dedicated directory",
                        "Check system requirements first"
                    }
                },
                Recommendations = new List<string>
                {
                    "Start with directory structure analysis",
                    "Focus on src/ directory for core functionality",
                    "Review package.json for build scripts"
                }
            },
            NextStep = new ExecutableStep
            {
                StepNumber = 1,
                Action = "Download the VSCode repository to analyze its structure",
                ToolName = "GitHubRepositoryDownloader",
                Parameters = new Dictionary<string, object>
                {
                    { "repositoryUrl", "https://github.com/microsoft/vscode" },
                    { "targetDirectory", "cache/demo-session-001/vscode" }
                },
                ExpectedOutcome = "Repository downloaded locally for detailed analysis",
                ValidationCriteria = new List<string>
                {
                    "Directory exists and contains .git folder",
                    "package.json file is present",
                    "src/ directory structure is accessible"
                },
                EstimatedDurationSeconds = 180
            },
            Reasoning = new ReasoningProcess
            {
                Approach = "Conservative step-by-step analysis starting with repository acquisition",
                AlternativesConsidered = new List<string>
                {
                    "Direct GitHub API analysis without download",
                    "Clone only specific branches",
                    "Use GitHub's download ZIP feature"
                },
                DecisionFactors = new List<string>
                {
                    "Need local access for detailed file analysis",
                    "Full git history provides valuable context",
                    "Pessimistic strategy requires thorough approach"
                }
            },
            Confidence = new ConfidenceMetrics
            {
                OverallConfidence = 0.85,
                AnalysisConfidence = 0.90,
                ActionConfidence = 0.80,
                ConfidenceJustification = "VSCode is well-documented public repository with standard structure"
            },
            Continuation = new ContinuationInfo
            {
                RequiresUserConfirmation = true,
                NextExpectedInput = "User confirmation to proceed with download, or request for modification",
                IsComplete = false,
                ProgressPercentage = 15,
                EstimatedRemainingSteps = 4
            },
            Metadata = new ResponseMetadata
            {
                ResponseTime = DateTime.UtcNow,
                ModelUsed = "qwen2.5:7b-instruct-q4_K_M",
                ProcessingDurationMs = 2350,
                StrategyApplied = "Pessimistic"
            }
        };

        // Serialize the response
        var responseJson = JsonSerializer.Serialize(responseSchema, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Console.WriteLine("📥 Response Schema (FROM LLM):");
        Console.WriteLine(responseJson);
        Console.WriteLine("\n" + new string('=', 80) + "\n");

        Console.WriteLine("✅ Key Benefits of Schema-Based Communication:");
        Console.WriteLine("🔹 Consistent structure for reliable parsing");
        Console.WriteLine("🔹 Rich metadata for better decision making"); 
        Console.WriteLine("🔹 Executable steps with validation criteria");
        Console.WriteLine("🔹 Confidence metrics for risk assessment");
        Console.WriteLine("🔹 Continuation info for multi-step workflows");
        Console.WriteLine("🔹 Detailed reasoning for transparency");
        Console.WriteLine("\n🧠 LLM is the brain - schemas provide the communication protocol!");
    }
}
