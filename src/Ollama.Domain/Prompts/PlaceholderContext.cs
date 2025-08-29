using System.Collections.Generic;

namespace Ollama.Domain.Prompts
{
    /// <summary>
    /// Context for passing dynamic placeholder values to decorators
    /// </summary>
    public class PlaceholderContext
    {
        public string SessionId { get; set; } = string.Empty;
        public Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
        
        /// <summary>
        /// Add a placeholder value
        /// </summary>
        public PlaceholderContext AddValue(string placeholder, object value)
        {
            Values[placeholder] = value;
            return this;
        }
        
        /// <summary>
        /// Get a placeholder value
        /// </summary>
        public T? GetValue<T>(string placeholder) where T : class
        {
            return Values.TryGetValue(placeholder, out var value) ? value as T : null;
        }
        
        /// <summary>
        /// Get a placeholder value with default
        /// </summary>
        public T GetValueOrDefault<T>(string placeholder, T defaultValue)
        {
            if (Values.TryGetValue(placeholder, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
    }
}
