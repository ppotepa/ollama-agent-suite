using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ollama.Bootstrap.Composition;
using Ollama.Application.Orchestrator;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddOllamaServices();

using var app = builder.Build();

var query = args.FirstOrDefault() ?? "Hello world";
var modeArg = args.Skip(1).FirstOrDefault(); // e.g., "single"|"collaborative"|"intelligent"
var orchestrator = app.Services.GetRequiredService<StrategyOrchestrator>();

try
{
    Console.WriteLine($"🤖 OllamaAgentSuite - Processing query: '{query}'");
    if (!string.IsNullOrEmpty(modeArg))
    {
        Console.WriteLine($"📋 Requested mode: {modeArg}");
    }
    
    var sessionId = orchestrator.ExecuteQuery(query, mode: modeArg);
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
