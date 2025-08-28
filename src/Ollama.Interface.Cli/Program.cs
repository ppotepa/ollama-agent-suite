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
using Ollama.Interface.Cli;


// Clear the console at the very start
if (OperatingSystem.IsWindows())
{
    System.Console.Clear();
}
else
{
    // ANSI escape code for clear screen (for Linux/macOS)
    System.Console.Write("\x1b[2J\x1b[H");
}

Console.WriteLine("🚀 Starting OllamaAgentSuite CLI...");
Console.WriteLine($"📝 Command line arguments: {string.Join(" ", args)}");

// Check for schema demo
if (args.Length > 0 && args[0] == "schema-demo")
{
    Console.WriteLine("\n🎯 Running Schema Communication Demo...\n");
    SchemaDemo.DemonstrateSchemas();
    return 0;
}

// Check for schema test with real LLM
if (args.Length > 0 && args[0] == "schema-test")
{
    Console.WriteLine("\n🧠 Testing Real Schema-Based LLM Communication...\n");
    // We'll implement this after setting up the host
}

ILogger? logger = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    Console.WriteLine("📁 Setting up configuration...");
    
    // Get the solution root directory (go up from bin/Debug/net9.0)
    var currentDir = Directory.GetCurrentDirectory();
    var solutionRoot = currentDir;
    
    // Find the solution root by looking for the .sln file
    while (!Directory.GetFiles(solutionRoot, "*.sln").Any() && Directory.GetParent(solutionRoot) != null)
    {
        solutionRoot = Directory.GetParent(solutionRoot)!.FullName;
    }
    
    var configPath = Path.Combine(solutionRoot, "config", "appsettings.json");
    Console.WriteLine($"📁 Looking for config at: {configPath}");
    
    // Add configuration
    builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: true);
    builder.Configuration.AddJsonFile(Path.Combine(solutionRoot, "config", $"appsettings.{builder.Environment.EnvironmentName}.json"), optional: true, reloadOnChange: true);
    
    // Update Python subsystem path to be relative to solution root
    var pythonSubsystemPath = Path.Combine(solutionRoot, "python_subsystem");
    builder.Configuration["PythonSubsystem:Path"] = pythonSubsystemPath;
    Console.WriteLine($"📁 Setting Python subsystem path to: {pythonSubsystemPath}");

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
    string? planning = null;
    bool verbose = false;

    Console.WriteLine("📋 Parsing command line arguments...");

    for (int i = 0; i < args.Length; i++)
    {
        Console.WriteLine($"  Processing arg {i}: {args[i]}");
        
        if ((args[i] == "query" || args[i] == "--query") && i + 1 < args.Length)
        {
            // The next argument is the actual query text
            query = args[i + 1];
            Console.WriteLine($"  Found query: {query}");
            i++; // Skip next argument since we consumed it
        }
        else if (args[i] == "--planning" && i + 1 < args.Length)
        {
            planning = args[i + 1];
            Console.WriteLine($"  Found planning: {planning}");
            i++; // Skip next argument since we consumed it
        }
        else if (args[i] == "--verbose")
        {
            verbose = true;
            Console.WriteLine($"  Found verbose flag");
        }
        else if (args[i] == "--help" || args[i] == "-h")
        {
            Console.WriteLine("Usage: Ollama.Interface.Cli [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  query <text>          The query to process");
            Console.WriteLine("  --planning <type>     Specify the planning type (pessimistic)");
            Console.WriteLine("  --verbose             Enable verbose output");
            Console.WriteLine("  --help, -h            Show this help message");
            return 0;
        }
    }

    Console.WriteLine($"📋 Parsed arguments - Query: '{query}', Planning: '{planning}', Verbose: {verbose}");

    // Use defaults if not provided
    query ??= "Hello world";
    var appSettings = app.Services.GetRequiredService<AppSettings>();
    planning ??= "pessimistic";

    Console.WriteLine($"📋 Final values - Query: '{query}', Planning: '{planning}'");

    Console.WriteLine("🎯 Testing service resolution step by step...");
    
    try
    {
        Console.WriteLine("  ➤ Getting AppSettings...");
        // appSettings already resolved above, but let's test it again
        Console.WriteLine("  ✅ AppSettings resolved");
        
        Console.WriteLine("  ➤ Getting IToolRepository...");
        var toolRepo = app.Services.GetRequiredService<IToolRepository>();
        Console.WriteLine("  ✅ IToolRepository resolved");
        
        Console.WriteLine("  ➤ Getting StrategicAgent...");
        var strategicAgent = app.Services.GetRequiredService<Ollama.Infrastructure.Agents.StrategicAgent>();
        Console.WriteLine("  ✅ StrategicAgent resolved");

        Console.WriteLine($"🤖 OllamaAgentSuite - Processing query: '{query}'");
        Console.WriteLine($"📊 Planning: {planning}");
        
        Console.WriteLine("🔄 Executing query...");
        
        // Always use StrategicAgent since we only have pessimistic strategy now
        Console.WriteLine($"🧠 Using StrategicAgent with {planning} strategy...");
        
        var sessionId = Guid.NewGuid().ToString();
        
        Console.WriteLine($"🚀 Executing strategic query with session: {sessionId}");
        
        // Test schema-based communication if query contains "schema-test"
        string response;
        if (query.Contains("schema-test", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("📡 Using SCHEMA-BASED communication with real LLM...");
            response = await strategicAgent.AnswerWithSchemaAsync(query.Replace("schema-test", "").Trim(), sessionId);
        }
        else
        {
            Console.WriteLine("📞 Using traditional communication...");
            response = strategicAgent.Answer(query, sessionId);
        }
        
        // Create session result manually
        var session = new Dictionary<string, object>
        {
            ["sessionId"] = sessionId,
            ["strategy"] = $"Strategic ({planning})",
            ["response"] = response,
            ["query"] = query,
            ["timestamp"] = DateTime.UtcNow
        };
        
        // Create session result manually
        session = new Dictionary<string, object>
        {
            ["sessionId"] = sessionId,
            ["strategy"] = $"Strategic ({planning})",
            ["response"] = response,
            ["query"] = query,
            ["timestamp"] = DateTime.UtcNow
        };
        
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
