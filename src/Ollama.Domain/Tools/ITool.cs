using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ollama.Domain.Tools
{
    public interface ITool
    {
        string Name { get; }
        string Description { get; }
        IEnumerable<string> Capabilities { get; }
        bool RequiresNetwork { get; }
        bool RequiresFileSystem { get; }

        Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default);
        Task<decimal> EstimateCostAsync(ToolContext context);
        Task<bool> DryRunAsync(ToolContext context);
    }

    public class ToolContext
    {
        public IDictionary<string, object> Parameters { get; } = new Dictionary<string, object>();
        public IDictionary<string, object> State { get; } = new Dictionary<string, object>();
        public string? WorkingDirectory { get; set; }
    }

    public class ToolResult
    {
        public bool Success { get; set; }
        public object? Output { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal CostIncurred { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }
}
