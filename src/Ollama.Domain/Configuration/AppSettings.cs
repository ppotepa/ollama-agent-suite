using System.ComponentModel.DataAnnotations;

namespace Ollama.Domain.Configuration
{
    public class AppSettings
    {
        [Required]
        public string DefaultMode { get; set; } = "Intelligent";
    }

    public class OllamaSettings
    {
        [Required]
        public string BaseUrl { get; set; } = "http://localhost:11434";
        
        [Required]
        public string ApiEndpoint { get; set; } = "http://localhost:11434/api";
        
        [Required]
        public string GenerateEndpoint { get; set; } = "http://localhost:11434/api/generate";
        
        [Required]
        public string ChatEndpoint { get; set; } = "http://localhost:11434/api/chat";
        
        [Required]
        public string ModelsEndpoint { get; set; } = "http://localhost:11434/api/tags";
        
        [Required]
        public string DefaultModel { get; set; } = "qwen2.5:7b-instruct-q4_K_M";
        
        [Required]
        public string CoderModel { get; set; } = "qwen2.5:7b-instruct-q4_K_M";
        
        [Required]
        public string ThinkerModel { get; set; } = "qwen2.5:7b-instruct-q4_K_M";
        
        [Range(1, 300)]
        public int ConnectionTimeout { get; set; } = 30;
        
        [Range(1, 600)]
        public int RequestTimeout { get; set; } = 120;
        
        [Range(1, 10)]
        public int MaxRetries { get; set; } = 3;
        
        [Range(100, 10000)]
        public int RetryDelay { get; set; } = 1000;
    }

    public class AgentSettings
    {
        [Range(1, 20)]
        public int MaxConcurrentAgents { get; set; } = 5;
        
        [Range(10, 600)]
        public int DefaultAgentTimeout { get; set; } = 60;
        
        public bool CollaborationEnabled { get; set; } = true;
        
        [Range(1, 50)]
        public int ExecutionTreeDepth { get; set; } = 10;
        
        public bool EnableDebugMode { get; set; } = false;
    }

    public class ModeSettings
    {
        public SingleQueryModeSettings SingleQuery { get; set; } = new();
        public IntelligentModeSettings Intelligent { get; set; } = new();
        public CollaborativeModeSettings Collaborative { get; set; } = new();
    }

    public class SingleQueryModeSettings
    {
        public bool Enabled { get; set; } = true;
        public string DefaultModel { get; set; } = "qwen2.5:7b-instruct-q4_K_M";
        public bool DebugOutput { get; set; } = false;
    }

    public class IntelligentModeSettings
    {
        public bool Enabled { get; set; } = true;
        public string DefaultModel { get; set; } = "qwen2.5:7b-instruct-q4_K_M";
        public bool UseContextSwitching { get; set; } = true;
        public bool DebugContextSwitching { get; set; } = false;
    }

    public class CollaborativeModeSettings
    {
        public bool Enabled { get; set; } = true;
        
        [Range(1, 10)]
        public int MaxCollaborators { get; set; } = 3;
        
        [Range(1000, 30000)]
        public int SyncInterval { get; set; } = 5000;
        
        public bool DebugCollaboration { get; set; } = false;
    }

    public class InfrastructureSettings
    {
        public bool CacheEnabled { get; set; } = true;
        
        [Range(1, 1440)]
        public int CacheExpirationMinutes { get; set; } = 30;
        
        public bool LogRequests { get; set; } = true;
        public bool LogResponses { get; set; } = false;
        public bool EnableDetailedLogging { get; set; } = false;
    }

    public class CursorSettings
    {
        /// <summary>
        /// When true, tools will return full system paths in responses.
        /// When false (default), only relative paths from session cache are returned for security.
        /// </summary>
        public bool UseFullPaths { get; set; } = false;
        
        /// <summary>
        /// When true, includes absolute path information in cursor context for debugging.
        /// </summary>
        public bool IncludeDebugPaths { get; set; } = false;
        
        /// <summary>
        /// Path separator to use in responses (defaults to forward slash for cross-platform compatibility).
        /// </summary>
        public string PathSeparator { get; set; } = "/";
        
        /// <summary>
        /// Whether to show the session root in path displays.
        /// When true, shows context about session boundaries.
        /// </summary>
        public bool ShowSessionRoot { get; set; } = true;
        
        /// <summary>
        /// Whether to mask/hide system paths that fall outside session boundaries.
        /// When true, external paths are shown as "[EXTERNAL_PATH]" for security.
        /// </summary>
        public bool MaskSystemPaths { get; set; } = true;
        
        /// <summary>
        /// Gets the effective path separator based on configuration and system
        /// </summary>
        public string GetEffectivePathSeparator()
        {
            return PathSeparator.ToLower() switch
            {
                "auto" => Path.DirectorySeparatorChar.ToString(),
                "/" => "/",
                "\\" => "\\",
                _ => "/"
            };
        }
    }
}
