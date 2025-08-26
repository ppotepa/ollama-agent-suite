using Ollama.Domain.Planning;
using PlanningExecutionContext = Ollama.Domain.Planning.ExecutionContext;

namespace Ollama.Domain.Services
{
    public interface IPlanningService
    {
        Task<ExecutionPlan> CreateInitialPlanAsync(string query, PlanningExecutionContext context);
        Task<ExecutionPlan> CreateNextStepAsync(PlanningExecutionContext context);
        Task<bool> IsExecutionCompleteAsync(PlanningExecutionContext context);
        Task<string> GenerateFinalResponseAsync(PlanningExecutionContext context);
        string GenerateSystemPrompt(PlanningExecutionContext context);
    }
}
