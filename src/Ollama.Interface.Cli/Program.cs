using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ollama.Bootstrap.Composition;
using Ollama.Bootstrap.Configuration;
using Ollama.Application.Orchestrator;
using Ollama.Domain.Configuration;
using Ollama.Domain.Services;
using Ollama.Infrastructure.Services;
using Ollama.Infrastructure.Clients;
using Ollama.Infrastructure.Agents;
using Ollama.Infrastructure.Interactive;
using Ollama.Domain.Agents;
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

// Check for new schema system demo
if (args.Length > 0 && args[0] == "schema-system-demo")
{
    Console.WriteLine("\n🎯 Running Generic Response Schema System Demo...\n");
    SchemaSystemDemo.DemonstrateSchemaSystem();
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
    
    // Update prompt path to be relative to solution root
    var promptPath = Path.Combine(solutionRoot, "prompts");
    builder.Configuration["PromptConfiguration:PromptBasePath"] = promptPath;
    Console.WriteLine($"📁 Setting prompt path to: {promptPath}");

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
        
        Console.WriteLine("  - Adding interactive mode services...");
        builder.Services.AddInteractiveMode();
        
        // Register IAgent interface for interactive mode
        builder.Services.AddSingleton<IAgent>(provider => 
            provider.GetRequiredService<StrategicAgent>());
        Console.WriteLine("  - Interactive mode services added successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  - Failed during service registration: {ex.Message}");
        throw;
    }

    Console.WriteLine("🏗️ Building application...");
    
    using var app = builder.Build();
    
    // Configure interceptor chain for interactive mode
    app.Services.ConfigureInterceptorChain();
    
    Console.WriteLine("✅ Application built successfully");

    // Get logger
    logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Application started successfully");

    // Parse command line arguments
    string? query = null;
    bool verbose = false;
    bool clearCache = false; // New flag for -nc (no cache)
    bool interactiveMode = false; // New flag for interactive mode

    Console.WriteLine("📋 Parsing command line arguments...");

    for (int i = 0; i < args.Length; i++)
    {
        Console.WriteLine($"  Processing arg {i}: {args[i]}");
        
        if ((args[i] == "query" || args[i] == "--query" || args[i] == "-q") && i + 1 < args.Length)
        {
            // The next argument is the actual query text
            query = args[i + 1];
            Console.WriteLine($"  Found query: {query}");
            i++; // Skip next argument since we consumed it
        }
        else if (args[i] == "--verbose")
        {
            verbose = true;
            Console.WriteLine($"  Found verbose flag");
        }
        else if (args[i] == "-nc" || args[i] == "--no-cache")
        {
            clearCache = true;
            Console.WriteLine($"  Found no-cache flag - cache will be cleared");
        }
        else if (args[i] == "--interactive" || args[i] == "-i")
        {
            interactiveMode = true;
            Console.WriteLine($"  Found interactive mode flag");
        }
        else if (args[i] == "--help" || args[i] == "-h")
        {
            Console.WriteLine("🤖 Ollama Agent Suite - Backend Development AI Assistant");
            Console.WriteLine("====================================================");
            Console.WriteLine("Usage: Ollama.Interface.Cli [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  query <text>          The query to process");
            Console.WriteLine("  -q, --query <text>    The query to process (short form)");
            Console.WriteLine("  --verbose             Enable verbose output");
            Console.WriteLine("  -nc, --no-cache       Clear cache before running");
            Console.WriteLine("  -i, --interactive     Start interactive mode");
            Console.WriteLine("  --help, -h            Show this help message");
            Console.WriteLine();
            Console.WriteLine("Strategy Configuration:");
            Console.WriteLine("  This system uses PESSIMISTIC STRATEGY EXCLUSIVELY for all queries.");
            Console.WriteLine("  - Conservative, backend-focused approach");
            Console.WriteLine("  - Provides specific development guidance");
            Console.WriteLine("  - Extensive validation and risk assessment");
            Console.WriteLine("  - No generic responses allowed");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run -- query \"Create user authentication system\"");
            Console.WriteLine("  dotnet run -- query \"Analyze this GitHub repository\" --verbose");
            Console.WriteLine("  dotnet run -- query \"Download and examine code quality\" -nc");
            Console.WriteLine("  dotnet run -- --interactive");
            return 0;
        }
    }

    Console.WriteLine($"📋 Parsed arguments - Query: '{query}', Verbose: {verbose}, ClearCache: {clearCache}, Interactive: {interactiveMode}");

    // Handle interactive mode first
    if (interactiveMode)
    {
        Console.WriteLine("🚀 Starting Interactive Mode...");
        
        // Clear cache if requested in interactive mode
        if (clearCache)
        {
            try
            {
                Console.WriteLine("🧹 No-cache flag specified - clearing cache...");
                var sessionFileSystem = app.Services.GetRequiredService<ISessionFileSystem>();
                sessionFileSystem.ClearEntireCache();
                Console.WriteLine("✅ Cache cleared successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to clear cache: {ex.Message}");
                logger?.LogWarning(ex, "Failed to clear cache on startup");
            }
        }
        
        // Start interactive session
        var sessionHandler = app.Services.GetRequiredService<InteractiveSessionHandler>();
        await sessionHandler.StartInteractiveSessionAsync();
        return 0;
    }

    // Clear cache if -nc flag is specified
    if (clearCache)
    {
        try
        {
            Console.WriteLine("🧹 No-cache flag specified - clearing cache...");
            var sessionFileSystem = app.Services.GetRequiredService<ISessionFileSystem>();
            sessionFileSystem.ClearEntireCache();
            Console.WriteLine("✅ Cache cleared successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to clear cache: {ex.Message}");
            logger?.LogWarning(ex, "Failed to clear cache on startup");
        }
    }
    else
    {
        Console.WriteLine($"🏭 Cache preserved - use -nc flag to clear cache");
    }

    // Use defaults if not provided
    query ??= "Hello world";
    var appSettings = app.Services.GetRequiredService<AppSettings>();

    Console.WriteLine($"📋 Final values - Query: '{query}'");

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
        Console.WriteLine($"📊 Strategy: Pessimistic (Backend Development Focus)");
        Console.WriteLine($"⚠️  System configured for PESSIMISTIC MODE ONLY");
        Console.WriteLine($"💡 Expect: Conservative execution, specific backend guidance, comprehensive validation");
        
        Console.WriteLine("🔄 Executing query...");
        
        // Always use StrategicAgent with pessimistic strategy
        Console.WriteLine($"🧠 Using StrategicAgent with pessimistic strategy...");
        
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
            ["strategy"] = "Strategic (Pessimistic)",
            ["response"] = response,
            ["query"] = query,
            ["timestamp"] = DateTime.UtcNow
        };
        
        // Create session result manually
        session = new Dictionary<string, object>
        {
            ["sessionId"] = sessionId,
            ["strategy"] = "Strategic (Pessimistic)",
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
