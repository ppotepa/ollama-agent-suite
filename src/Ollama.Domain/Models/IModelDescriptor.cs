using System.ComponentModel.DataAnnotations;

namespace Ollama.Domain.Models
{
    /// <summary>
    /// Interface for model descriptor providing metadata about Ollama models
    /// </summary>
    public interface IModelDescriptor
    {
        /// <summary>
        /// The unique identifier/name of the model (derived from link)
        /// </summary>
        string ModelName { get; }
        
        /// <summary>
        /// Human-readable description of the model's purpose and characteristics
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// List of capabilities the model supports (e.g., "tools", "vision", "embedding")
        /// </summary>
        IReadOnlyList<ModelCapability> Capabilities { get; }
        
        /// <summary>
        /// Available parameter sizes for the model (e.g., "7b", "13b", "70b")
        /// </summary>
        IReadOnlyList<ModelSize> Sizes { get; }
        
        /// <summary>
        /// Statistics about model usage and updates
        /// </summary>
        ModelStatistics Statistics { get; }
        
        /// <summary>
        /// Official link to the model's page
        /// </summary>
        Uri OfficialLink { get; }
        
        /// <summary>
        /// Determines if the model supports a specific capability
        /// </summary>
        /// <param name="capability">The capability to check</param>
        /// <returns>True if the capability is supported, false otherwise</returns>
        bool SupportsCapability(ModelCapability capability);
        
        /// <summary>
        /// Determines if the model is available in a specific size
        /// </summary>
        /// <param name="size">The size to check</param>
        /// <returns>True if the size is available, false otherwise</returns>
        bool HasSize(ModelSize size);
        
        /// <summary>
        /// Gets the recommended size based on use case requirements
        /// </summary>
        /// <param name="performanceRequirement">Performance vs resource trade-off</param>
        /// <returns>Recommended model size</returns>
        ModelSize GetRecommendedSize(PerformanceRequirement performanceRequirement);
    }

    /// <summary>
    /// Concrete implementation of model descriptor for Ollama models
    /// </summary>
    public sealed class OllamaModelDescriptor : IModelDescriptor
    {
        public string ModelName { get; }
        public string Description { get; }
        public IReadOnlyList<ModelCapability> Capabilities { get; }
        public IReadOnlyList<ModelSize> Sizes { get; }
        public ModelStatistics Statistics { get; }
        public Uri OfficialLink { get; }

        public OllamaModelDescriptor(
            string modelName,
            string description,
            IEnumerable<ModelCapability> capabilities,
            IEnumerable<ModelSize> sizes,
            ModelStatistics statistics,
            Uri officialLink)
        {
            ModelName = !string.IsNullOrWhiteSpace(modelName) 
                ? modelName 
                : throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
            
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Capabilities = capabilities?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(capabilities));
            Sizes = sizes?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(sizes));
            Statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
            OfficialLink = officialLink ?? throw new ArgumentNullException(nameof(officialLink));
        }

        public bool SupportsCapability(ModelCapability capability)
        {
            return Capabilities.Contains(capability);
        }

        public bool HasSize(ModelSize size)
        {
            return Sizes.Contains(size);
        }

        public ModelSize GetRecommendedSize(PerformanceRequirement performanceRequirement)
        {
            if (!Sizes.Any())
                throw new InvalidOperationException("No sizes available for this model");

            return performanceRequirement switch
            {
                PerformanceRequirement.Maximum => Sizes.OrderByDescending(s => s.ParameterCount).First(),
                PerformanceRequirement.Balanced => Sizes.OrderBy(s => Math.Abs(s.ParameterCount - 7_000_000_000)).First(), // Prefer ~7B models
                PerformanceRequirement.Minimal => Sizes.OrderBy(s => s.ParameterCount).First(),
                _ => throw new ArgumentOutOfRangeException(nameof(performanceRequirement))
            };
        }
    }

    /// <summary>
    /// Represents model capabilities
    /// </summary>
    public sealed class ModelCapability : IEquatable<ModelCapability>
    {
        public string Name { get; }
        
        // Predefined capabilities
        public static readonly ModelCapability Tools = new("tools");
        public static readonly ModelCapability Vision = new("vision");
        public static readonly ModelCapability Embedding = new("embedding");
        public static readonly ModelCapability Thinking = new("thinking");
        public static readonly ModelCapability Chat = new("chat");
        public static readonly ModelCapability Code = new("code");

        public ModelCapability(string name)
        {
            Name = !string.IsNullOrWhiteSpace(name) 
                ? name.ToLowerInvariant() 
                : throw new ArgumentException("Capability name cannot be null or empty", nameof(name));
        }

        public bool Equals(ModelCapability? other)
        {
            return other != null && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ModelCapability);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString() => Name;

        public static bool operator ==(ModelCapability? left, ModelCapability? right)
        {
            return left?.Equals(right) ?? right is null;
        }

        public static bool operator !=(ModelCapability? left, ModelCapability? right)
        {
            return !(left == right);
        }

        public static implicit operator ModelCapability(string name) => new(name);
    }

    /// <summary>
    /// Represents model size with parameter count
    /// </summary>
    public sealed class ModelSize : IEquatable<ModelSize>, IComparable<ModelSize>
    {
        public string SizeString { get; }
        public long ParameterCount { get; }
        public int QuantizationSize { get; }

        public ModelSize(string sizeString)
        {
            SizeString = !string.IsNullOrWhiteSpace(sizeString) 
                ? sizeString.ToLowerInvariant() 
                : throw new ArgumentException("Size string cannot be null or empty", nameof(sizeString));
            
            (ParameterCount, QuantizationSize) = ParseSizeString(sizeString);
        }

        private static (long parameterCount, int quantizationSize) ParseSizeString(string sizeString)
        {
            var size = sizeString.ToLowerInvariant().Trim();
            
            if (size.EndsWith("b"))
            {
                var numberPart = size[..^1];
                if (decimal.TryParse(numberPart, out var billions))
                {
                    var parameterCount = (long)(billions * 1_000_000_000);
                    var quantizationSize = (int)(billions * 1000); // Convert to millions for quantization
                    return (parameterCount, quantizationSize);
                }
            }
            else if (size.EndsWith("m"))
            {
                var numberPart = size[..^1];
                if (decimal.TryParse(numberPart, out var millions))
                {
                    var parameterCount = (long)(millions * 1_000_000);
                    var quantizationSize = (int)millions;
                    return (parameterCount, quantizationSize);
                }
            }

            throw new ArgumentException($"Unable to parse size string: {sizeString}", nameof(sizeString));
        }

        public bool Equals(ModelSize? other)
        {
            return other != null && ParameterCount == other.ParameterCount;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as ModelSize);
        }

        public override int GetHashCode()
        {
            return ParameterCount.GetHashCode();
        }

        public int CompareTo(ModelSize? other)
        {
            return other == null ? 1 : ParameterCount.CompareTo(other.ParameterCount);
        }

        public override string ToString() => SizeString;

        public static bool operator ==(ModelSize? left, ModelSize? right)
        {
            return left?.Equals(right) ?? right is null;
        }

        public static bool operator !=(ModelSize? left, ModelSize? right)
        {
            return !(left == right);
        }

        public static bool operator <(ModelSize? left, ModelSize? right)
        {
            return left?.CompareTo(right) < 0;
        }

        public static bool operator >(ModelSize? left, ModelSize? right)
        {
            return left?.CompareTo(right) > 0;
        }

        public static bool operator <=(ModelSize? left, ModelSize? right)
        {
            return left?.CompareTo(right) <= 0;
        }

        public static bool operator >=(ModelSize? left, ModelSize? right)
        {
            return left?.CompareTo(right) >= 0;
        }

        public static implicit operator ModelSize(string sizeString) => new(sizeString);
    }

    /// <summary>
    /// Model usage and update statistics
    /// </summary>
    public sealed class ModelStatistics
    {
        [Required]
        public long PullCount { get; }
        
        [Required]
        public int TagCount { get; }
        
        [Required]
        public string LastUpdated { get; }

        public ModelStatistics(string pullCountString, string tagCountString, string lastUpdated)
        {
            PullCount = ParsePullCount(pullCountString);
            TagCount = ParseTagCount(tagCountString);
            LastUpdated = !string.IsNullOrWhiteSpace(lastUpdated) 
                ? lastUpdated 
                : throw new ArgumentException("Last updated cannot be null or empty", nameof(lastUpdated));
        }

        public ModelStatistics(long pullCount, int tagCount, string lastUpdated)
        {
            PullCount = pullCount >= 0 
                ? pullCount 
                : throw new ArgumentException("Pull count cannot be negative", nameof(pullCount));
            
            TagCount = tagCount >= 0 
                ? tagCount 
                : throw new ArgumentException("Tag count cannot be negative", nameof(tagCount));
            
            LastUpdated = !string.IsNullOrWhiteSpace(lastUpdated) 
                ? lastUpdated 
                : throw new ArgumentException("Last updated cannot be null or empty", nameof(lastUpdated));
        }

        private static long ParsePullCount(string pullCountString)
        {
            if (string.IsNullOrWhiteSpace(pullCountString))
                throw new ArgumentException("Pull count cannot be null or empty", nameof(pullCountString));

            var normalized = pullCountString.ToLowerInvariant().Replace(",", "").Replace(".", "");
            
            if (normalized.EndsWith("m"))
            {
                var numberPart = normalized[..^1];
                if (decimal.TryParse(numberPart, out var millions))
                {
                    return (long)(millions * 1_000_000);
                }
            }
            else if (normalized.EndsWith("k"))
            {
                var numberPart = normalized[..^1];
                if (decimal.TryParse(numberPart, out var thousands))
                {
                    return (long)(thousands * 1_000);
                }
            }
            else if (long.TryParse(normalized, out var exact))
            {
                return exact;
            }

            throw new ArgumentException($"Unable to parse pull count: {pullCountString}", nameof(pullCountString));
        }

        private static int ParseTagCount(string tagCountString)
        {
            if (string.IsNullOrWhiteSpace(tagCountString))
                throw new ArgumentException("Tag count cannot be null or empty", nameof(tagCountString));

            if (int.TryParse(tagCountString.Trim(), out var count))
            {
                return count;
            }

            throw new ArgumentException($"Unable to parse tag count: {tagCountString}", nameof(tagCountString));
        }

        /// <summary>
        /// Gets the pull count formatted as a human-readable string
        /// </summary>
        public string GetFormattedPullCount()
        {
            return PullCount switch
            {
                >= 1_000_000_000 => $"{PullCount / 1_000_000_000.0:F1}B",
                >= 1_000_000 => $"{PullCount / 1_000_000.0:F1}M",
                >= 1_000 => $"{PullCount / 1_000.0:F1}K",
                _ => PullCount.ToString()
            };
        }
    }

    /// <summary>
    /// Performance requirement levels for model size recommendation
    /// </summary>
    public enum PerformanceRequirement
    {
        /// <summary>
        /// Prioritize resource efficiency over performance
        /// </summary>
        Minimal,
        
        /// <summary>
        /// Balance between performance and resource usage
        /// </summary>
        Balanced,
        
        /// <summary>
        /// Prioritize maximum performance regardless of resource usage
        /// </summary>
        Maximum
    }
}
