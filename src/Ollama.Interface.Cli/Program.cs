using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Ollama.Bootstrap.Composition;
using Ollama.Bootstrap.Configuration;
using Ollama.Application.Orchestrator;
using Ollama.Domain.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Add configuration
builder.Configuration.AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"config/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Add services
builder.Services.AddOllamaConfiguration(builder.Configuration);
builder.Services.AddOllamaServices();

using var app = builder.Build();

var query = args.FirstOrDefault() ?? "Hello world";
var modeArg = args.Skip(1).FirstOrDefault(); // e.g., "single"|"collaborative"|"intelligent"

// Get configuration and apply default mode if no mode specified
var appSettings = app.Services.GetRequiredService<AppSettings>();
var effectiveMode = !string.IsNullOrEmpty(modeArg) ? modeArg : appSettings.DefaultMode;

var orchestrator = app.Services.GetRequiredService<StrategyOrchestrator>();

try
{
    Console.WriteLine($"🤖 OllamaAgentSuite - Processing query: '{query}'");
    if (!string.IsNullOrEmpty(modeArg))
    {
        Console.WriteLine($"📋 Requested mode: {modeArg}");
    }
    else
    {
        Console.WriteLine($"📋 Using default mode: {effectiveMode}");
    }
    
    var sessionId = orchestrator.ExecuteQuery(query, mode: effectiveMode);
    var session = orchestrator.GetSession(sessionId);
    
    if (session != null)
    {
        Console.WriteLine($"\n✅ Session ID: {sessionId}");
        Console.WriteLine($"🎯 Strategy used: {session["strategy"]}");
        
        if (session.ContainsKey("response"))
        {
            Console.WriteLine($"💬 Response: {session["response"]}");
        }
        
        if (session.ContainsKey("analysis"))
        {
            Console.WriteLine($"🧠 Analysis: {session["analysis"]}");
        }
        
        if (session.ContainsKey("implementation"))
        {
            Console.WriteLine($"⚙️ Implementation: {session["implementation"]}");
        }
        
        if (session.ContainsKey("reasoning"))
        {
            Console.WriteLine($"🤔 Reasoning: {session["reasoning"]}");
        }
    }
    else
    {
        Console.WriteLine("❌ Failed to retrieve session");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    return 1;
}

return 0;
