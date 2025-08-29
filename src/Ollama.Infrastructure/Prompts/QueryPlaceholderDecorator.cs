using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ollama.Domain.Prompts;

namespace Ollama.Infrastructure.Prompts
{
    /// <summary>
    /// Decorator that replaces [QUERY] and [SESSION_INFO] placeholders with context-specific information
    /// </summary>
    public class QueryPlaceholderDecorator : IPlaceholderDecorator
    {
        private readonly ILogger<QueryPlaceholderDecorator> _logger;
        
        public QueryPlaceholderDecorator(ILogger<QueryPlaceholderDecorator> logger)
        {
            _logger = logger;
        }
        
        public IEnumerable<string> SupportedPlaceholders => new[] { "[QUERY]", "[SESSION_INFO]" };
        
        public int Priority => 50; // Medium priority
        
        public Task<string> ProcessPlaceholdersAsync(string template, string sessionId)
        {
            // Default implementation without context
            return Task.FromResult(template);
        }
        
        public Task<string> ProcessPlaceholdersAsync(string template, PlaceholderContext context)
        {
            try
            {
                string processedTemplate = template;
                
                // Replace [QUERY] placeholder if provided in context
                if (processedTemplate.Contains("[QUERY]"))
                {
                    var query = context.GetValue<string>("[QUERY]") ?? "";
                    processedTemplate = processedTemplate.Replace("[QUERY]", query);
                    _logger.LogDebug("Replaced [QUERY] placeholder for session {SessionId}", context.SessionId);
                }
                
                // Replace [SESSION_INFO] placeholder if provided in context
                if (processedTemplate.Contains("[SESSION_INFO]"))
                {
                    var sessionInfo = context.GetValue<string>("[SESSION_INFO]") ?? "";
                    processedTemplate = processedTemplate.Replace("[SESSION_INFO]", sessionInfo);
                    _logger.LogDebug("Replaced [SESSION_INFO] placeholder for session {SessionId}", context.SessionId);
                }
                
                return Task.FromResult(processedTemplate);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error processing query placeholders");
                return Task.FromResult(template);
            }
        }
    }
}
