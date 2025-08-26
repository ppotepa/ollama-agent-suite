🟦 What the application is

A modular agent runner built with strong SRP and SOLID principles.

Designed for clean layering:

Domain → pure contracts (interfaces, entities, strategies).

Application → orchestration logic, use-cases, services.

Infrastructure → adapters for agents, tools, file system, etc.

Interface → CLI or HTTP front-end.

Bootstrap → DI container and wiring.

It’s basically the glue between human prompts and agent-driven reasoning/execution, ensuring each responsibility is well-separated and testable.

🟩 What the application does

Accepts a query from the user (e.g., via CLI).

Wraps it into an ExecutionContext (contains query, metadata, session id).

Passes it to the Strategy Orchestrator, which chooses a mode via the Mode Registry.

Executes the query using one of three strategy modes:

🔹 Single-Query Mode

Uses one agent.

Minimal orchestration.

Example: “Summarize this text.”

🔹 Collaborative Mode

Uses at least two agents (e.g., a thinker to reason, a coder to apply changes).

Can involve tool adapters (CLI, repo ops, etc.).

Example: “Figure out how to update this project and then make the change.”

🔹 Intelligent Mode

Has a thinking agent that dynamically builds an execution tree.

Decides when to swap or activate other agents.

More autonomous, flexible orchestration.

Example: “Debug this system and fix what’s broken.”

🟨 Why it matters

Traceability: reasoning steps and actions are recorded in an execution tree.

Extensibility: add new agents, tools, or orchestration modes without changing existing core logic.

Testability: domain & application code are free of I/O; infrastructure is pluggable.

SRP enforced: thinking, execution, orchestration, and context management are all separate.

👉 In short:
This app is a framework to orchestrate intelligent agents in different collaboration styles (single, team, or dynamic), while keeping the architecture clean, extensible, and easy to maintain.

Do you want me to also draw you a high-level diagram (like a flowchart or layered architecture in Mermaid) so you have a visual picture of how it works?


# 0) Solution layout

```
OllamaAgentSuite.sln
src/
  Ollama.Domain/
    Agents/
      IAgent.cs
      ICommandExecutorPort.cs
      IRepositoryPort.cs
    Execution/
      ExecutionNode.cs
      ExecutionNodeType.cs
    Strategies/
      StrategyType.cs
      ExecutionContext.cs
      IModeStrategy.cs

  Ollama.Application/
    Modes/
      SingleQueryMode.cs
      CollaborativeMode.cs
      IntelligentMode.cs
      ModeRegistry.cs
    Orchestrator/
      StrategyOrchestrator.cs
    Services/
      ExecutionTreeBuilder.cs
      CollaborationContextService.cs
      AgentSwitchService.cs

  Ollama.Infrastructure/
    Agents/
      UniversalAgentAdapter.cs
    IO/
      // Filesystem, Git, etc. (adapters)
    Tools/
      // CLI/PowerShell/Web adapters
    Discovery/
      // Model/agent discovery
    State/
      // Caches, repository state
    Logging/
      // Optional logging setup

  Ollama.Interface.Cli/
    Program.cs

  Ollama.Bootstrap/
    Composition/
      ServiceRegistration.cs  // DI wiring

config/
  appsettings.json

docs/
  ARCHITECTURE.md
  EXECUTION_FLOW.md
  STRATEGY_MODES.md

tests/
  Ollama.Tests.Domain/
  Ollama.Tests.Application/
  Ollama.Tests.Infrastructure/
```

# 1) Create projects & references (commands)

```bash
dotnet new sln -n OllamaAgentSuite
mkdir src tests

# libraries
dotnet new classlib -n Ollama.Domain -o src/Ollama.Domain
dotnet new classlib -n Ollama.Application -o src/Ollama.Application
dotnet new classlib -n Ollama.Infrastructure -o src/Ollama.Infrastructure
dotnet new classlib -n Ollama.Bootstrap -o src/Ollama.Bootstrap

# console interface
dotnet new console -n Ollama.Interface.Cli -o src/Ollama.Interface.Cli

# tests (xUnit)
dotnet new xunit -n Ollama.Tests.Domain -o tests/Ollama.Tests.Domain
dotnet new xunit -n Ollama.Tests.Application -o tests/Ollama.Tests.Application
dotnet new xunit -n Ollama.Tests.Infrastructure -o tests/Ollama.Tests.Infrastructure

# add to solution
dotnet sln OllamaAgentSuite.sln add src/**/**.csproj tests/**/**.csproj

# project references
dotnet add src/Ollama.Application/Ollama.Application.csproj reference src/Ollama.Domain/Ollama.Domain.csproj
dotnet add src/Ollama.Infrastructure/Ollama.Infrastructure.csproj reference src/Ollama.Domain/Ollama.Domain.csproj
dotnet add src/Ollama.Bootstrap/Ollama.Bootstrap.csproj reference src/Ollama.Domain/Ollama.Domain.csproj
dotnet add src/Ollama.Bootstrap/Ollama.Bootstrap.csproj reference src/Ollama.Application/Ollama.Application.csproj
dotnet add src/Ollama.Bootstrap/Ollama.Bootstrap.csproj reference src/Ollama.Infrastructure/Ollama.Infrastructure.csproj
dotnet add src/Ollama.Interface.Cli/Ollama.Interface.Cli.csproj reference src/Ollama.Bootstrap/Ollama.Bootstrap.csproj

# test references
dotnet add tests/Ollama.Tests.Domain/Ollama.Tests.Domain.csproj reference src/Ollama.Domain/Ollama.Domain.csproj
dotnet add tests/Ollama.Tests.Application/Ollama.Tests.Application.csproj reference src/Ollama.Application/Ollama.Application.csproj
dotnet add tests/Ollama.Tests.Application/Ollama.Tests.Application.csproj reference src/Ollama.Domain/Ollama.Domain.csproj
dotnet add tests/Ollama.Tests.Infrastructure/Ollama.Tests.Infrastructure.csproj reference src/Ollama.Infrastructure/Ollama.Infrastructure.csproj
dotnet add tests/Ollama.Tests.Infrastructure/Ollama.Tests.Infrastructure.csproj reference src/Ollama.Domain/Ollama.Domain.csproj
```

# 2) Domain (pure, SRP)

## Agents/IAgent.cs

```csharp
namespace Ollama.Domain.Agents;

public interface IAgent
{
    string Answer(string prompt);
    string Think(string prompt);
    object Plan(string prompt);
    object Act(string instruction);
}

public interface ICommandExecutorPort
{
    CommandResult Run(string command, string? workingDirectory = null);
}

public record CommandResult(bool Success, string StdOut = "", string StdErr = "");
```

## Agents/IRepositoryPort.cs

```csharp
namespace Ollama.Domain.Agents;

public interface IRepositoryPort
{
    IEnumerable<string> List(string path);
    // Add other repo ops as ports when needed
}
```

## Execution/ExecutionNodeType.cs

```csharp
namespace Ollama.Domain.Execution;

public enum ExecutionNodeType
{
    UserQuery,
    InterceptorAnalysis,
    CommandExecution,
    AgentResponse,
    FinalResult
}
```

## Execution/ExecutionNode.cs

```csharp
using System.Collections.Generic;

namespace Ollama.Domain.Execution;

public sealed class ExecutionNode
{
    public ExecutionNodeType NodeType { get; }
    public string Content { get; }
    public Dictionary<string, object> Metadata { get; } = new();
    public List<ExecutionNode> Children { get; } = new();
    public ExecutionNode? Parent { get; private set; }

    public ExecutionNode(ExecutionNodeType type, string content)
    {
        NodeType = type;
        Content = content;
    }

    public void AddChild(ExecutionNode child)
    {
        child.Parent = this;
        Children.Add(child);
    }
}
```

## Strategies/StrategyType.cs & ExecutionContext.cs & IModeStrategy.cs

```csharp
namespace Ollama.Domain.Strategies;

public enum StrategyType
{
    SingleQuery,
    Collaborative,
    Intelligent
}

public sealed class ExecutionContext
{
    public string Query { get; }
    public string? SessionId { get; }
    public Dictionary<string, object> Metadata { get; } = new();
    public Dictionary<string, object> Intermediate { get; } = new();

    public ExecutionContext(string query, string? sessionId = null, Dictionary<string, object>? metadata = null)
    {
        Query = query;
        SessionId = sessionId;
        if (metadata != null)
            foreach (var kv in metadata) Metadata[kv.Key] = kv.Value;
    }
}

public interface IModeStrategy
{
    StrategyType Type { get; }
    bool CanHandle(ExecutionContext ctx);
    Dictionary<string, object> Execute(ExecutionContext ctx);
}
```

# 3) Application (use-cases, no I/O)

## Services/ExecutionTreeBuilder.cs

```csharp
using Ollama.Domain.Execution;

namespace Ollama.Application.Services;

public sealed class ExecutionTreeBuilder
{
    private ExecutionNode? _root;
    private ExecutionNode? _cursor;

    public ExecutionTreeBuilder Begin(string query)
    {
        _root = new ExecutionNode(ExecutionNodeType.UserQuery, query);
        _cursor = _root;
        return this;
    }

    public ExecutionTreeBuilder AddAnalysis(string content)
        => Add(ExecutionNodeType.InterceptorAnalysis, content);

    public ExecutionTreeBuilder AddCommand(string content)
        => Add(ExecutionNodeType.CommandExecution, content);

    public ExecutionTreeBuilder AddResponse(string content)
        => Add(ExecutionNodeType.AgentResponse, content);

    public ExecutionTreeBuilder Finish(string result)
        => Add(ExecutionNodeType.FinalResult, result);

    private ExecutionTreeBuilder Add(ExecutionNodeType type, string content)
    {
        var node = new ExecutionNode(type, content);
        _cursor?.AddChild(node);
        _cursor = node;
        return this;
    }

    public ExecutionNode? Snapshot() => _root;

    public bool Done()
    {
        if (_cursor == null) return false;
        return _cursor.Children.Any(c => c.NodeType == ExecutionNodeType.FinalResult);
    }

    public string Result()
    {
        if (_root is null) return string.Empty;
        var stack = new Stack<ExecutionNode>();
        stack.Push(_root);
        while (stack.Count > 0)
        {
            var n = stack.Pop();
            if (n.NodeType == ExecutionNodeType.FinalResult) return n.Content;
            foreach (var c in n.Children) stack.Push(c);
        }
        return string.Empty;
    }
}
```

## Services/CollaborationContextService.cs

```csharp
namespace Ollama.Application.Services;

public sealed class CollaborationContextService
{
    private readonly List<string> _notes = new();
    private readonly List<object> _commands = new();

    public string Workdir { get; private set; } = ".";

    public CollaborationContextService Start(string query, int maxSteps = 5)
    {
        _notes.Clear();
        _commands.Clear();
        return this;
    }

    public void RecordNote(string note) => _notes.Add(note);
    public void RecordCommand(object result) => _commands.Add(result);

    public Dictionary<string, object> Finish() => new()
    {
        ["notes"] = _notes.ToArray(),
        ["commands"] = _commands.ToArray()
    };
}
```

## Services/AgentSwitchService.cs

```csharp
namespace Ollama.Application.Services;

public sealed class AgentSwitchService
{
    private readonly Dictionary<string, object> _registry = new();

    public void Register(string role, object agent) => _registry[role] = agent;

    public T Resolve<T>(string role) where T : class
        => _registry.TryGetValue(role, out var ag) ? ag as T ?? throw new InvalidCastException(role)
                                                   : throw new KeyNotFoundException($"Agent role '{role}' not registered");
}
```

## Modes/SingleQueryMode.cs

```csharp
using Ollama.Domain.Agents;
using Ollama.Domain.Strategies;

namespace Ollama.Application.Modes;

public sealed class SingleQueryMode : IModeStrategy
{
    private readonly IAgent _agent;
    public SingleQueryMode(IAgent agent) => _agent = agent;
    public StrategyType Type => StrategyType.SingleQuery;

    public bool CanHandle(ExecutionContext ctx)
        => !ctx.Metadata.ContainsKey("mode") || (string)ctx.Metadata["mode"] == "single";

    public Dictionary<string, object> Execute(ExecutionContext ctx)
        => new()
        {
            ["mode"] = "single_query",
            ["answer"] = _agent.Answer(ctx.Query)
        };
}
```

## Modes/CollaborativeMode.cs

```csharp
using Ollama.Application.Services;
using Ollama.Domain.Agents;
using Ollama.Domain.Strategies;

namespace Ollama.Application.Modes;

public sealed class CollaborativeMode : IModeStrategy
{
    private readonly IAgent _thinker;
    private readonly IAgent _coder;
    private readonly CollaborationContextService _ctx;
    private readonly AgentSwitchService _switcher;
    private readonly ICommandExecutorPort? _cmd;

    public CollaborativeMode(
        IAgent thinker,
        IAgent coder,
        CollaborationContextService ctx,
        AgentSwitchService switcher,
        ICommandExecutorPort? cmdPort = null)
    {
        _thinker = thinker;
        _coder = coder;
        _ctx = ctx;
        _switcher = switcher;
        _cmd = cmdPort;
    }

    public StrategyType Type => StrategyType.Collaborative;

    public bool CanHandle(ExecutionContext ctx)
        => ctx.Metadata.TryGetValue("mode", out var m) && (string)m == "collaborative";

    public Dictionary<string, object> Execute(ExecutionContext ctx)
    {
        var cctx = _ctx.Start(ctx.Query, maxSteps: ctx.Metadata.TryGetValue("max_steps", out var ms) ? Convert.ToInt32(ms) : 5);
        var note = _thinker.Think(ctx.Query);
        _ctx.RecordNote(note);

        if (_cmd is not null)
        {
            var res = _cmd.Run("echo collaborative-step", cctx.Workdir);
            _ctx.RecordCommand(res);
        }

        return _ctx.Finish();
    }
}
```

## Modes/IntelligentMode.cs

```csharp
using Ollama.Application.Services;
using Ollama.Domain.Strategies;

namespace Ollama.Application.Modes;

public sealed class IntelligentMode : IModeStrategy
{
    private readonly object _thinker; // keep generic; only needs a Think method
    private readonly AgentSwitchService _switcher;
    private readonly ExecutionTreeBuilder _tree;

    public IntelligentMode(object thinker, AgentSwitchService switcher, ExecutionTreeBuilder tree)
    {
        _thinker = thinker;
        _switcher = switcher;
        _tree = tree;
    }

    public StrategyType Type => StrategyType.Intelligent;

    public bool CanHandle(ExecutionContext ctx)
        => ctx.Metadata.TryGetValue("mode", out var m) && ((string)m == "intelligent" || (string)m == "auto");

    public Dictionary<string, object> Execute(ExecutionContext ctx)
    {
        var think = _thinker.GetType().GetMethod("Think");
        var analysis = think is not null ? think.Invoke(_thinker, new object[] { ctx.Query })?.ToString() ?? "" : $"Plan for: {ctx.Query}";

        _tree.Begin(ctx.Query)
             .AddAnalysis(analysis!)
             .Finish($"Completed: {ctx.Query}");

        return new()
        {
            ["mode"] = "intelligent",
            ["result"] = _tree.Result()
        };
    }
}
```

## Modes/ModeRegistry.cs

```csharp
using Ollama.Domain.Strategies;

namespace Ollama.Application.Modes;

public sealed class ModeRegistry
{
    private readonly Dictionary<StrategyType, IModeStrategy> _strategies;

    public ModeRegistry(IEnumerable<IModeStrategy> strategies)
        => _strategies = strategies.ToDictionary(s => s.Type, s => s);

    public IModeStrategy Pick(ExecutionContext ctx)
    {
        if (ctx.Metadata.TryGetValue("mode", out var m))
        {
            var wanted = (string)m;
            var match = _strategies.Values.FirstOrDefault(s => s.Type.ToString().Equals(wanted, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match;
        }

        var first = _strategies.Values.FirstOrDefault(s => s.CanHandle(ctx));
        return first ?? _strategies[StrategyType.SingleQuery];
    }
}
```

## Orchestrator/StrategyOrchestrator.cs

```csharp
using Ollama.Domain.Strategies;

namespace Ollama.Application.Orchestrator;

public sealed class StrategyOrchestrator
{
    private readonly Modes.ModeRegistry _registry;
    private readonly Dictionary<string, Dictionary<string, object>> _sessions = new();

    public StrategyOrchestrator(Modes.ModeRegistry registry) => _registry = registry;

    public async Task<string> ExecuteQueryAsync(string query, string? mode = null, Dictionary<string, object>? metadata = null)
    {
        var sid = Guid.NewGuid().ToString("N");
        var ctx = new ExecutionContext(query, sid, metadata ?? new Dictionary<string, object>());
        if (!string.IsNullOrEmpty(mode)) ctx.Metadata["mode"] = mode!;
        var strategy = _registry.Pick(ctx);
        var result = await Task.Run(() => strategy.Execute(ctx));
        _sessions[sid] = new() { ["status"] = "completed", ["result"] = result };
        return sid;
    }

    public Dictionary<string, object> GetSession(string sessionId)
        => _sessions.TryGetValue(sessionId, out var s) ? s : new() { ["status"] = "unknown" };
}
```

# 4) Infrastructure (adapters behind ports)

## Agents/UniversalAgentAdapter.cs

```csharp
using Ollama.Domain.Agents;

namespace Ollama.Infrastructure.Agents;

public sealed class UniversalAgentAdapter : IAgent
{
    private readonly string _model;
    private readonly bool _streaming;

    public UniversalAgentAdapter(string modelName, bool streaming = true)
    {
        _model = modelName;
        _streaming = streaming;
    }

    public string Answer(string prompt) => $"[{_model}] answer: {prompt}";
    public string Think(string prompt)  => $"[{_model}] think: {prompt}";
    public object Plan(string prompt)   => new { steps = Array.Empty<object>() };
    public object Act(string instruction) => new { ok = true, instruction };
}
```

> Add more adapters for filesystem, git, shell, etc., each implementing a small port from `Ollama.Domain.Agents`.

# 5) Bootstrap (DI wiring)

## Composition/ServiceRegistration.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using Ollama.Application.Modes;
using Ollama.Application.Orchestrator;
using Ollama.Application.Services;
using Ollama.Domain.Agents;
using Ollama.Domain.Strategies;
using Ollama.Infrastructure.Agents;

namespace Ollama.Bootstrap.Composition;

public static class ServiceRegistration
{
    public static IServiceCollection AddOllamaServices(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<ExecutionTreeBuilder>();
        services.AddSingleton<AgentSwitchService>();
        services.AddSingleton<CollaborationContextService>();

        // Agents (adapters)
        services.AddSingleton<IAgent>(sp => new UniversalAgentAdapter("qwen2.5:14b"));           // thinker as default IAgent
        services.AddSingleton<IAgent>(sp => new UniversalAgentAdapter("deepseek-coder:6.7b"));   // coder also IAgent

        // Named agents via factory (simple)
        services.AddSingleton<Func<string, IAgent>>(sp => role =>
        {
            var agents = sp.GetServices<IAgent>().ToList();
            return role switch
            {
                "thinker" => agents[0],
                "coder"   => agents.Count > 1 ? agents[1] : agents[0],
                _ => agents[0]
            };
        });

        // Modes
        services.AddSingleton<IModeStrategy>(sp => new SingleQueryMode(sp.GetRequiredService<Func<string, IAgent>>()("thinker")));
        services.AddSingleton<IModeStrategy>(sp => new CollaborativeMode(
            thinker: sp.GetRequiredService<Func<string, IAgent>>()("thinker"),
            coder:   sp.GetRequiredService<Func<string, IAgent>>()("coder"),
            ctx:     sp.GetRequiredService<CollaborationContextService>(),
            switcher:sp.GetRequiredService<AgentSwitchService>(),
            cmdPort: null /* plug real ICommandExecutorPort adapter here */
        ));
        services.AddSingleton<IModeStrategy>(sp => new IntelligentMode(
            thinker: sp.GetRequiredService<Func<string, IAgent>>()("thinker"),
            switcher: sp.GetRequiredService<AgentSwitchService>(),
            tree: sp.GetRequiredService<ExecutionTreeBuilder>()
        ));

        // Registry & Orchestrator
        services.AddSingleton<ModeRegistry>(sp => new ModeRegistry(sp.GetServices<IModeStrategy>()));
        services.AddSingleton<StrategyOrchestrator>();

        return services;
    }
}
```

# 6) CLI entry

## Ollama.Interface.Cli/Program.cs

```csharp
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

var sid = await orchestrator.ExecuteQueryAsync(query, mode: modeArg);
var session = orchestrator.GetSession(sid);

Console.WriteLine($"Session: {sid}");
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(session, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
```

# 7) appsettings.json (optional)

`config/appsettings.json`

```json
{
  "Agents": {
    "Thinker": "qwen2.5:14b",
    "Coder": "deepseek-coder:6.7b"
  },
  "Modes": {
    "Default": "single"
  }
}
```

> If you want to bind from config, inject `IConfiguration` in `ServiceRegistration` and pick agent names from here.

# 8) Tests (sketches)

* **Ollama.Tests.Domain**: `ExecutionTreeBuilder` traversal and `ExecutionNode` parenting.
* **Ollama.Tests.Application**:

  * `ModeRegistry_PicksExplicitMode()`
  * `SingleQueryMode_AnswersWithAgent()`
  * `CollaborativeMode_RecordsNoteAndOptionalCommand()`
  * `IntelligentMode_BuildsTreeAndReturnsFinalResult()`
* **Ollama.Tests.Infrastructure**:

  * `UniversalAgentAdapter_ImplementsIAgent()`.

Example (xUnit) for `SingleQueryMode`:

```csharp
using Ollama.Application.Modes;
using Ollama.Domain.Agents;
using Ollama.Domain.Strategies;
using Xunit;

public class SingleQueryModeTests
{
    private sealed class FakeAgent : IAgent
    {
        public string Answer(string prompt) => "A:" + prompt;
        public string Think(string prompt) => "T:" + prompt;
        public object Plan(string prompt) => new { };
        public object Act(string instruction) => new { };
    }

    [Fact]
    public void Execute_ReturnsAnswer()
    {
        var mode = new SingleQueryMode(new FakeAgent());
        var ctx = new ExecutionContext("Ping");
        var result = mode.Execute(ctx);
        Assert.Equal("single_query", result["mode"]);
        Assert.Equal("A:Ping", result["answer"]);
    }
}
```

# 9) How to extend (Open/Closed)

* **New mode**: create a class implementing `IModeStrategy`, add it to DI in `ServiceRegistration`.
* **New agent**: add a new adapter implementing `IAgent` and register + role-map in the `Func<string, IAgent>` factory.
* **New tool**: define a small port in `Ollama.Domain.Agents` and implement an adapter in `Ollama.Infrastructure`.

# 10) Running

```bash
dotnet build
dotnet run --project src/Ollama.Interface.Cli -- "Write a README for my repo" --mode single
dotnet run --project src/Ollama.Interface.Cli -- "Add a file + run a script" --mode collaborative
dotnet run --project src/Ollama.Interface.Cli -- "Do dynamic steps" --mode intelligent
```

---

that’s the whole C# translation — same responsibilities, same boundaries, just idiomatic .NET with `Microsoft.Extensions.DependencyInjection` and a tidy solution layout. if you want, i can also spit out the exact files as a ready-to-build zip in this C# structure.
