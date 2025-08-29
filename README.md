# Ollama Agent Suite

A comprehensive .NET 9.0 framework for orchestrating intelligent LLM agents with clean architecture principles, designed for backend development assistance and AI-powered automation.

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    CLEAN ARCHITECTURE                      │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐                                           │
│  │ Interface   │  CLI Program                              │
│  │   Layer     │  → Entry Point                           │
│  └─────────────┘  → Argument Parsing                      │
│         │         → User Interaction                      │
│         ▼                                                 │
│  ┌─────────────┐                                           │
│  │ Application │  Orchestrator                            │
│  │   Layer     │  → Strategy Selection                     │
│  └─────────────┘  → Execution Coordination                │
│         │         → Mode Registry                         │
│         ▼                                                 │
│  ┌─────────────┐                                           │
│  │   Domain    │  Pure Business Logic git                    │
│  │   Layer     │  → Agents & Strategies                   │
│  └─────────────┘  → Execution Models                      │
│         │         → Tool Contracts                        │
│         ▼                                                 │
│  ┌─────────────┐                                           │
│  │Infrastructure│ External Integrations                   │
│  │   Layer     │  → Ollama Client                         │
│  └─────────────┘  → File System Tools                     │
│         │         → Session Management                    │
│         ▼                                                 │
│  ┌─────────────┐                                           │
│  │ Bootstrap   │  Dependency Injection                     │
│  │   Layer     │  → Service Registration                   │
│  └─────────────┘  → Configuration Setup                   │
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
├── python_subsystem/               # Python integration layer
├── cache/                          # Session-based caching
└── docs/                           # Technical documentation
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

- Conservative, backend-focused approach
- Comprehensive validation and risk assessment  
- Specific development guidance
- Extensive error handling and recovery
- Detailed execution logging and tracing

### Session-Scoped File System

```
Session Management Architecture:
┌─────────────────────────────────────┐
│ Session ID: [UUID]                  │
├─────────────────────────────────────┤
│ Working Directory: cache/[session]/ │
│ Isolated Workspace                  │
│ Tool State Management               │
│ Cursor Navigation Support          │
└─────────────────────────────────────┘
```

### Advanced Tool System

The framework includes a comprehensive tool ecosystem:

#### File System Tools
- **FileSystemAnalyzer**: Enhanced directory analysis with parent folder support
- **DirectoryListTool**: Advanced directory listing with sorting and filtering
- **File Operations**: Read, write, copy, move, delete with session boundaries
- **CursorNavigationTool**: Smart directory navigation with state preservation

#### Analysis Tools
- **CodeAnalyzer**: Source code analysis and insights
- **MathEvaluator**: Mathematical computation support
- **ExternalCommandExecutor**: Safe external command execution

#### Network Tools
- **GitHubRepositoryDownloader**: Repository cloning and management
- **DownloadTool**: File downloading with session isolation

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

| Command | Short | Description |
|---------|-------|-------------|
| `query <text>` | `-q` | Process a query using pessimistic strategy |
| `--verbose` | | Enable detailed execution logging |
| `--no-cache` | `-nc` | Clear cache before execution |
| `--help` | `-h` | Display usage information |

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
│ 2. Session Initialization              │
│    → Generate UUID session ID          │
│    → Create isolated workspace         │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│ 3. Strategy Selection                   │
│    → Apply Pessimistic Strategy Only   │
│    → Load system prompts               │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│ 4. Tool Orchestration                  │
│    → FileSystemAnalyzer execution      │
│    → CodeAnalyzer for insights         │
│    → Generate comprehensive report     │
└─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────┐
│ 5. Output Generation                   │
│    → Structured analysis results       │
│    → Actionable recommendations        │
│    → Session state preservation        │
└─────────────────────────────────────────┘
```

## Development Environment

### Prerequisites

- .NET 9.0 SDK
- Ollama server running on localhost:11434
- Python 3.9+ (for subsystem integration)
- PowerShell (Windows) or Bash (Linux/macOS)

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
- Each layer has a distinct responsibility
- Tools are focused on single capabilities
- Services handle specific concerns

### Strategy Pattern
- Pluggable execution strategies
- **Current**: Pessimistic-only configuration
- **Future**: Extensible to multiple strategies

### Repository Pattern
- Tool repository for dynamic tool management
- Session-scoped tool state
- Configurable tool capabilities

### Factory Pattern
- Service factory for dependency injection
- Tool factory for dynamic instantiation
- Session factory for isolated workspaces

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

- Execution time tracking for all operations
- Memory usage monitoring during large file operations
- Network request timing and retry logic
- Session resource utilization tracking

## Security Considerations

### Session Isolation
- All file operations are scoped to session directories
- No access to system files outside session boundaries
- Automatic cleanup of temporary resources

### Input Validation
- Path traversal protection
- Command injection prevention
- Resource limit enforcement
- Safe external command execution

### Network Security
- Configurable timeout limits
- Retry policies with exponential backoff
- HTTPS enforcement for external downloads
- Request size limitations

## Performance Optimization

### File System Operations
- Asynchronous I/O for large directory scanning
- Configurable depth limits for recursive operations
- Memory-efficient file enumeration
- Alternative analysis methods for different scenarios

### Tool Execution
- Parallel tool execution where safe
- Caching of expensive operations
- Lazy loading of tool dependencies
- Resource pooling for network operations

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

## Contributing

### Code Standards
- Follow SOLID principles
- Implement comprehensive unit tests
- Use structured logging throughout
- Document all public APIs
- Maintain session isolation

### Testing Requirements
- Unit tests for all business logic
- Integration tests for tool implementations
- End-to-end tests for complete workflows
- Performance tests for large operations

---

## License

This project is licensed under the MIT License. See LICENSE file for details.

## Support

For questions and support:
- Check the docs/ directory for detailed technical documentation
- Review logs in cache/[session-id]/ for debugging information
- Examine tool attribute metadata for usage guidance

The Strategic Agent analyzes each query and determines the optimal execution path:

```
Query Analysis Flow:
User Input → Intent Recognition → Risk Assessment → Strategy Selection → Tool Planning → Execution
```

### Multi-Modal Tool Integration
The system seamlessly integrates various tool types:

#### Internal Tool Categories
- **File System Operations**: Read, write, analyze files within session boundaries
- **Code Analysis Tools**: Syntax checking, pattern detection, architecture review
- **Generation Tools**: Code generation, documentation creation, configuration files
- **Command Execution**: Shell commands executed within isolated session environment
- **Network Operations**: HTTP requests, API calls, data retrieval (session-scoped)

#### Python Subsystem Integration
```
┌─────────────────┐    HTTP API    ┌─────────────────┐
│   .NET Core     │ ←────────────→ │ Python FastAPI  │
│ Strategic Agent │                │   Subsystem     │
└─────────────────┘                └─────────────────┘
        │                                    │
        │                              ┌─────────────┐
        │                              │ Ollama      │
        └──────────────────────────────│ Client      │
                                       │ (Python)    │
                                       └─────────────┘
```

### Session Lifecycle Management

#### Session Creation
```csharp
// Automatic session initialization
string sessionId = Guid.NewGuid().ToString();
sessionFileSystem.InitializeSession(sessionId);
conversationHistory = new List<ConversationTurn>();
```

#### Session Isolation
```
Security Enforcement:
├── Path Validation: Prevents directory traversal
├── Command Sandboxing: Working directory locked to session
├── File Access Control: Only session directory accessible  
├── Network Restrictions: Outbound calls logged and monitored
└── Resource Limits: CPU/Memory usage tracked per session
```

#### Session Cleanup
```csharp
// Automatic cleanup after session timeout
public void CleanupSession(string sessionId)
{
    sessionFileSystem.CleanupSession(sessionId);
    conversationHistory.Clear();
    toolExecutionLogs.Archive(sessionId);
}
```

## Advanced Features

### JSON-Driven Tool Orchestration
All tool execution follows a strict JSON contract:

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
Tools are automatically discovered and registered at startup:

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

### Strategic Decision Making
The Pessimistic Strategy implements comprehensive risk assessment:

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

## Development Guidelines

### Adding New Tools
1. Implement the `ITool` interface:
```csharp
public class CustomAnalysisTool : ITool
{
    public string Name => "custom_analysis";
    public string Description => "Performs custom analysis on input data";
    public ToolCapability[] Capabilities => new[] { 
        ToolCapability.Analysis, 
        ToolCapability.Reporting 
    };
    
    public async Task<string> ExecuteAsync(Dictionary<string, string> parameters, string sessionId)
    {
        // Implementation with session-aware operations
        var sessionPath = sessionFileSystem.GetSessionPath(sessionId);
        // ... tool logic ...
        return result;
    }
}
```

2. Tools are automatically discovered via reflection
3. Follow session isolation principles
4. Implement comprehensive error handling

### Extending Strategies
Create new strategies by implementing `IAgentStrategy`:

```csharp
public class CustomStrategy : IAgentStrategy
{
    public string GetSystemPrompt() { /* Strategy-specific prompt */ }
    public async Task<string> ProcessResponseAsync(string llmResponse) { /* Custom processing */ }
}
```

### Testing Guidelines
- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test tool execution with real sessions
- **Session Tests**: Verify isolation and security boundaries
- **Strategy Tests**: Validate strategic decision-making logic

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
- **Tool Caching**: Frequently used tool results cached per session
- **Response Streaming**: Large LLM responses streamed for better UX
- **Parallel Execution**: Independent tools executed concurrently
- **Connection Pooling**: HTTP connections reused across tool calls

## Troubleshooting

### Common Issues

#### Session Directory Not Found
```
Error: Session directory not accessible
Solution: Verify cache directory permissions and disk space
Check: sessionFileSystem.InitializeSession() was called
```

#### Tool Execution Timeout
```
Error: Tool execution exceeded timeout limit
Solution: Increase timeout in configuration or optimize tool logic
Check: Tool is properly handling async operations
```

#### LLM Communication Failure
```
Error: Failed to communicate with Ollama service
Solution: Verify Ollama is running on http://localhost:11434
Check: Required models (llama3.1:8b-instruct) are installed
```

### Debug Mode
Enable detailed logging for troubleshooting:

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

## Security Features

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
- **Operation Logging**: All tool executions logged with timestamps
- **Session Tracking**: Complete audit trail for security review
- **Error Documentation**: Detailed error logs for troubleshooting
- **Performance Metrics**: Resource usage tracking per session

## Contributing

### Development Setup
1. Clone the repository
2. Install .NET 9.0 SDK
3. Install required Python packages: `pip install -r python_subsystem/requirements.txt`
4. Install Ollama and pull required models
5. Run tests: `dotnet test OllamaAgentSuite.sln`

### Code Standards
- **Clean Architecture**: Maintain strict layer separation
- **SOLID Principles**: Follow single responsibility and dependency inversion
- **Session Safety**: All new tools must respect session boundaries
- **Error Handling**: Comprehensive exception handling required
- **Documentation**: Update docs for any new features or changes

### Pull Request Guidelines
1. All tests must pass
2. Code coverage must not decrease
3. Follow established naming conventions
4. Include comprehensive unit tests for new features
5. Update documentation for any API changes

## Key Benefits

### For Developers
- **Traceability**: Complete execution history and reasoning steps recorded
- **Extensibility**: Easy addition of new tools and strategies
- **Testability**: Clean architecture enables comprehensive testing
- **Maintainability**: Clear separation of concerns and SOLID principles

### For Operations
- **Security**: Complete session isolation prevents system compromise
- **Monitoring**: Comprehensive logging and audit capabilities
- **Scalability**: Session-based architecture supports concurrent operations
- **Reliability**: Pessimistic strategy ensures conservative, safe execution

### For Organizations
- **Risk Mitigation**: Conservative approach with extensive validation
- **Compliance**: Complete audit trails for regulatory requirements
- **Cost Control**: Resource limits prevent runaway operations
- **Productivity**: Intelligent automation with human oversight

## License

[Add your license information here]

## Support and Documentation

- **Comprehensive Documentation**: `docs/DOCUMENTATION.md`
- **Architecture Overview**: `docs/architecture.md`
- **API Reference**: Generated from code documentation
- **Examples**: `RUN-SCRIPTS.md` for usage examples
- **Troubleshooting**: See troubleshooting section above

---

**Built with Clean Architecture, SOLID principles, and security-first design for enterprise-grade AI orchestration.**
