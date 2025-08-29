using Ollama.Domain.Configuration;

namespace Ollama.Domain.Services
{
    /// <summary>
    /// Service for accessing cursor configuration settings
    /// </summary>
    public interface ICursorConfigurationService
    {
        /// <summary>
        /// Gets the current cursor settings
        /// </summary>
        CursorSettings Settings { get; }
        
        /// <summary>
        /// Formats a path according to cursor configuration (full vs relative)
        /// </summary>
        string FormatPath(string fullPath, string sessionRoot);
        
        /// <summary>
        /// Normalizes path separators according to configuration
        /// </summary>
        string NormalizePathSeparators(string path);
    }
}
