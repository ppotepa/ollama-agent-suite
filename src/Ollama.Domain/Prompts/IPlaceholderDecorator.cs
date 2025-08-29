using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ollama.Domain.Prompts
{
    /// <summary>
    /// Interface for processing placeholders in prompt templates
    /// </summary>
    public interface IPlaceholderDecorator
    {
        /// <summary>
        /// Process placeholders in the template and replace them with actual content
        /// </summary>
        /// <param name="template">The template containing placeholders</param>
        /// <param name="sessionId">The current session ID</param>
        /// <returns>Template with placeholders replaced</returns>
        Task<string> ProcessPlaceholdersAsync(string template, string sessionId);
        
        /// <summary>
        /// Gets the placeholders that this decorator handles
        /// </summary>
        IEnumerable<string> SupportedPlaceholders { get; }
        
        /// <summary>
        /// Priority order for applying decorators (lower numbers processed first)
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Process placeholders in the template with additional context
        /// Default implementation falls back to the simple sessionId method
        /// </summary>
        /// <param name="template">The template containing placeholders</param>
        /// <param name="context">The placeholder context with values</param>
        /// <returns>Template with placeholders replaced</returns>
        Task<string> ProcessPlaceholdersAsync(string template, PlaceholderContext context)
        {
            return ProcessPlaceholdersAsync(template, context.SessionId);
        }
    }
}
