using System;

namespace NumericDemo;

public class ModelSize
{
    public long ParameterCount { get; set; }
    public int QuantizationSize { get; set; }
    
    public ModelSize(long parameterCount, int quantizationSize)
    {
        ParameterCount = parameterCount;
        QuantizationSize = quantizationSize;
    }
    
    // Helper method to parse from string like "7B"
    public static ModelSize ParseFromString(string sizeString, string quantString = "4")
    {
        var paramCount = ParseParameterCount(sizeString);
        var quantSize = int.Parse(quantString);
        return new ModelSize(paramCount, quantSize);
    }
    
    private static long ParseParameterCount(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        
        value = value.ToUpperInvariant().Replace("B", "");
        if (decimal.TryParse(value, out var parsed))
        {
            return (long)(parsed * 1_000_000_000); // Convert billions to actual number
        }
        return 0;
    }
    
    public string GetFormattedSize()
    {
        if (ParameterCount >= 1_000_000_000)
        {
            return $"{(decimal)ParameterCount / 1_000_000_000:0.#}B";
        }
        else if (ParameterCount >= 1_000_000)
        {
            return $"{(decimal)ParameterCount / 1_000_000:0.#}M";
        }
        return ParameterCount.ToString();
    }
}

public class ModelStatistics
{
    public long PullCount { get; set; }
    public int TagCount { get; set; }
    
    public ModelStatistics(long pullCount, int tagCount)
    {
        PullCount = pullCount;
        TagCount = tagCount;
    }
    
    // Helper method to parse from string like "58.9M"
    public static ModelStatistics ParseFromStrings(string pullCountString, string tagCountString)
    {
        var pullCount = ParseCount(pullCountString);
        var tagCount = int.Parse(tagCountString);
        return new ModelStatistics(pullCount, tagCount);
    }
    
    private static long ParseCount(string value)
    {
        if (string.IsNullOrEmpty(value)) return 0;
        
        value = value.ToUpperInvariant();
        var multiplier = 1L;
        
        if (value.EndsWith("M"))
        {
            multiplier = 1_000_000;
            value = value.Replace("M", "");
        }
        else if (value.EndsWith("K"))
        {
            multiplier = 1_000;
            value = value.Replace("K", "");
        }
        
        if (decimal.TryParse(value, out var parsed))
        {
            return (long)(parsed * multiplier);
        }
        return 0;
    }
    
    public string GetFormattedPullCount()
    {
        if (PullCount >= 1_000_000)
        {
            return $"{(decimal)PullCount / 1_000_000:0.#}M";
        }
        else if (PullCount >= 1_000)
        {
            return $"{(decimal)PullCount / 1_000:0.#}K";
        }
        return PullCount.ToString();
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Numeric Model Improvements Demo ===\n");
        
        // Demonstrate ModelSize with numeric operations
        Console.WriteLine("1. ModelSize Improvements:");
        var llama7b = ModelSize.ParseFromString("7", "4");
        var llama13b = ModelSize.ParseFromString("13", "8");
        
        Console.WriteLine($"Llama 7B: {llama7b.ParameterCount:N0} parameters, {llama7b.QuantizationSize}-bit quantization");
        Console.WriteLine($"Formatted: {llama7b.GetFormattedSize()}");
        
        Console.WriteLine($"Llama 13B: {llama13b.ParameterCount:N0} parameters, {llama13b.QuantizationSize}-bit quantization");
        Console.WriteLine($"Formatted: {llama13b.GetFormattedSize()}");
        
        // Numeric comparisons now possible
        Console.WriteLine($"\nNumeric Comparisons:");
        Console.WriteLine($"13B is {(double)llama13b.ParameterCount / llama7b.ParameterCount:0.##}x larger than 7B");
        Console.WriteLine($"Combined parameters: {(llama7b.ParameterCount + llama13b.ParameterCount):N0}");
        
        // Demonstrate ModelStatistics with numeric operations
        Console.WriteLine("\n2. ModelStatistics Improvements:");
        var stats1 = ModelStatistics.ParseFromStrings("58.9M", "15");
        var stats2 = ModelStatistics.ParseFromStrings("2.3K", "8");
        
        Console.WriteLine($"Popular model: {stats1.PullCount:N0} pulls, {stats1.TagCount} tags");
        Console.WriteLine($"Formatted: {stats1.GetFormattedPullCount()} pulls");
        
        Console.WriteLine($"New model: {stats2.PullCount:N0} pulls, {stats2.TagCount} tags");
        Console.WriteLine($"Formatted: {stats2.GetFormattedPullCount()} pulls");
        
        // Numeric operations now possible
        Console.WriteLine($"\nNumeric Operations:");
        Console.WriteLine($"Total pulls across both models: {(stats1.PullCount + stats2.PullCount):N0}");
        Console.WriteLine($"Popular model has {stats1.PullCount / stats2.PullCount:0.#}x more pulls");
        Console.WriteLine($"Average tags per model: {(stats1.TagCount + stats2.TagCount) / 2.0:0.#}");
        
        // Performance benefits
        Console.WriteLine("\n3. Performance Benefits:");
        Console.WriteLine("✓ No string parsing at runtime for comparisons");
        Console.WriteLine("✓ Efficient sorting and filtering operations");
        Console.WriteLine("✓ Mathematical operations on model metrics");
        Console.WriteLine("✓ Type safety with compile-time checking");
        
        Console.WriteLine("\nDemonstration complete!");
    }
}
