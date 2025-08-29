using Ollama.Domain.Tools;
using Ollama.Domain.Tools.Attributes;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Ollama.Infrastructure.Tools
{
    [ToolDescription(
        "Analyzes code files for structure, patterns, and potential improvements",
        "Comprehensive code analysis tool that examines file structure, identifies patterns, suggests improvements, and provides quality metrics. Analyzes syntax, complexity, and adherence to coding standards.",
        "Code Analysis")]
    [ToolUsage(
        "Analyze code files for quality, structure, and improvement opportunities",
        SecondaryUseCases = new[] { "Code quality assessment", "Pattern recognition", "Syntax analysis", "Complexity measurement" },
        RequiredParameters = new[] { "path" },
        OptionalParameters = new[] { "cd", "includeMetrics", "analysisDepth" },
        ExampleInvocation = "CodeAnalyzer with path=\"MyProject.cs\" to analyze code structure",
        ExpectedOutput = "Detailed analysis report with metrics, patterns, and suggestions",
        RequiresFileSystem = true,
        RequiresNetwork = false,
        SafetyNotes = "Read-only analysis within session boundaries",
        PerformanceNotes = "Analysis time depends on file size and complexity")]
    [ToolCapabilities(
        ToolCapability.CodeAnalysis | ToolCapability.FileRead | ToolCapability.TextAnalysis,
        FallbackStrategy = "Basic syntax analysis if advanced metrics fail")]
    public class CodeAnalyzer : AbstractTool
    {
        public override string Name => "CodeAnalyzer";
        public override string Description => "Analyzes code files for structure, patterns, and potential improvements";
        public override IEnumerable<string> Capabilities => new[] { "code:analyze", "code:quality", "code:pattern" };
        public override bool RequiresNetwork => false;
        public override bool RequiresFileSystem => true;

        public CodeAnalyzer(ISessionScope sessionScope, ILogger<CodeAnalyzer> logger) 
            : base(sessionScope, logger)
        {
        }

        public override Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(context.State.ContainsKey("fileStats"));
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.5m); // Small cost for analysis
        }

        public override async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            if (!context.State.TryGetValue("fileStats", out var fileStatsObj) || 
                !(fileStatsObj is FileSystemStats fileStats))
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = "No file system stats available for analysis",
                    ExecutionTime = DateTime.Now - startTime
                };
            }

            try
            {
                var analysis = await Task.Run(() => AnalyzeCode(fileStats), cancellationToken);
                
                // Save analysis in state for other tools
                context.State["codeAnalysis"] = analysis;
                
                return new ToolResult
                {
                    Success = true,
                    Output = analysis,
                    ExecutionTime = DateTime.Now - startTime
                };
            }
            catch (Exception ex)
            {
                return new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Error analyzing code: {ex.Message}",
                    ExecutionTime = DateTime.Now - startTime
                };
            }
        }

        private CodeAnalysisResult AnalyzeCode(FileSystemStats fileStats)
        {
            var result = new CodeAnalysisResult();
            
            // Determine project type based on file extensions
            result.ProjectType = DetermineProjectType(fileStats.FileTypeDistribution);
            
            // Analyze code samples
            foreach (var fileSample in fileStats.FileSamples)
            {
                var codeFile = new CodeFile
                {
                    FileName = fileSample.Name,
                    FileExtension = fileSample.Extension,
                    FileSize = fileSample.Size
                };
                
                // Simple code smells detection
                codeFile.CodeSmells = DetectCodeSmells(fileSample.Preview, fileSample.Extension);
                
                // Count lines of code (approximation)
                codeFile.LinesOfCode = CountLines(fileSample.Preview);
                
                result.AnalyzedFiles.Add(codeFile);
            }
            
            // Calculate code statistics
            CalculateCodeMetrics(result, fileStats);
            
            return result;
        }

        private string DetermineProjectType(Dictionary<string, int> fileTypeDistribution)
        {
            // Simple heuristic to determine project type
            if (fileTypeDistribution.TryGetValue(".cs", out var csCount) && csCount > 0)
            {
                return ".NET/C#";
            }
            else if (fileTypeDistribution.TryGetValue(".java", out var javaCount) && javaCount > 0)
            {
                return "Java";
            }
            else if (fileTypeDistribution.TryGetValue(".js", out var jsCount) && jsCount > 0)
            {
                if (fileTypeDistribution.TryGetValue(".tsx", out var tsxCount) && tsxCount > 0)
                {
                    return "TypeScript/React";
                }
                else if (fileTypeDistribution.TryGetValue(".ts", out var tsCount) && tsCount > 0)
                {
                    return "TypeScript";
                }
                return "JavaScript";
            }
            else if (fileTypeDistribution.TryGetValue(".py", out var pyCount) && pyCount > 0)
            {
                return "Python";
            }
            else if (fileTypeDistribution.TryGetValue(".go", out var goCount) && goCount > 0)
            {
                return "Go";
            }
            else if (fileTypeDistribution.TryGetValue(".rb", out var rbCount) && rbCount > 0)
            {
                return "Ruby";
            }
            else if (fileTypeDistribution.TryGetValue(".php", out var phpCount) && phpCount > 0)
            {
                return "PHP";
            }
            
            return "Unknown";
        }

        private List<CodeSmell> DetectCodeSmells(string code, string extension)
        {
            var codeSmells = new List<CodeSmell>();
            
            // Check for long lines
            var lines = code.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length > 120)
                {
                    codeSmells.Add(new CodeSmell
                    {
                        Type = "Long Line",
                        Severity = "Low",
                        LineNumber = i + 1,
                        Description = $"Line is {lines[i].Length} characters long (recommended max: 120)"
                    });
                }
            }
            
            // Check for magic numbers
            if (extension == ".cs" || extension == ".java" || extension == ".js" || extension == ".ts")
            {
                var magicNumberRegex = new Regex(@"[^0-9a-zA-Z_]\d+[^0-9a-zA-Z_]");
                var matches = magicNumberRegex.Matches(code);
                foreach (Match match in matches)
                {
                    // Skip common numbers like 0, 1, -1
                    if (match.Value == "0" || match.Value == "1" || match.Value == "-1")
                        continue;
                    
                    codeSmells.Add(new CodeSmell
                    {
                        Type = "Magic Number",
                        Severity = "Medium",
                        LineNumber = GetLineNumber(code, match.Index),
                        Description = $"Magic number: {match.Value}"
                    });
                }
            }
            
            return codeSmells;
        }

        private int CountLines(string code)
        {
            if (string.IsNullOrEmpty(code))
                return 0;
            
            return code.Split('\n').Length;
        }

        private int GetLineNumber(string code, int position)
        {
            var lineCount = 1;
            for (int i = 0; i < position && i < code.Length; i++)
            {
                if (code[i] == '\n')
                    lineCount++;
            }
            return lineCount;
        }

        private void CalculateCodeMetrics(CodeAnalysisResult result, FileSystemStats fileStats)
        {
            // Calculate metrics based on file statistics
            result.TotalCodeFiles = fileStats.FileTypeDistribution
                .Where(kv => IsCodeFile(kv.Key))
                .Sum(kv => kv.Value);
            
            result.TotalLinesOfCode = result.AnalyzedFiles.Sum(f => f.LinesOfCode);
            
            // Estimate complexity based on file count and size
            if (result.TotalCodeFiles < 10)
                result.ProjectComplexity = "Low";
            else if (result.TotalCodeFiles < 50)
                result.ProjectComplexity = "Medium";
            else
                result.ProjectComplexity = "High";
            
            // Calculate code to non-code ratio
            var nonCodeFiles = fileStats.TotalFiles - result.TotalCodeFiles;
            result.CodeToNonCodeRatio = result.TotalCodeFiles > 0 ? 
                (double)nonCodeFiles / result.TotalCodeFiles : 0;
        }

        private bool IsCodeFile(string extension)
        {
            var codeExtensions = new[]
            {
                ".cs", ".java", ".js", ".ts", ".py", ".go", ".rb", ".php", ".c", ".cpp", ".h", 
                ".hpp", ".jsx", ".tsx", ".vue", ".scala", ".kt", ".kts", ".swift", ".m", ".rs"
            };
            
            return codeExtensions.Contains(extension.ToLowerInvariant());
        }
    }

    public class CodeAnalysisResult
    {
        public string ProjectType { get; set; } = string.Empty;
        public int TotalCodeFiles { get; set; }
        public int TotalLinesOfCode { get; set; }
        public string ProjectComplexity { get; set; } = string.Empty;
        public double CodeToNonCodeRatio { get; set; }
        public List<CodeFile> AnalyzedFiles { get; set; } = new List<CodeFile>();
        public List<string> SuggestedImprovements { get; set; } = new List<string>();
    }

    public class CodeFile
    {
        public string FileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int LinesOfCode { get; set; }
        public List<CodeSmell> CodeSmells { get; set; } = new List<CodeSmell>();
    }

    public class CodeSmell
    {
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
