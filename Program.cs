using Ollama.Bootstrap.Composition;
using Ollama.Bootstrap.Configuration;
using Ollama.Infrastructure.Interactive;
using Ollama.Infrastructure.Agents;
using Ollama.Domain.Agents;
using Ollama.Domain.Models;
using Ollama.Domain.Services;
using Ollama.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

// Check command line arguments
var args = Environment.GetCommandLineArgs();

// Handle interactive mode
if (args.Contains("--interactive") || args.Contains("-i"))
{
    await RunInteractiveModeAsync();
    return;
}

// Original test code
await RunTestsAsync();

async Task RunInteractiveModeAsync()
{
    Console.WriteLine("🚀 Initializing Interactive Mode...");
    Console.WriteLine();

    // Build service container with interactive mode
    var builder = Host.CreateApplicationBuilder();
    
    // Get the solution root directory
    var currentDir = Directory.GetCurrentDirectory();
    var solutionRoot = currentDir;
    
    // Find the solution root by looking for the .sln file
    while (!Directory.GetFiles(solutionRoot, "*.sln").Any() && Directory.GetParent(solutionRoot) != null)
    {
        solutionRoot = Directory.GetParent(solutionRoot)!.FullName;
    }
    
    var configPath = Path.Combine(solutionRoot, "config", "appsettings.json");
    
    // Add configuration
    builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: true);
    builder.Configuration.AddJsonFile(Path.Combine(solutionRoot, "config", $"appsettings.{builder.Environment.EnvironmentName}.json"), optional: true, reloadOnChange: true);
    
    // Update Python subsystem path to be relative to solution root
    var pythonSubsystemPath = Path.Combine(solutionRoot, "python_subsystem");
    builder.Configuration["PythonSubsystem:Path"] = pythonSubsystemPath;
    
    // Add core services
    builder.Services.AddOllamaConfiguration(builder.Configuration);
    builder.Services.AddOllamaServices();
    builder.Services.AddInteractiveMode();
    
    // Register IAgent interface 
    builder.Services.AddSingleton<IAgent>(provider => 
        provider.GetRequiredService<StrategicAgent>());
    
    var app = builder.Build();
    var serviceProvider = app.Services;
    
    // Configure interceptor chain
    serviceProvider.ConfigureInterceptorChain();
    
    // Start interactive session
    var sessionHandler = serviceProvider.GetRequiredService<InteractiveSessionHandler>();
    await sessionHandler.StartInteractiveSessionAsync();
}

async Task RunTestsAsync()
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

    // Test ModelStatistics with numeric parsing
    var stats1 = new ModelStatistics("58.9M", "35", "1 month ago");
    var stats2 = new ModelStatistics("1.4M", "3", "1 week ago");
    var stats3 = new ModelStatistics("100.7M", "93", "8 months ago");

    Console.WriteLine("=== ModelStatistics Tests ===");
    Console.WriteLine($"58.9M: PullCount={stats1.PullCount:N0}, TagCount={stats1.TagCount}, Formatted={stats1.GetFormattedPullCount()}");
    Console.WriteLine($"1.4M: PullCount={stats2.PullCount:N0}, TagCount={stats2.TagCount}, Formatted={stats2.GetFormattedPullCount()}");
    Console.WriteLine($"100.7M: PullCount={stats3.PullCount:N0}, TagCount={stats3.TagCount}, Formatted={stats3.GetFormattedPullCount()}");
    Console.WriteLine();

    // Test Model Repository
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

    // Test size comparisons
    Console.WriteLine("\n=== Size Comparison Tests ===");
    var sizes = new[] { new ModelSize("1b"), new ModelSize("7b"), new ModelSize("13b"), new ModelSize("70b") };
    var sortedSizes = sizes.OrderBy(s => s.ParameterCount).ToArray();
    Console.WriteLine($"Sorted by parameter count: {string.Join(" < ", sortedSizes.Select(s => s.SizeString))}");

    Console.WriteLine("\n✅ All tests completed!");

    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("🆕 NEW: Generic Response Schema System Demo");
    Console.WriteLine(new string('=', 80));

    // Demonstrate the new schema system
    // SchemaSystemDemo.DemonstrateSchemaSystem();
    
    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("💡 Tip: Use --interactive or -i to start interactive mode");
    Console.WriteLine(new string('=', 80));
}
