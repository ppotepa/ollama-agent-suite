using System;

namespace Ollama.Domain.Tools.Attributes
{
    /// <summary>
    /// Attribute to specify capabilities of a tool using the ToolCapability enum
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ToolCapabilitiesAttribute : Attribute
    {
        public ToolCapability Capabilities { get; }
        public string[] AdditionalNotes { get; set; }
        public bool IsExperimental { get; set; }
        public string? FallbackStrategy { get; set; }

        public ToolCapabilitiesAttribute(ToolCapability capabilities)
        {
            Capabilities = capabilities;
            AdditionalNotes = Array.Empty<string>();
        }
    }
}
