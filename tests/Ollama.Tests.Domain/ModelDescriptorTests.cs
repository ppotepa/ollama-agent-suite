// Test file to demonstrate the numeric ModelSize and ModelStatistics
using Ollama.Domain.Models;
using Ollama.Domain.Services;
using Ollama.Infrastructure.Repositories;

namespace Ollama.Domain.Tests
{
    public class ModelDescriptorTests
    {
        public static void TestModelSizeQuantization()
        {
            // Test ModelSize with quantization
            var size7b = new ModelSize("7b");
            var size270m = new ModelSize("270m");
            var size1_5b = new ModelSize("1.5b");

            Console.WriteLine("=== ModelSize Tests ===");
            Console.WriteLine($"7b: ParameterCount={size7b.ParameterCount:N0}, QuantizationSize={size7b.QuantizationSize}");
            Console.WriteLine($"270m: ParameterCount={size270m.ParameterCount:N0}, QuantizationSize={size270m.QuantizationSize}");
            Console.WriteLine($"1.5b: ParameterCount={size1_5b.ParameterCount:N0}, QuantizationSize={size1_5b.QuantizationSize}");
            Console.WriteLine();
        }

        public static void TestModelStatisticsNumeric()
        {
            // Test ModelStatistics with numeric parsing
            var stats1 = new ModelStatistics("58.9M", "35", "1 month ago");
            var stats2 = new ModelStatistics("1.4M", "3", "1 week ago");
            var stats3 = new ModelStatistics("100.7M", "93", "8 months ago");

            Console.WriteLine("=== ModelStatistics Tests ===");
            Console.WriteLine($"58.9M: PullCount={stats1.PullCount:N0}, TagCount={stats1.TagCount}, Formatted={stats1.GetFormattedPullCount()}");
            Console.WriteLine($"1.4M: PullCount={stats2.PullCount:N0}, TagCount={stats2.TagCount}, Formatted={stats2.GetFormattedPullCount()}");
            Console.WriteLine($"100.7M: PullCount={stats3.PullCount:N0}, TagCount={stats3.TagCount}, Formatted={stats3.GetFormattedPullCount()}");
            Console.WriteLine();
        }

        public static void TestModelRepository()
        {
            var repository = new OllamaModelRepository();
            var allModels = repository.GetAllModels();

            Console.WriteLine("=== Model Repository Tests ===");
            Console.WriteLine($"Total models: {allModels.Count}");

            // Find models by size range using numeric comparison
            var smallModels = repository.FindBySizeRange(maxSize: new ModelSize("7b"));
            var largeModels = repository.FindBySizeRange(minSize: new ModelSize("70b"));

            Console.WriteLine($"Small models (≤7B): {smallModels.Count}");
            Console.WriteLine($"Large models (≥70B): {largeModels.Count}");

            // Show a specific model's numeric properties
            var llama31 = repository.FindByName("llama3.1");
            if (llama31 != null)
            {
                Console.WriteLine($"\nLlama 3.1 details:");
                Console.WriteLine($"  Pull count: {llama31.Statistics.PullCount:N0} ({llama31.Statistics.GetFormattedPullCount()})");
                Console.WriteLine($"  Tag count: {llama31.Statistics.TagCount}");
                Console.WriteLine($"  Sizes: {string.Join(", ", llama31.Sizes.Select(s => $"{s.SizeString} ({s.QuantizationSize})"))}");
            }
        }

        public static void RunAllTests()
        {
            TestModelSizeQuantization();
            TestModelStatisticsNumeric();
            TestModelRepository();
        }
    }
}
