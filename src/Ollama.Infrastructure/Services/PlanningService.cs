using Microsoft.Extensions.Logging;
using Ollama.Domain.Planning;
using Ollama.Domain.Services;
using Ollama.Domain.Tools;
using Ollama.Infrastructure.Clients;
using System.Text;
using System.Text.Json;
using PlanningExecutionContext = Ollama.Domain.Planning.ExecutionContext;

namespace Ollama.Infrastructure.Services
{
    public class PlanningService : IPlanningService
    {
        private readonly IPythonLlmClient _pythonClient;
        private readonly IToolRepository _toolRepository;
        private readonly IModelRegistryService _modelRegistry;
        private readonly ILogger<PlanningService> _logger;
        private const string PLANNING_MODEL = "llama3.1:8b";

        public PlanningService(
            IPythonLlmClient pythonClient,
            IToolRepository toolRepository,
            IModelRegistryService modelRegistry,
            ILogger<PlanningService> logger)
        {
            _pythonClient = pythonClient;
            _toolRepository = toolRepository;
            _modelRegistry = modelRegistry;
            _logger = logger;
        }

        public async Task<ExecutionPlan> CreateInitialPlanAsync(string query, PlanningExecutionContext context)
        {
            _logger.LogInformation("🧠 Creating initial execution plan for query: {Query}", query);
            _logger.LogInformation("📊 Using planning model: {Model}", PLANNING_MODEL);

            var systemPrompt = GenerateSystemPrompt(context);
            var userPrompt = $@"
ORIGINAL QUERY: {query}

Create an initial execution plan to answer this query. Analyze what needs to be done and create the first step only.
If the query is simple enough to answer directly, set is_complete to true.

Respond with valid JSON following the ExecutionPlan schema.";

            try
            {
                _logger.LogInformation("🚀 Calling planning model: {Model}", PLANNING_MODEL);
                var planJson = await CallPlanningModelAsync(systemPrompt, userPrompt, context.SessionId);
                
                _logger.LogDebug("📄 Planning model response: {Response}", 
                    planJson.Length > 200 ? planJson.Substring(0, 200) + "..." : planJson);
                
                var plan = JsonSerializer.Deserialize<ExecutionPlan>(planJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (plan == null)
                {
                    _logger.LogError("❌ Failed to deserialize execution plan from model: {Model}", PLANNING_MODEL);
                    return CreateFallbackPlan(query);
                }

                _logger.LogInformation("✅ Planning model {Model} generated plan with {StepCount} steps", 
                    PLANNING_MODEL, plan.Steps.Count);
                
                // Log what models/tools the plan recommends
                foreach (var step in plan.Steps)
                {
                    if (!string.IsNullOrEmpty(step.Model))
                    {
                        _logger.LogInformation("📊 Plan recommends model: {Model} for step {StepId}", step.Model, step.Id);
                    }
                    if (!string.IsNullOrEmpty(step.Tool))
                    {
                        _logger.LogInformation("🔧 Plan recommends tool: {Tool} for step {StepId}", step.Tool, step.Id);
                    }
                }

                context.PlanHistory.Add(plan);
                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating initial plan with model: {Model}", PLANNING_MODEL);
                return CreateFallbackPlan(query);
            }
        }

        public async Task<ExecutionPlan> CreateNextStepAsync(PlanningExecutionContext context)
        {
            _logger.LogInformation("Creating next execution step for session: {SessionId}", context.SessionId);

            var systemPrompt = GenerateSystemPrompt(context);
            var executionHistory = BuildExecutionHistory(context);
            
            var userPrompt = $@"
ORIGINAL QUERY: {context.OriginalQuery}

EXECUTION HISTORY:
{executionHistory}

Based on the execution history and results so far, create the next execution step.
If the query has been fully answered, set is_complete to true and provide a final_response_template.

Respond with valid JSON following the ExecutionPlan schema.";

            try
            {
                var planJson = await CallPlanningModelAsync(systemPrompt, userPrompt, context.SessionId);
                var plan = JsonSerializer.Deserialize<ExecutionPlan>(planJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (plan == null)
                {
                    _logger.LogError("Failed to deserialize next step plan");
                    return CreateCompletionPlan();
                }

                context.PlanHistory.Add(plan);
                return plan;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating next step");
                return CreateCompletionPlan();
            }
        }

        public Task<bool> IsExecutionCompleteAsync(PlanningExecutionContext context)
        {
            if (context.PlanHistory.Count == 0)
                return Task.FromResult(false);

            var lastPlan = context.PlanHistory.Last();
            return Task.FromResult(lastPlan.IsComplete);
        }

        public Task<string> GenerateFinalResponseAsync(PlanningExecutionContext context)
        {
            _logger.LogInformation("Generating final response for session: {SessionId}", context.SessionId);

            var lastPlan = context.PlanHistory.LastOrDefault();
            var template = lastPlan?.FinalResponseTemplate ?? "Based on the execution results: {results}";
            
            var results = string.Join("\n", context.Results.Select(r => r.Output));
            var finalResponse = template.Replace("{results}", results);

            return Task.FromResult(finalResponse);
        }

        public string GenerateSystemPrompt(PlanningExecutionContext context)
        {
            var prompt = new StringBuilder();
            
            prompt.AppendLine("You are an AI planning assistant that creates execution steps for complex queries.");
            prompt.AppendLine("Your role is to break down queries into actionable steps and coordinate between different tools and models.");
            prompt.AppendLine();
            
            // Add tool information
            prompt.AppendLine("AVAILABLE TOOLS:");
            foreach (var tool in _toolRepository.GetAllTools())
            {
                prompt.AppendLine($"- {tool.Name}: {tool.Description ?? "Tool for specialized tasks"}");
                prompt.AppendLine($"  Capabilities: {string.Join(", ", tool.Capabilities)}");
            }
            prompt.AppendLine();

            // Add model information
            prompt.AppendLine("AVAILABLE MODELS:");
            var modelCapabilities = _modelRegistry.GetModelCapabilities();
            foreach (var (model, capabilities) in modelCapabilities)
            {
                prompt.AppendLine($"- {model}: {string.Join(", ", capabilities)}");
            }
            prompt.AppendLine();

            // Add environment information
            prompt.AppendLine("OPERATING ENVIRONMENT:");
            prompt.AppendLine($"- OS: {Environment.OSVersion}");
            prompt.AppendLine($"- Runtime: .NET {Environment.Version}");
            prompt.AppendLine($"- Working Directory: {Environment.CurrentDirectory}");
            prompt.AppendLine();

            // Add response format
            prompt.AppendLine("RESPONSE FORMAT:");
            prompt.AppendLine("You must respond with valid JSON following this ExecutionPlan schema:");
            prompt.AppendLine(@"{
  ""reasoning"": ""Your step-by-step thought process"",
  ""steps"": [
    {
      ""id"": 1,
      ""tool"": ""ToolName"" | null,
      ""model"": ""ModelName"" | null,
      ""agent_type"": ""Planning|Coding|Math|Research|General"" | null,
      ""parameters"": {},
      ""purpose"": ""Why this step is needed"",
      ""expected_output"": ""What you expect to get"",
      ""enhanced_prompt"": ""Enhanced prompt for the target model/tool"" | null,
      ""dependencies"": [previous_step_ids]
    }
  ],
  ""final_response_template"": ""Template for final response"" | """",
  ""is_complete"": false
}");

            prompt.AppendLine();
            prompt.AppendLine("EXECUTION CONSTRAINTS:");
            prompt.AppendLine("- Create only ONE step at a time");
            prompt.AppendLine("- Choose the most appropriate tool or model for each step");
            prompt.AppendLine("- Enhance prompts with context and specific instructions");
            prompt.AppendLine("- Set is_complete to true only when the query is fully answered");

            return prompt.ToString();
        }

        private async Task<string> CallPlanningModelAsync(string systemPrompt, string userPrompt, string sessionId)
        {
            _logger.LogInformation("🔗 Initializing chat session for planning model: {Model}", PLANNING_MODEL);
            
            var chatId = await _pythonClient.InitializeChatAsync(PLANNING_MODEL, systemPrompt);
            _logger.LogInformation("✅ Planning chat session created: {ChatId}", chatId);
            
            var parameters = new Dictionary<string, object>
            {
                ["temperature"] = 0.2, // Low temperature for consistent planning
                ["max_tokens"] = 1000,
                ["stop"] = new[] { "```", "---" }
            };

            _logger.LogInformation("🚀 Processing planning request with model: {Model}", PLANNING_MODEL);
            foreach (var param in parameters)
            {
                _logger.LogDebug("⚙️ Planning parameter: {Key} = {Value}", param.Key, param.Value);
            }

            var response = await _pythonClient.ProcessInstructionAsync(
                PLANNING_MODEL, 
                userPrompt, 
                chatId, 
                parameters);

            _logger.LogInformation("✅ Planning model {Model} response received - Length: {Length} chars", 
                PLANNING_MODEL, response.Length);

            return response;
        }

        private string BuildExecutionHistory(PlanningExecutionContext context)
        {
            var history = new StringBuilder();
            
            for (int i = 0; i < context.Results.Count; i++)
            {
                var result = context.Results[i];
                history.AppendLine($"Step {result.StepId}:");
                history.AppendLine($"  Success: {result.Success}");
                history.AppendLine($"  Output: {result.Output}");
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    history.AppendLine($"  Error: {result.ErrorMessage}");
                }
                history.AppendLine();
            }

            return history.ToString();
        }

        private ExecutionPlan CreateFallbackPlan(string query)
        {
            return new ExecutionPlan
            {
                Reasoning = "Fallback plan: Process query directly with general model",
                Steps = new List<ExecutionStep>
                {
                    new ExecutionStep
                    {
                        Id = 1,
                        Model = "llama3.3:70b-instruct-q3_K_M",
                        AgentType = "General",
                        Purpose = "Answer the query directly",
                        ExpectedOutput = "Direct answer to the user's question",
                        EnhancedPrompt = $"Please answer this question clearly and concisely: {query}",
                        Parameters = new Dictionary<string, object>
                        {
                            ["temperature"] = 0.7,
                            ["max_tokens"] = 500
                        }
                    }
                },
                IsComplete = true,
                FinalResponseTemplate = "{results}"
            };
        }

        private ExecutionPlan CreateCompletionPlan()
        {
            return new ExecutionPlan
            {
                Reasoning = "Execution completed based on previous steps",
                Steps = new List<ExecutionStep>(),
                IsComplete = true,
                FinalResponseTemplate = "Based on the execution: {results}"
            };
        }
    }
}
