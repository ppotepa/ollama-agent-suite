using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ollama.Bootstrap.Composition;
using Ollama.Bootstrap.Configuration;
using Ollama.Application.Orchestrator;
using Ollama.Domain.Configuration;
using Ollama.Infrastructure.Services;
using Ollama.Infrastructure.Clients;
using Ollama.Infrastructure.Agents;
using Ollama.Domain.Tools;

Console.WriteLine("🚀 Starting OllamaAgentSuite CLI...");
Console.WriteLine($"📝 Command line arguments: {string.Join(" ", args)}");

ILogger? logger = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    Console.WriteLine("📁 Setting up configuration...");
    
    // Add configuration
    builder.Configuration.AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true);
    builder.Configuration.AddJsonFile($"config/appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

    Console.WriteLine("🔧 Registering services...");
    
    // Add services with detailed logging
    try 
    {
        Console.WriteLine("  - Adding Ollama configuration...");
        builder.Services.AddOllamaConfiguration(builder.Configuration);
        Console.WriteLine("  - Ollama configuration added successfully");
        
        Console.WriteLine("  - Adding Ollama services...");
        builder.Services.AddOllamaServices();
        Console.WriteLine("  - Ollama services added successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  - Failed during service registration: {ex.Message}");
        throw;
    }

    Console.WriteLine("🏗️ Building application...");
    
    using var app = builder.Build();
    
    Console.WriteLine("✅ Application built successfully");

    // Get logger
    logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Application started successfully");

    // Parse command line arguments
    string? query = null;
    string? mode = null;

    Console.WriteLine("📋 Parsing command line arguments...");

    for (int i = 0; i < args.Length; i++)
    {
        Console.WriteLine($"  Processing arg {i}: {args[i]}");
        switch (args[i])
        {
            case "--mode":
            case "-m":
                if (i + 1 < args.Length)
                {
                    mode = args[i + 1];
                    Console.WriteLine($"  Found mode: {mode}");
                    i++; // Skip next argument since we consumed it
                }
                break;
            case "--query":
            case "-q":
                if (i + 1 < args.Length)
                {
                    query = args[i + 1];
                    Console.WriteLine($"  Found query: {query}");
                    i++; // Skip next argument since we consumed it
                }
                break;
            case "--help":
            case "-h":
                Console.WriteLine("Usage: Ollama.Interface.Cli [options]");
                Console.WriteLine("Options:");
                Console.WriteLine("  --mode, -m <mode>     Specify the mode (single, collaborative, intelligent)");
                Console.WriteLine("  --query, -q <query>   Specify the query to process");
                Console.WriteLine("  --help, -h            Show this help message");
                return 0;
        }
    }

    Console.WriteLine($"📋 Parsed arguments - Query: '{query}', Mode: '{mode}'");

    // Use defaults if not provided
    query ??= "Hello world";
    var appSettings = app.Services.GetRequiredService<AppSettings>();
    mode ??= appSettings.DefaultMode;

    Console.WriteLine($"📋 Final values - Query: '{query}', Mode: '{mode}'");

    Console.WriteLine("🎯 Testing service resolution step by step...");
    
    try
    {
        Console.WriteLine("  ➤ Getting AppSettings...");
        // appSettings already resolved above, but let's test it again
        Console.WriteLine("  ✅ AppSettings resolved");
        
        // Only test Python services if we're using intelligent mode
        if (mode?.ToLowerInvariant() == "intelligent")
        {
            Console.WriteLine("  ➤ Getting IPythonSubsystemService...");
            var pythonService = app.Services.GetRequiredService<IPythonSubsystemService>();
            Console.WriteLine("  ✅ IPythonSubsystemService resolved");
            
            Console.WriteLine("  ➤ Getting IPythonLlmClient...");
            var pythonClient = app.Services.GetRequiredService<IPythonLlmClient>();
            Console.WriteLine("  ✅ IPythonLlmClient resolved");
            
            Console.WriteLine("  ➤ Getting IntelligentAgent...");
            var intelligentAgent = app.Services.GetRequiredService<IntelligentAgent>();
            Console.WriteLine("  ✅ IntelligentAgent resolved");
        }
        
        Console.WriteLine("  ➤ Getting IToolRepository...");
        var toolRepo = app.Services.GetRequiredService<IToolRepository>();
        Console.WriteLine("  ✅ IToolRepository resolved");
        
        Console.WriteLine("  ➤ Getting StrategyOrchestrator...");
        var orchestrator = app.Services.GetRequiredService<StrategyOrchestrator>();
        Console.WriteLine("  ✅ StrategyOrchestrator resolved");

        Console.WriteLine($"🤖 OllamaAgentSuite - Processing query: '{query}'");
        Console.WriteLine($"📋 Mode: {mode}");
        
        Console.WriteLine("🔄 Executing query...");
        var sessionId = orchestrator.ExecuteQuery(query, mode: mode);
        Console.WriteLine($"📊 Session ID returned: {sessionId}");
        
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
    catch (Exception serviceEx)
    {
        Console.WriteLine($"❌ Service resolution error: {serviceEx.Message}");
        Console.WriteLine($"❌ Service stack trace: {serviceEx.StackTrace}");
        throw;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error occurred: {ex.Message}");
    Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
    logger?.LogError(ex, "Unhandled exception in CLI application");
    return 1;
}

Console.WriteLine("🎉 Application completed successfully");
return 0;
