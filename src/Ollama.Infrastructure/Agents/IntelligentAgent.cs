using Ollama.Domain.Agents;
using Ollama.Domain.Services;
using Ollama.Domain.Tools;
using Ollama.Infrastructure.Tools;
using Ollama.Infrastructure.Services;
using Ollama.Infrastructure.Clients;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace Ollama.Infrastructure.Agents
{
    public class IntelligentAgent : IAgent
    {
        private readonly IToolRepository _toolRepository;
        private readonly ILogger<IntelligentAgent> _logger;
        private readonly IPythonSubsystemService _pythonService;
        private readonly IPythonLlmClient _pythonClient;
        private readonly IPlanningService _planningService;

        public IntelligentAgent(
            IToolRepository toolRepository, 
            ILogger<IntelligentAgent> logger,
            IPythonSubsystemService pythonService,
            IPythonLlmClient pythonClient,
            IPlanningService planningService)
        {
            _toolRepository = toolRepository;
            _logger = logger;
            _pythonService = pythonService;
            _pythonClient = pythonClient;
            _planningService = planningService;
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
                _ => HandleGeneralQueryAsync(query).Result
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

        private async Task<string> HandleGeneralQueryAsync(string query)
        {
            try
            {
                // Ensure Python service is running for LLM queries
                if (!_pythonService.IsRunning)
                {
                    _logger.LogInformation("Starting Python subsystem for LLM processing...");
                    var started = await _pythonService.StartAsync();
                    if (!started)
                    {
                        _logger.LogWarning("Failed to start Python subsystem, falling back to local processing");
                        return HandleGeneralQuery(query);
                    }
                }

                // Use intelligent planning for this query
                _logger.LogInformation("Using intelligent planning for query: {Query}", query);
                
                // Create execution context
                var context = new Domain.Planning.ExecutionContext
                {
                    SessionId = Guid.NewGuid().ToString(),
                    OriginalQuery = query
                };

                // Create initial plan
                var initialPlan = await _planningService.CreateInitialPlanAsync(query, context);
                _logger.LogInformation("📋 Created initial plan with {StepCount} steps. Complete: {IsComplete}", 
                    initialPlan.Steps.Count, initialPlan.IsComplete);
                
                // Log planned models and tools
                foreach (var step in initialPlan.Steps)
                {
                    if (!string.IsNullOrEmpty(step.Model))
                    {
                        _logger.LogInformation("📊 Step {StepId} planned to use model: {Model} for {Purpose}", 
                            step.Id, step.Model, step.Purpose);
                    }
                    if (!string.IsNullOrEmpty(step.Tool))
                    {
                        _logger.LogInformation("🔧 Step {StepId} planned to use tool: {Tool} for {Purpose}", 
                            step.Id, step.Tool, step.Purpose);
                    }
                }

                // If it's a simple query that can be completed immediately
                if (initialPlan.IsComplete && initialPlan.Steps.Count == 1)
                {
                    var step = initialPlan.Steps.First();
                    var result = await ExecuteSimpleStepAsync(step, context);
                    return result;
                }

                // For now, fall back to the basic approach for complex queries
                // TODO: Implement full recursive planning loop
                _logger.LogInformation("Complex query detected, using basic approach for now");
                return await ExecuteBasicQueryAsync(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleGeneralQueryAsync");
                return $"I encountered an error while processing your request: {ex.Message}";
            }
        }

        private async Task<string> ExecuteSimpleStepAsync(Domain.Planning.ExecutionStep step, Domain.Planning.ExecutionContext context)
        {
            _logger.LogInformation("🚀 Executing step {StepId}: {Purpose}", step.Id, step.Purpose);
            
            try
            {
                if (!string.IsNullOrEmpty(step.Tool))
                {
                    _logger.LogInformation("🔧 Executing with tool: {Tool}", step.Tool);
                    return await ExecuteToolStepAsync(step, context);
                }
                else if (!string.IsNullOrEmpty(step.Model))
                {
                    _logger.LogInformation("🤖 Executing with model: {Model}", step.Model);
                    return await ExecuteModelStepAsync(step, context);
                }
                else
                {
                    _logger.LogWarning("⚠️ Step has no specific tool or model defined");
                    return "Step completed without specific execution";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error executing step {StepId}", step.Id);
                return $"Error executing step: {ex.Message}";
            }
        }

        private async Task<string> ExecuteToolStepAsync(Domain.Planning.ExecutionStep step, Domain.Planning.ExecutionContext context)
        {
            _logger.LogInformation("🔧 Starting tool execution: {Tool}", step.Tool);
            
            var tool = _toolRepository.GetToolByName(step.Tool!);
            if (tool == null)
            {
                _logger.LogError("❌ Tool not found: {Tool}", step.Tool);
                throw new InvalidOperationException($"Tool '{step.Tool}' not found");
            }

            _logger.LogInformation("✅ Tool found: {Tool} - {Description}", step.Tool, tool.Description ?? "No description");

            // Create tool context from step parameters
            var toolContext = new ToolContext();
            
            // Add the input parameter
            var input = step.Parameters.GetValueOrDefault("input", context.OriginalQuery)?.ToString() ?? context.OriginalQuery;
            toolContext.Parameters["input"] = input;
            _logger.LogInformation("📝 Tool input: {Input}", input);
            
            // Copy step parameters to tool context
            foreach (var param in step.Parameters)
            {
                toolContext.Parameters[param.Key] = param.Value;
                _logger.LogDebug("🔧 Tool parameter: {Key} = {Value}", param.Key, param.Value);
            }
            
            // Copy shared state
            foreach (var state in context.SharedState)
            {
                toolContext.State[state.Key] = state.Value;
            }

            _logger.LogInformation("⚙️ Executing tool: {Tool}", step.Tool);
            var result = await tool.RunAsync(toolContext);
            
            if (!result.Success)
            {
                _logger.LogError("❌ Tool execution failed: {Tool} - {Error}", step.Tool, result.ErrorMessage);
                throw new InvalidOperationException($"Tool execution failed: {result.ErrorMessage}");
            }

            var output = result.Output?.ToString() ?? "Tool executed successfully";
            _logger.LogInformation("✅ Tool execution completed: {Tool} - Output length: {Length} chars", 
                step.Tool, output.Length);
            
            return output;
        }

        private async Task<string> ExecuteModelStepAsync(Domain.Planning.ExecutionStep step, Domain.Planning.ExecutionContext context)
        {
            var prompt = step.EnhancedPrompt ?? step.Parameters.GetValueOrDefault("prompt", context.OriginalQuery)?.ToString() ?? context.OriginalQuery;
            var model = step.Model!;

            _logger.LogInformation("🤖 Starting model execution with: {Model}", model);
            _logger.LogInformation("📝 Model prompt: {Prompt}", prompt.Length > 100 ? prompt.Substring(0, 100) + "..." : prompt);

            // Log model parameters
            var parameters = new Dictionary<string, object>(step.Parameters);
            if (!parameters.ContainsKey("temperature"))
                parameters["temperature"] = 0.7;
            if (!parameters.ContainsKey("max_tokens"))
                parameters["max_tokens"] = 500;

            foreach (var param in parameters)
            {
                _logger.LogInformation("⚙️ Model parameter: {Key} = {Value}", param.Key, param.Value);
            }

            // Initialize chat session for this step
            _logger.LogInformation("🔗 Initializing chat session for model: {Model}", model);
            var chatId = await _pythonClient.InitializeChatAsync(model, "You are a helpful AI assistant.");
            _logger.LogInformation("✅ Chat session created: {ChatId}", chatId);

            try
            {
                _logger.LogInformation("🚀 Processing instruction with model: {Model}", model);
                var result = await _pythonClient.ProcessInstructionAsync(model, prompt, chatId, parameters);
                
                _logger.LogInformation("✅ Model execution completed: {Model} - Response length: {Length} chars", 
                    model, result.Length);
                _logger.LogDebug("📄 Model response: {Response}", result.Length > 200 ? result.Substring(0, 200) + "..." : result);
                
                return result;
            }
            finally
            {
                // Cleanup chat session
                _logger.LogInformation("🧹 Cleaning up chat session: {ChatId}", chatId);
                await _pythonClient.CleanupChatAsync(chatId);
                _logger.LogDebug("✅ Chat session cleanup completed");
            }
        }

        private async Task<string> ExecuteBasicQueryAsync(string query)
        {
            const string basicModel = "llama3.1:8b";
            
            _logger.LogInformation("🔄 Falling back to basic query execution with model: {Model}", basicModel);
            
            // Fallback to the basic approach
            var chatId = await _pythonClient.InitializeChatAsync(
                basicModel, 
                "You are a helpful AI assistant. Provide clear, accurate, and concise responses.");
            
            _logger.LogInformation("✅ Basic chat session created: {ChatId}", chatId);

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["temperature"] = 0.7,
                    ["max_tokens"] = 500
                };
                
                _logger.LogInformation("🚀 Processing basic query with model: {Model}", basicModel);
                foreach (var param in parameters)
                {
                    _logger.LogDebug("⚙️ Basic parameter: {Key} = {Value}", param.Key, param.Value);
                }

                var result = await _pythonClient.ProcessInstructionAsync(
                    basicModel, 
                    query, 
                    chatId, 
                    parameters);

                _logger.LogInformation("✅ Basic execution completed with model: {Model} - Response length: {Length} chars", 
                    basicModel, result.Length);
                
                return result;
            }
            finally
            {
                _logger.LogInformation("🧹 Cleaning up basic chat session: {ChatId}", chatId);
                await _pythonClient.CleanupChatAsync(chatId);
            }
        }

        private enum QueryType
        {
            SimpleArithmetic,
            RepositoryAnalysis,
            General
        }
    }
}
