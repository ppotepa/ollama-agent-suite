namespace Ollama.Domain.Configuration
{
    /// <summary>
    /// Configuration settings for cursor path handling and display
    /// </summary>
    public class CursorSettings
    {
        /// <summary>
        /// Whether to use full system paths in responses.
        /// When false (default), paths are shown relative to session root for security.
        /// </summary>
        public bool UseFullPaths { get; set; } = false;
        
        /// <summary>
        /// Whether to show the session root in path displays.
        /// When true, shows context about session boundaries.
        /// </summary>
        public bool ShowSessionRoot { get; set; } = true;
        
        /// <summary>
        /// When true, includes absolute path information in cursor context for debugging.
        /// </summary>
        public bool IncludeDebugPaths { get; set; } = false;
        
        /// <summary>
        /// Path separator to use in responses ("/", "\", or "auto").
        /// "auto" uses the system default.
        /// </summary>
        public string PathSeparator { get; set; } = "/";
        
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
