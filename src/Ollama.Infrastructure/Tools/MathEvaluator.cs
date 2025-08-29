using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Tools
{
    public class MathEvaluator : AbstractTool
    {
        public override string Name => "MathEvaluator";
        public override string Description => "Evaluates simple mathematical expressions safely";
        public override IEnumerable<string> Capabilities => new[] { "math:evaluate", "arithmetic:calculate" };
        public override bool RequiresNetwork => false;
        public override bool RequiresFileSystem => false;

        public MathEvaluator(ISessionScope sessionScope, ILogger<MathEvaluator> logger) 
            : base(sessionScope, logger)
        {
        }

        public override Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(context.Parameters.ContainsKey("expression"));
        }

        public override Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m); // Free operation
        }

        public override Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
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
