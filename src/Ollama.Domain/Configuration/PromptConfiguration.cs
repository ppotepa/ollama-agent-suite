namespace Ollama.Domain.Configuration
{
    /// <summary>
    /// Configuration for prompt templates and placeholder processing
    /// </summary>
    public class PromptConfiguration
    {
        public string PromptBasePath { get; set; } = "prompts";
        public string PessimisticPromptFileName { get; set; } = "pessimistic-initial-system-prompt.txt";
        public string OptimisticPromptFileName { get; set; } = "optimistic-initial-system-prompt.txt";
        public string DefaultPromptFileName { get; set; } = "default-initial-system-prompt.txt";
        
        /// <summary>
        /// Whether to throw exceptions when prompt files are missing (true) or use fallback (false)
        /// </summary>
        public bool RequirePromptFiles { get; set; } = true;
        
        /// <summary>
        /// Whether to include JSON formatting instructions in prompts
        /// </summary>
        public bool IncludeJsonInstructions { get; set; } = true;
        
        /// <summary>
        /// Whether to include tool guidance in prompts
        /// </summary>
        public bool IncludeToolGuidance { get; set; } = true;
        
        /// <summary>
        /// Maximum size for prompt files in bytes to prevent memory issues
        /// </summary>
        public long MaxPromptFileSize { get; set; } = 1024 * 1024; // 1MB
    }
}
