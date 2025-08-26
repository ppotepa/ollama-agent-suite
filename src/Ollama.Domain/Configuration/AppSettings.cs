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
        public string DefaultModel { get; set; } = "llama3.2";
        
        [Required]
        public string CoderModel { get; set; } = "codellama:7b";
        
        [Required]
        public string ThinkerModel { get; set; } = "llama3.2";
        
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
        public string DefaultModel { get; set; } = "llama3.2";
        public bool DebugOutput { get; set; } = false;
    }

    public class IntelligentModeSettings
    {
        public bool Enabled { get; set; } = true;
        public string DefaultModel { get; set; } = "llama3.2";
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
}
