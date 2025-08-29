using Ollama.Domain.Tools;
using Ollama.Domain.Tools.Attributes;
using Ollama.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Ollama.Infrastructure.Tools
{
    [ToolDescription(
        "Evaluates mathematical expressions safely using built-in arithmetic operations", 
        "Safe mathematical expression evaluator that supports basic arithmetic (+, -, *, /), parentheses, and common mathematical functions. Does not execute arbitrary code and is sandboxed for security.", 
        "Mathematical Operations")]
    [ToolUsage(
        "Evaluate mathematical expressions and perform calculations",
        SecondaryUseCases = new[] { "Arithmetic calculations", "Formula evaluation", "Basic math operations", "Number processing" },
        RequiredParameters = new[] { "expression" },
        OptionalParameters = new string[0],
        ExampleInvocation = "MathEvaluator with expression=\"2 + 3 * 4\" to calculate result",
        ExpectedOutput = "Numerical result of the expression evaluation",
        RequiresFileSystem = false,
        RequiresNetwork = false,
        SafetyNotes = "Sandboxed evaluation - no code execution, only safe mathematical operations",
        PerformanceNotes = "Very fast operation, suitable for real-time calculations")]
    [ToolCapabilities(
        ToolCapability.MathCalculation | ToolCapability.MathEvaluation,
        FallbackStrategy = "Built-in .NET mathematical operations with expression parsing")]
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
