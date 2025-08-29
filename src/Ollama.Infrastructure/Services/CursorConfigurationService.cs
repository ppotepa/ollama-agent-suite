using Microsoft.Extensions.Options;
using Ollama.Domain.Configuration;
using Ollama.Domain.Services;

namespace Ollama.Infrastructure.Services
{
    /// <summary>
    /// Service for accessing cursor configuration settings and formatting paths
    /// </summary>
    public class CursorConfigurationService : ICursorConfigurationService
    {
        public CursorSettings Settings { get; }

        public CursorConfigurationService(IOptions<CursorSettings> settings)
        {
            Settings = settings.Value;
        }

        /// <summary>
        /// Formats a path according to cursor configuration (full vs relative)
        /// </summary>
        public string FormatPath(string fullPath, string sessionRoot)
        {
            if (Settings.UseFullPaths)
            {
                return NormalizePathSeparators(fullPath);
            }

            // Check if path is within session boundaries
            if (fullPath.StartsWith(sessionRoot, StringComparison.OrdinalIgnoreCase))
            {
                var relative = fullPath.Substring(sessionRoot.Length);
                var trimmed = relative.TrimStart('\\', '/');
                var result = string.IsNullOrEmpty(trimmed) ? "." : trimmed;
                return NormalizePathSeparators(result);
            }

            // Path is outside session boundaries - apply security policy
            if (Settings.MaskSystemPaths)
            {
                // Mask the path for security (default behavior)
                return "[EXTERNAL_PATH]";
            }
            else
            {
                // Show relative to session root with indication it's external
                return $"../{Path.GetFileName(fullPath)}";
            }
        }

        /// <summary>
        /// Normalizes path separators according to configuration
        /// </summary>
        public string NormalizePathSeparators(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Replace Windows and Unix path separators with configured separator
            return path.Replace('\\', Settings.PathSeparator[0])
                      .Replace('/', Settings.PathSeparator[0]);
        }
    }
}
