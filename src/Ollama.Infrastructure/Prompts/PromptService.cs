using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Ollama.Domain.Configuration;
using Ollama.Domain.Prompts;

namespace Ollama.Infrastructure.Prompts
{
    /// <summary>
    /// Service responsible for loading and processing prompt templates
    /// </summary>
    public class PromptService
    {
        private readonly PromptConfiguration _configuration;
        private readonly IEnumerable<IPlaceholderDecorator> _placeholderDecorators;
        private readonly ILogger<PromptService> _logger;
        
        public PromptService(
            PromptConfiguration configuration,
            IEnumerable<IPlaceholderDecorator> placeholderDecorators,
            ILogger<PromptService> logger)
        {
            _configuration = configuration;
            _placeholderDecorators = placeholderDecorators;
            _logger = logger;
        }
        
        /// <summary>
        /// Load and process a prompt template with all placeholders replaced
        /// </summary>
        /// <param name="promptFileName">Name of the prompt file</param>
        /// <param name="sessionId">Current session ID</param>
        /// <returns>Processed prompt template</returns>
        public async Task<string> GetProcessedPromptAsync(string promptFileName, string sessionId)
        {
            try
            {
                // Load the raw template
                string template = await LoadPromptTemplateAsync(promptFileName);
                
                // Process all placeholders
                template = await ProcessAllPlaceholdersAsync(template, sessionId);
                
                _logger.LogDebug("Successfully processed prompt template {FileName} for session {SessionId}", 
                    promptFileName, sessionId);
                
                return template;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing prompt template {FileName} for session {SessionId}", 
                    promptFileName, sessionId);
                throw;
            }
        }
        
        /// <summary>
        /// Load raw prompt template from file
        /// </summary>
        private async Task<string> LoadPromptTemplateAsync(string promptFileName)
        {
            string promptFilePath = Path.Combine(_configuration.PromptBasePath, promptFileName);
            
            if (!File.Exists(promptFilePath))
            {
                var message = $"Prompt template not found at {promptFilePath}";
                _logger.LogError(message);
                
                if (_configuration.RequirePromptFiles)
                {
                    throw new FileNotFoundException(message);
                }
                else
                {
                    _logger.LogWarning("Using empty template as fallback");
                    return string.Empty;
                }
            }
            
            // Check file size to prevent memory issues
            var fileInfo = new FileInfo(promptFilePath);
            if (fileInfo.Length > _configuration.MaxPromptFileSize)
            {
                throw new InvalidOperationException($"Prompt file {promptFilePath} is too large ({fileInfo.Length} bytes, max: {_configuration.MaxPromptFileSize})");
            }
            
            _logger.LogDebug("Loading prompt template from {FilePath}", promptFilePath);
            return await File.ReadAllTextAsync(promptFilePath);
        }
        
        /// <summary>
        /// Process all registered placeholder decorators
        /// </summary>
        private async Task<string> ProcessAllPlaceholdersAsync(string template, string sessionId)
        {
            string processedTemplate = template;
            
            foreach (var decorator in _placeholderDecorators)
            {
                try
                {
                    processedTemplate = await decorator.ProcessPlaceholdersAsync(processedTemplate, sessionId);
                    
                    _logger.LogDebug("Processed placeholders {Placeholders} using {DecoratorType}", 
                        string.Join(", ", decorator.SupportedPlaceholders), 
                        decorator.GetType().Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing placeholders {Placeholders} with {DecoratorType}", 
                        string.Join(", ", decorator.SupportedPlaceholders), 
                        decorator.GetType().Name);
                    
                    // Continue with other decorators rather than failing completely
                }
            }
            
            return processedTemplate;
        }
        
        /// <summary>
        /// Get all supported placeholders from registered decorators
        /// </summary>
        public IEnumerable<string> GetSupportedPlaceholders()
        {
            return _placeholderDecorators.SelectMany(d => d.SupportedPlaceholders).Distinct();
        }
    }
}
