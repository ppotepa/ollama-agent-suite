using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ollama.Domain.Prompts;
using Ollama.Domain.Tools;
using Ollama.Infrastructure.Tools;

namespace Ollama.Infrastructure.Prompts
{
    /// <summary>
    /// Decorator that replaces [REFLECTION.TOOLS] placeholder with actual tool information
    /// </summary>
    public class ToolReflectionDecorator : IPlaceholderDecorator
    {
        private readonly IToolRepository _toolRepository;
        private readonly ILogger<ToolReflectionDecorator> _logger;
        
        public ToolReflectionDecorator(IToolRepository toolRepository, ILogger<ToolReflectionDecorator> logger)
        {
            _toolRepository = toolRepository;
            _logger = logger;
        }
        
        public IEnumerable<string> SupportedPlaceholders => new[] { "[REFLECTION.TOOLS]" };
        
        public int Priority => 100; // Lower priority, process after basic placeholders
        
        public Task<string> ProcessPlaceholdersAsync(string template, string sessionId)
        {
            try
            {
                if (template.Contains("[REFLECTION.TOOLS]"))
                {
                    _logger.LogDebug("Processing [REFLECTION.TOOLS] placeholder for session {SessionId}", sessionId);
                    
                    var toolInfo = ToolInfoGenerator.GenerateToolInformation(_toolRepository);
                    template = template.Replace("[REFLECTION.TOOLS]", toolInfo);
                    
                    _logger.LogDebug("Successfully replaced [REFLECTION.TOOLS] placeholder with {ToolCount} tools", 
                        toolInfo.Count(c => c == '\n'));
                }
                
                return Task.FromResult(template);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error processing [REFLECTION.TOOLS] placeholder");
                // Return template unchanged rather than failing the entire prompt generation
                return Task.FromResult(template);
            }
        }
    }
}
