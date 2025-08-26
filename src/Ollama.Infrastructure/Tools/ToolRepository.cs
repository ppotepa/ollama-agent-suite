using Ollama.Domain.Tools;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Ollama.Infrastructure.Tools
{
    public class ToolRepository : IToolRepository
    {
        private readonly ConcurrentDictionary<string, ITool> _tools = new();
        private readonly ILogger<ToolRepository> _logger;

        public ToolRepository(ILogger<ToolRepository> logger)
        {
            _logger = logger;
        }

        public void RegisterTool(ITool tool)
        {
            if (_tools.TryAdd(tool.Name.ToLowerInvariant(), tool))
            {
                _logger.LogInformation("Tool {ToolName} registered successfully", tool.Name);
            }
            else
            {
                _logger.LogWarning("Tool {ToolName} already registered", tool.Name);
            }
        }

        public ITool? GetToolByName(string name)
        {
            _tools.TryGetValue(name.ToLowerInvariant(), out var tool);
            return tool;
        }

        public IEnumerable<ITool> FindToolsByCapability(string capability)
        {
            return _tools.Values.Where(t => t.Capabilities.Contains(capability.ToLowerInvariant()));
        }

        public IEnumerable<ITool> GetAllTools()
        {
            return _tools.Values;
        }
    }
}
