using System;

namespace Ollama.Domain.Tools.Attributes
{
    /// <summary>
    /// Attribute to define tool usage patterns and contexts
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ToolUsageAttribute : Attribute
    {
        public string PrimaryUseCase { get; set; }
        public string[] SecondaryUseCases { get; set; }
        public string[] RequiredParameters { get; set; }
        public string[] OptionalParameters { get; set; }
        public string? ExampleInvocation { get; set; }
        public string? ExpectedOutput { get; set; }
        public bool RequiresFileSystem { get; set; }
        public bool RequiresNetwork { get; set; }
        public bool RequiresElevatedPrivileges { get; set; }
        public string? SafetyNotes { get; set; }
        public string? PerformanceNotes { get; set; }

        public ToolUsageAttribute(string primaryUseCase)
        {
            PrimaryUseCase = primaryUseCase ?? throw new ArgumentNullException(nameof(primaryUseCase));
            SecondaryUseCases = Array.Empty<string>();
            RequiredParameters = Array.Empty<string>();
            OptionalParameters = Array.Empty<string>();
        }
    }
}
