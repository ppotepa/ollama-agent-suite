using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ollama.Bootstrap.Composition;
using Ollama.Application.Modes;
using Ollama.Domain.Strategies;

var builder = Host.CreateApplicationBuilder(args);

// Add logging
builder.Services.AddLogging(configure => configure.AddConsole());

// Add our services
builder.Services.AddOllamaServices();

using var app = builder.Build();

// Test queries
var testQueries = new[]
{
    "What is 2 + 2?",
    "Calculate 15 * 3",
    "What is 100 / 4?",
    "Solve 2 + 2 * 3",
    "Analyze this repository for code improvements: https://github.com/ppotepa/tools"
};

Console.WriteLine("🧠 Testing Intelligent Mode with Math and Repository Analysis\n");

foreach (var query in testQueries)
{
    try
    {
        Console.WriteLine($"🔍 Query: {query}");
        
        // Get the intelligent mode strategy
        var strategies = app.Services.GetServices<IModeStrategy>();
        var intelligentMode = strategies.OfType<IntelligentMode>().FirstOrDefault();
        
        if (intelligentMode != null)
        {
            var executionContext = new ExecutionContext
            {
                Query = query,
                SessionId = Guid.NewGuid().ToString(),
                Metadata = new Dictionary<string, object>()
            };
            
            var result = intelligentMode.Execute(executionContext);
            
            Console.WriteLine($"   Strategy: {result.GetValueOrDefault("strategy", "unknown")}");
            
            if (result.ContainsKey("reasoning"))
            {
                Console.WriteLine($"   Reasoning: {result["reasoning"]}");
            }
            
            if (result.ContainsKey("plan"))
            {
                Console.WriteLine($"   Plan: {result["plan"]}");
            }
            
            if (result.ContainsKey("result"))
            {
                Console.WriteLine($"   Result: {result["result"]}");
            }
        }
        else
        {
            Console.WriteLine("   ❌ IntelligentMode not found");
        }
        
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ❌ Error: {ex.Message}\n");
    }
}

Console.WriteLine("✅ Testing completed!");
return 0;
