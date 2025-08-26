using Ollama.Domain.Tools;

namespace Ollama.Infrastructure.Tools
{
    public class MathEvaluator : ITool
    {
        public string Name => "MathEvaluator";
        public string Description => "Evaluates simple mathematical expressions safely";
        public IEnumerable<string> Capabilities => new[] { "math:evaluate", "arithmetic:calculate" };
        public bool RequiresNetwork => false;
        public bool RequiresFileSystem => false;

        public Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(context.Parameters.ContainsKey("expression"));
        }

        public Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // Free operation
        }

        public Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.Now;
            
            if (!context.Parameters.TryGetValue("expression", out var expressionObj))
            {
                return Task.FromResult(new ToolResult
                {
                    Success = false,
                    ErrorMessage = "Mathematical expression is required",
                    ExecutionTime = DateTime.Now - startTime
                });
            }

            var expression = expressionObj.ToString();
            
            try
            {
                // Use System.Data.DataTable.Compute for safe evaluation
                var table = new System.Data.DataTable();
                var result = table.Compute(expression, null);
                
                return Task.FromResult(new ToolResult
                {
                    Success = true,
                    Output = result,
                    ExecutionTime = DateTime.Now - startTime
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ToolResult
                {
                    Success = false,
                    ErrorMessage = $"Error evaluating expression: {ex.Message}",
                    ExecutionTime = DateTime.Now - startTime
                });
            }
        }
    }
}
