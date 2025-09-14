Here’s a cleaned-up, fully valid Markdown version with typos fixed, consistent code fences, and duplicated sections consolidated.

# Ollama Agent Suite

A comprehensive .NET 9.0 framework for orchestrating intelligent LLM agents with clean architecture principles, designed for backend development assistance and AI-powered automation.

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    CLEAN ARCHITECTURE                       │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐                                            │
│  │ Interface   │  CLI Program                               │
│  │   Layer     │  → Entry Point                             │
│  └─────────────┘  → Argument Parsing                        │
│         │         → User Interaction                        │
│         ▼                                                    │
│  ┌─────────────┐                                            │
│  │ Application │  Orchestrator                              │
│  │   Layer     │  → Strategy Selection                      │
│  └─────────────┘  → Execution Coordination                  │
│         │         → Mode Registry                           │
│         ▼                                                    │
│  ┌─────────────┐                                            │
│  │   Domain    │  Pure Business Logic                       │
│  │   Layer     │  → Agents & Strategies                     │
│  └─────────────┘  → Execution Models                        │
│         │         → Tool Contracts                          │
│         ▼                                                    │
│  ┌───────────────┐                                          │
│  │ Infrastructure │  External Integrations                  │
│  │     Layer      │  → Ollama Client                        │
│  └───────────────┘  → File System Tools                     │
│         │         → Session Management                      │
│         ▼                                                    │
│  ┌─────────────┐                                            │
│  │ Bootstrap   │  Dependency Injection                      │
│  │   Layer     │  → Service Registration                    │
│  └─────────────┘  → Configuration Setup                     │
└─────────────────────────────────────────────────────────────┘
```

## Project Structure

```
OllamaAgentSuite/
├── src/
│   ├── Ollama.Domain/               # Core business logic
│   │   ├── Agents/                  # Agent contracts
│   │   ├── Execution/               # Execution models
│   │   ├── Services/                # Core services
│   │   ├── Strategies/              # Strategy patterns
│   │   └── Tools/                   # Tool interfaces
│   │
│   ├── Ollama.Application/          # Use cases & orchestration
│   │   ├── Modes/                   # Execution modes
│   │   ├── Orchestrator/            # Main orchestrator
│   │   └── Services/                # Application services
│   │
│   ├── Ollama.Infrastructure/       # External integrations
│   │   ├── Agents/                  # Agent implementations
│   │   ├── Clients/                 # Ollama client
│   │   ├── Services/                # Infrastructure services
│   │   ├── Strategies/              # Strategy implementations
│   │   └── Tools/                   # Tool implementations
│   │
│   ├── Ollama.Bootstrap/            # DI & configuration
│   │   ├── Composition/             # Service registration
│   │   └── Configuration/           # App configuration
│   │
│   └── Ollama.Interface.Cli/        # Command-line interface
│
├── tests/                           # Comprehensive test suite
│   ├── Ollama.Tests.Domain/
│   ├── Ollama.Tests.Application/
│   └── Ollama.Tests.Infrastructure/
│
├── config/                          # Configuration files
│   ├── appsettings.json
│   └── appsettings.Development.json
│
├── prompts/                         # System prompt templates
├── python_subsystem/                # Python integration layer
├── cache/                           # Session-based caching
└── docs/                            # Technical documentation
```

```
┌─────────────────────────────────────┐
│ Session: f03b7671-581d-4d6b-b521    │
│ ┌─────────────────────────────────┐ │
│ │ All tools operate here ONLY    │ │
│ │ ✓ File operations              │ │
│ │ ✓ Command execution            │ │
│ │ ✓ Downloads & extractions      │ │
│ │ ✓ Temporary file storage       │ │
│ └─────────────────────────────────┘ │
│ ✗ Cannot escape to parent dirs     │
│ ✗ Cannot access other sessions     │
│ ✗ Cannot modify host system        │
└─────────────────────────────────────┘
```

## Core Features

### Execution Strategy: Pessimistic Mode Only

The system is **exclusively configured for pessimistic strategy execution**, providing:

* Conservative, backend-focused approach
* Comprehensive validation and risk assessment
* Specific development guidance
* Extensive error handling and recovery
* Detailed execution logging and tracing

### Session-Scoped File System

```
Session Management Architecture:
┌─────────────────────────────────────┐
│ Session ID: [UUID]                  │
├─────────────────────────────────────┤
│ Working Directory: cache/[session]/ │
│ Isolated Workspace                  │
│ Tool State Management               │
│ Cursor Navigation Support           │
└─────────────────────────────────────┘
```

### Advanced Tool System

The framework includes a comprehensive tool ecosystem:

#### File System Tools

* **FileSystemAnalyzer**: Enhanced directory analysis with parent folder support
* **DirectoryListTool**: Advanced directory listing with sorting and filtering
* **File Operations**: Read, write, copy, move, delete with session boundaries
* **CursorNavigationTool**: Smart directory navigation with state preservation

#### Analysis Tools

* **CodeAnalyzer**: Source code analysis and insights
* **MathEvaluator**: Mathematical computation support
* **ExternalCommandExecutor**: Safe external command execution

#### Network Tools

* **GitHubRepositoryDownloader**: Repository cloning and management
* **DownloadTool**: File downloading with session isolation

### Tool Attribute System

All tools include rich metadata through attribute decoration:

```csharp
[ToolDescription(
    "Analyzes file system structure within session boundaries",
    "Comprehensive analysis with statistical insights",
    "File System Analysis")]
[ToolUsage(
    "Analyze directory structure and generate reports",
    RequiredParameters = new[] { "path" },
    OptionalParameters = new[] { "includeSubdirectories", "maxDepth" },
    ExampleInvocation = "FileSystemAnalyzer with path=\".\"",
    RequiresFileSystem = true,
    SafetyNotes = "Read-only analysis within session boundaries")]
[ToolCapabilities(
    ToolCapability.FileSystemAnalysis | ToolCapability.DirectoryList,
    FallbackStrategy = "Basic directory listing if advanced analysis fails")]
```

## Configuration

### Ollama Settings

```json
{
  "OllamaSettings": {
    "BaseUrl": "http://localhost:11434",
    "DefaultModel": "llama3.1:8b-instruct-q4_K_M",
    "ConnectionTimeout": 30,
    "RequestTimeout": 120,
    "MaxRetries": 3,
    "RetryDelay": 1000
  }
}
```

### Agent Configuration

```json
{
  "AgentSettings": {
    "MaxConcurrentAgents": 5,
    "DefaultAgentTimeout": 60,
    "CollaborationEnabled": true,
    "ExecutionTreeDepth": 10,
    "ClearCacheOnStartup": false
  }
}
```

### Python Subsystem Integration

```json
{
  "PythonSubsystem": {
    "Enabled": true,
    "Path": "python_subsystem",
    "Script": "main.py",
    "Port": 8000,
    "StartupTimeoutSeconds": 10
  }
}
```

## Usage

### Command Line Interface

```bash
# Basic query execution
dotnet run -- query "Analyze the project structure"
dotnet run -- -q "Generate code documentation"

# Advanced options
dotnet run -- -q "Debug the authentication system" --verbose
dotnet run -- query "Refactor the data layer" --no-cache

# Help and configuration
dotnet run -- --help
```

### Available Commands

| Command        | Short | Description                                |
| -------------- | :---: | ------------------------------------------ |
| `query <text>` |  `-q` | Process a query using pessimistic strategy |
| `--verbose`    |       | Enable detailed execution logging          |
| `--no-cache`   | `-nc` | Clear cache before execution               |
| `--help`       |  `-h` | Display usage information                  |

### Example Execution Flow

```
Input:  "Analyze the repository structure and identify potential improvements"

Flow:
┌─────────────────────────────────────────┐
│ 1. CLI Argument Parsing                 │
│    → Extract query and options          │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│ 2. Session Initialization               │
│    → Generate UUID session ID           │
│    → Create isolated workspace          │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│ 3. Strategy Selection                   │
│    → Apply Pessimistic Strategy Only    │
│    → Load system prompts                │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│ 4. Tool Orchestration                   │
│    → FileSystemAnalyzer execution       │
│    → CodeAnalyzer for insights          │
│    → Generate comprehensive report      │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│ 5. Output Generation                    │
│    → Structured analysis results        │
│    → Actionable recommendations         │
│    → Session state preservation         │
└─────────────────────────────────────────┘
```

## Development Environment

### Prerequisites

* .NET 9.0 SDK
* Ollama server running on `localhost:11434`
* Python 3.9+ (for subsystem integration)
* PowerShell (Windows) or Bash (Linux/macOS)

### Build and Run

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Start the CLI
dotnet run --project src/Ollama.Interface.Cli

# Run with development configuration
dotnet run --project src/Ollama.Interface.Cli --environment Development
```

### Project Dependencies

```
Ollama.Interface.Cli
└── Ollama.Bootstrap
    ├── Ollama.Application
    │   └── Ollama.Domain
    └── Ollama.Infrastructure
        └── Ollama.Domain
```

## Key Design Patterns

### Single Responsibility Principle (SRP)

* Each layer has a distinct responsibility
* Tools are focused on single capabilities
* Services handle specific concerns

### Strategy Pattern

* Pluggable execution strategies
* **Current**: Pessimistic-only configuration
* **Future**: Extensible to multiple strategies

### Repository Pattern

* Tool repository for dynamic tool management
* Session-scoped tool state
* Configurable tool capabilities

### Factory Pattern

* Service factory for dependency injection
* Tool factory for dynamic instantiation
* Session factory for isolated workspaces

## Session Management

### Session Lifecycle

```
Session Creation → Tool Execution → State Persistence → Cleanup
      │                 │               │              │
      ▼                 ▼               ▼              ▼
   Generate UUID    Execute Tools   Save Context   Archive Results
   Create Cache     Update State    Log Actions    Clean Temp Files
   Set Working Dir  Track Changes   Store Metadata Optional Retention
```

### Cache Structure

```
cache/
└── [session-id]/
    ├── session_context.json        # Session metadata
    ├── session_info_log.txt        # Execution log
    ├── session_summary.json        # Results summary
    ├── conversation_history.json   # Interaction history
    ├── next_steps.txt              # Recommended actions
    ├── interactions/               # Tool interactions
    ├── tools/                      # Tool-specific data
    └── [downloaded-content]/       # Session downloads
```

## Extensibility

### Adding New Tools

```csharp
[ToolDescription("Custom tool description", "Detailed explanation", "Category")]
[ToolUsage("Primary use case", RequiredParameters = new[] { "param1" })]
[ToolCapabilities(ToolCapability.Custom, FallbackStrategy = "Safe fallback")]
public class CustomTool : AbstractTool
{
    public override string Name => "CustomTool";
    public override string Description => "Tool description";
    public override IEnumerable<string> Capabilities => new[] { "custom:capability" };

    public override async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

### Service Registration

```csharp
// In ServiceRegistration.cs
services.AddTransient<CustomTool>();
toolRepository.RegisterTool(new CustomTool(sessionScope, logger));
```

## Logging and Monitoring

### Structured Logging

```csharp
Logger.LogInformation("Tool execution started: {ToolName} for session {SessionId}",
    toolName, sessionId);
Logger.LogWarning("Fallback method used: {Method} due to {Reason}",
    fallbackMethod, reason);
Logger.LogError(ex, "Tool execution failed: {ToolName}", toolName);
```

### Performance Monitoring

* Execution time tracking for all operations
* Memory usage monitoring during large file operations
* Network request timing and retry logic
* Session resource utilization tracking

## Security Considerations

### Session Isolation

* All file operations are scoped to session directories
* No access to system files outside session boundaries
* Automatic cleanup of temporary resources

### Input Validation

* Path traversal protection
* Command injection prevention
* Resource limit enforcement
* Safe external command execution

### Network Security

* Configurable timeout limits
* Retry policies with exponential backoff
* HTTPS enforcement for external downloads
* Request size limitations

### Session Boundary Enforcement

```
Security Layer Implementation:
┌─────────────────────────────────────────┐
│ Path Validation Layer                   │
│ ├── Absolute path rejection             │
│ ├── Parent directory traversal block    │
│ └── Symbolic link resolution check      │
├─────────────────────────────────────────┤
│ Command Execution Layer                 │
│ ├── Working directory enforcement       │
│ ├── Environment variable sanitization   │
│ └── Command parameter validation        │
├─────────────────────────────────────────┤
│ File System Layer                       │
│ ├── Session-scoped file operations      │
│ ├── Read/Write permission management    │
│ └── Temporary file cleanup automation   │
└─────────────────────────────────────────┘
```

### Audit and Compliance

* **Operation Logging**: All tool executions logged with timestamps
* **Session Tracking**: Complete audit trail for security review
* **Error Documentation**: Detailed error logs for troubleshooting
* **Performance Metrics**: Resource usage tracking per session

## Performance Considerations

### Resource Management

```
Session Resource Limits:
├── Memory: 512MB per session
├── CPU: 25% utilization cap
├── Disk: 1GB storage limit per session
├── Network: Rate-limited outbound requests
└── Timeout: 60 minutes maximum session duration
```

### Optimization Techniques

* **File System Operations**: Async I/O, configurable recursion depth, efficient enumeration
* **Tool Execution**: Parallel where safe, cache expensive ops, lazy-load deps, connection pooling
* **Response Streaming**: Stream large LLM responses for better UX

## Troubleshooting

### Common Issues

**Connection to Ollama Failed**

```bash
# Check Ollama service status
ollama serve

# Verify model availability
ollama list

# Test connection
curl http://localhost:11434/api/tags
```

**Session Directory Errors**

```bash
# Clear cache directory
dotnet run -- --no-cache

# Check permissions
ls -la cache/

# Manual cleanup
rm -rf cache/*
```

**Tool Execution Failures**

```bash
# Enable verbose logging
dotnet run -- -q "test query" --verbose

# Check configuration
cat config/appsettings.json

# Verify Python subsystem
cd python_subsystem && python main.py
```

### Debug Mode

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Ollama.Infrastructure": "Debug",
      "Ollama.Application": "Debug"
    }
  }
}
```

## Advanced Features

### JSON-Driven Tool Orchestration

```json
{
  "reasoning": "Analysis of the user's request and why this approach is optimal",
  "action": "specific_tool_name",
  "parameters": {
    "param1": "value1",
    "param2": "value2"
  },
  "next_steps": ["Action 1", "Action 2", "Action 3"],
  "risks": ["Risk 1", "Risk 2"],
  "response": "Current status and findings"
}
```

### Reflection-Based Tool Discovery

```csharp
public class ToolRepository : IToolRepository
{
    public void DiscoverAndRegisterTools()
    {
        var toolTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(ITool).IsAssignableFrom(t) && !t.IsInterface)
            .ToList();

        foreach (var toolType in toolTypes)
        {
            var tool = (ITool)Activator.CreateInstance(toolType);
            RegisterTool(tool);
        }
    }
}
```

### Strategic Decision Making (Pessimistic Strategy)

```csharp
public class PessimisticStrategy : IAgentStrategy
{
    public string GetSystemPrompt()
    {
        return @"
        You are a backend development expert using pessimistic analysis.
        - Always assume worst-case scenarios for risk assessment
        - Provide specific, actionable backend development steps
        - Include comprehensive validation for all recommendations
        - Focus on production-ready, enterprise-grade solutions
        ";
    }
}
```

## Contributing

### Development Setup

1. Clone the repository
2. Install .NET 9.0 SDK
3. Install required Python packages: `pip install -r python_subsystem/requirements.txt`
4. Install Ollama and pull required models
5. Run tests: `dotnet test OllamaAgentSuite.sln`

### Code Standards

* Follow SOLID principles
* Implement comprehensive unit tests
* Use structured logging throughout
* Document all public APIs
* Maintain session isolation

### Testing Requirements

* Unit tests for all business logic
* Integration tests for tool implementations
* End-to-end tests for complete workflows
* Performance tests for large operations

### Pull Request Guidelines

1. All tests must pass
2. Code coverage must not decrease
3. Follow established naming conventions
4. Include comprehensive unit tests for new features
5. Update documentation for any API changes

## Key Benefits

### For Developers

* **Traceability**: Complete execution history and reasoning steps recorded
* **Extensibility**: Easy addition of new tools and strategies
* **Testability**: Clean architecture enables comprehensive testing
* **Maintainability**: Clear separation of concerns and SOLID principles

### For Operations

* **Security**: Complete session isolation prevents system compromise
* **Monitoring**: Comprehensive logging and audit capabilities
* **Scalability**: Session-based architecture supports concurrent operations
* **Reliability**: Pessimistic strategy ensures conservative, safe execution

### For Organizations

* **Risk Mitigation**: Conservative approach with extensive validation
* **Compliance**: Complete audit trails for regulatory requirements
* **Cost Control**: Resource limits prevent runaway operations
* **Productivity**: Intelligent automation with human oversight

## License

This project is licensed under the MIT License. See the **LICENSE** file for details.

## Support and Documentation

* **Comprehensive Documentation**: `docs/DOCUMENTATION.md`
* **Architecture Overview**: `docs/architecture.md`
* **API Reference**: Generated from code documentation
* **Examples**: `RUN-SCRIPTS.md` for usage examples
* **Troubleshooting**: See the Troubleshooting section above

---

**Built with Clean Architecture, SOLID principles, and security-first design for enterprise-grade AI orchestration.**
