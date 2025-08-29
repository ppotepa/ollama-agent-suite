using System;

namespace Ollama.Domain.Tools.Attributes
{
    /// <summary>
    /// Provides detailed description information for tools
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ToolDescriptionAttribute : Attribute
    {
        /// <summary>
        /// Brief description of what the tool does
        /// </summary>
        public string Description { get; }
        
        /// <summary>
        /// Detailed usage instructions and examples
        /// </summary>
        public string Usage { get; }
        
        /// <summary>
        /// Category this tool belongs to (e.g., "File Operations", "Network", "Analysis")
        /// </summary>
        public string Category { get; }
        
        /// <summary>
        /// Whether this tool requires network access
        /// </summary>
        public bool RequiresNetwork { get; set; } = false;
        
        /// <summary>
        /// Whether this tool requires file system access
        /// </summary>
        public bool RequiresFileSystem { get; set; } = false;
        
        /// <summary>
        /// Whether this tool modifies data (vs read-only operations)
        /// </summary>
        public bool IsDestructive { get; set; } = false;

        public ToolDescriptionAttribute(string description, string usage, string category)
        {
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Usage = usage ?? throw new ArgumentNullException(nameof(usage));
            Category = category ?? throw new ArgumentNullException(nameof(category));
        }
    }
}
