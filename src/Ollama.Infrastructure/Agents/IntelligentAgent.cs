using Ollama.Domain.Agents;
using Ollama.Domain.Tools;
using Ollama.Infrastructure.Tools;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace Ollama.Infrastructure.Agents
{
    public class IntelligentAgent : IAgent
    {
        private readonly IToolRepository _toolRepository;
        private readonly ILogger<IntelligentAgent> _logger;

        public IntelligentAgent(IToolRepository toolRepository, ILogger<IntelligentAgent> logger)
        {
            _toolRepository = toolRepository;
            _logger = logger;
        }

        public string Think(string query)
        {
            _logger.LogInformation("IntelligentAgent thinking about query: {Query}", query);

            var queryType = ClassifyQuery(query);
            
            return queryType switch
            {
                QueryType.SimpleArithmetic => "This appears to be a simple arithmetic question. I can evaluate this directly using mathematical operations.",
                QueryType.RepositoryAnalysis => "This looks like a request to analyze a code repository. I'll need to download the repository, analyze its file structure, and examine the code for potential improvements.",
                _ => "This is a general query that I'll process using my knowledge base."
            };
        }

        public object Plan(string query)
        {
            var queryType = ClassifyQuery(query);
            
            return queryType switch
            {
                QueryType.SimpleArithmetic => "1. Extract the mathematical expression\n2. Use MathEvaluator tool to compute the result\n3. Return the answer",
                QueryType.RepositoryAnalysis => "1. Extract repository URL from query\n2. Use GitHubDownloader to fetch the repository\n3. Use FileSystemAnalyzer to examine structure\n4. Use CodeAnalyzer to identify code patterns\n5. Generate improvement suggestions\n6. Create comprehensive report",
                _ => "1. Process query using general knowledge\n2. Provide informative response"
            };
        }

        public string Answer(string query)
        {
            var queryType = ClassifyQuery(query);
            
            return queryType switch
            {
                QueryType.SimpleArithmetic => HandleArithmeticAsync(query).Result,
                QueryType.RepositoryAnalysis => HandleRepositoryAnalysisAsync(query).Result,
                _ => HandleGeneralQuery(query)
            };
        }

        public object Act(string instruction)
        {
            // For this implementation, Act simply delegates to Answer
            // In a more sophisticated implementation, Act could perform actual actions
            return Answer(instruction);
        }

        private QueryType ClassifyQuery(string query)
        {
            query = query.ToLowerInvariant().Trim();

            if (query.Contains("repository") || 
                query.Contains("repo") || 
                query.Contains("code") || 
                query.Contains("analyze") ||
                query.Contains("github.com") ||
                query.Contains("improvements"))
            {
                return QueryType.RepositoryAnalysis;
            }
            else if (ContainsArithmeticOperation(query))
            {
                return QueryType.SimpleArithmetic;
            }

            return QueryType.General;
        }

        private bool ContainsArithmeticOperation(string query)
        {
            return Regex.IsMatch(query, @"\d+\s*[\+\-\*\/]\s*\d+|what\s+is\s+\d+\s*[\+\-\*\/]\s*\d+");
        }

        private async Task<string> HandleArithmeticAsync(string query)
        {
            try
            {
                var mathExpression = ExtractMathExpression(query);
                
                var mathTool = _toolRepository.GetToolByName("MathEvaluator");
                if (mathTool == null)
                {
                    return "Math evaluation tool is not available.";
                }

                var context = new ToolContext();
                context.Parameters["expression"] = mathExpression;

                var result = await mathTool.RunAsync(context);
                
                if (result.Success)
                {
                    return $"The result of {mathExpression} is {result.Output}";
                }
                else
                {
                    return $"Error calculating {mathExpression}: {result.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating arithmetic expression");
                return "I encountered an error while calculating that expression.";
            }
        }

        private string ExtractMathExpression(string query)
        {
            var match = Regex.Match(query, @"(\d+\s*[\+\-\*\/]\s*\d+)");
                
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            
            match = Regex.Match(query, @"what\s+is\s+(\d+\s*[\+\-\*\/]\s*\d+)");
                
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            
            return query.Replace("what is", "").Replace("calculate", "").Trim();
        }

        private async Task<string> HandleRepositoryAnalysisAsync(string query)
        {
            _logger.LogInformation("Analyzing repository from query: {Query}", query);

            var repoUrl = ExtractRepositoryUrl(query);
            if (string.IsNullOrEmpty(repoUrl))
            {
                return "I couldn't identify a valid GitHub repository URL in your query. " +
                       "Please provide a query with a GitHub repository URL.";
            }

            try
            {
                var toolContext = new ToolContext
                {
                    WorkingDirectory = Path.GetTempPath()
                };
                
                toolContext.Parameters["repoUrl"] = repoUrl;
                
                // Step 1: Download repository
                var downloader = _toolRepository.GetToolByName("GitHubDownloader");
                if (downloader == null)
                {
                    return "Repository download tools are not available.";
                }
                
                var downloadResult = await downloader.RunAsync(toolContext);
                if (!downloadResult.Success)
                {
                    return $"Failed to download repository: {downloadResult.ErrorMessage}";
                }
                
                // Step 2: Analyze file system
                var fsAnalyzer = _toolRepository.GetToolByName("FileSystemAnalyzer");
                if (fsAnalyzer == null)
                {
                    return "File system analysis tools are not available.";
                }
                
                var fsAnalysisResult = await fsAnalyzer.RunAsync(toolContext);
                if (!fsAnalysisResult.Success)
                {
                    return $"Failed to analyze repository structure: {fsAnalysisResult.ErrorMessage}";
                }
                
                // Step 3: Analyze code
                var codeAnalyzer = _toolRepository.GetToolByName("CodeAnalyzer");
                if (codeAnalyzer == null)
                {
                    return "Code analysis tools are not available.";
                }
                
                var codeAnalysisResult = await codeAnalyzer.RunAsync(toolContext);
                if (!codeAnalysisResult.Success)
                {
                    return $"Failed to analyze code: {codeAnalysisResult.ErrorMessage}";
                }
                
                // Step 4: Generate report
                var fsStats = toolContext.State["fileStats"] as FileSystemStats;
                var codeAnalysis = toolContext.State["codeAnalysis"] as CodeAnalysisResult;
                
                var suggestions = GenerateImprovementSuggestions(fsStats!, codeAnalysis!);
                
                return BuildAnalysisReport(repoUrl, fsStats!, codeAnalysis!, suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during repository analysis");
                return $"An error occurred during repository analysis: {ex.Message}";
            }
        }

        private string? ExtractRepositoryUrl(string query)
        {
            var match = Regex.Match(query, @"(https?://github\.com/[a-zA-Z0-9\-_]+/[a-zA-Z0-9\-_]+)");
                
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            
            return null;
        }

        private List<string> GenerateImprovementSuggestions(FileSystemStats fsStats, CodeAnalysisResult codeAnalysis)
        {
            var suggestions = new List<string>();
            
            if (codeAnalysis.ProjectComplexity == "High")
            {
                suggestions.Add("Consider breaking down the project into smaller, more manageable modules");
            }
            
            if (codeAnalysis.CodeToNonCodeRatio > 0.5)
            {
                suggestions.Add("High ratio of non-code to code files suggests reviewing documentation or reducing build artifacts");
            }
            
            var codeSmellCount = codeAnalysis.AnalyzedFiles.Sum(f => f.CodeSmells.Count);
            if (codeSmellCount > 10)
            {
                suggestions.Add("High number of code smells detected. Consider code cleanup and refactoring");
            }
            
            var hasReadme = fsStats.FileSamples.Any(f => f.Name.ToLowerInvariant() == "readme.md");
            if (!hasReadme)
            {
                suggestions.Add("Add a README.md file to improve project documentation");
            }
            
            var hasGitIgnore = fsStats.FileSamples.Any(f => f.Name.ToLowerInvariant() == ".gitignore");
            if (!hasGitIgnore)
            {
                suggestions.Add("Add a .gitignore file to prevent committing unnecessary files");
            }

            if (suggestions.Count == 0)
            {
                suggestions.Add("Repository structure looks good. Consider adding more tests for code reliability.");
                suggestions.Add("Consider implementing continuous integration for automated testing.");
                suggestions.Add("Review dependencies for security vulnerabilities and updates.");
            }
            
            return suggestions;
        }

        private string BuildAnalysisReport(string repoUrl, FileSystemStats fsStats, CodeAnalysisResult codeAnalysis, List<string> suggestions)
        {
            var report = new StringBuilder();
            report.AppendLine($"# Repository Analysis: {repoUrl}");
            report.AppendLine();
            report.AppendLine("## Repository Structure");
            report.AppendLine($"- Project Type: {codeAnalysis.ProjectType}");
            report.AppendLine($"- Total Files: {fsStats.TotalFiles}");
            report.AppendLine($"- Total Directories: {fsStats.TotalDirectories}");
            report.AppendLine($"- Code Files: {codeAnalysis.TotalCodeFiles}");
            report.AppendLine($"- Lines of Code: {codeAnalysis.TotalLinesOfCode}");
            report.AppendLine($"- Project Complexity: {codeAnalysis.ProjectComplexity}");
            report.AppendLine();
            report.AppendLine("## File Types");
            
            foreach (var fileType in fsStats.FileTypeDistribution.OrderByDescending(ft => ft.Value))
            {
                report.AppendLine($"- {fileType.Key}: {fileType.Value} files");
            }
            
            report.AppendLine();
            report.AppendLine("## Code Quality Issues");
            var totalCodeSmells = codeAnalysis.AnalyzedFiles.Sum(f => f.CodeSmells.Count);
            if (totalCodeSmells > 0)
            {
                report.AppendLine($"Found {totalCodeSmells} potential code issues:");
                foreach (var file in codeAnalysis.AnalyzedFiles.Where(f => f.CodeSmells.Count > 0))
                {
                    report.AppendLine($"- {file.FileName}: {file.CodeSmells.Count} issues");
                }
            }
            else
            {
                report.AppendLine("No obvious code quality issues detected in analyzed files.");
            }
            
            report.AppendLine();
            report.AppendLine("## Improvement Suggestions");
            foreach (var suggestion in suggestions)
            {
                report.AppendLine($"- {suggestion}");
            }
            
            return report.ToString();
        }

        private string HandleGeneralQuery(string query)
        {
            return "I'm an intelligent agent that can handle arithmetic calculations and repository analysis. " +
                   "For other types of queries, I would need integration with a language model.";
        }

        private enum QueryType
        {
            SimpleArithmetic,
            RepositoryAnalysis,
            General
        }
    }
}
