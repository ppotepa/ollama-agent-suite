# Ollama Agent Suite - Comprehensive Documentation

## Table of Contents

1. [Overview](#overview)
2. [Core Concepts](#core-concepts)
3. [Architecture](#architecture)
4. [Components](#components)
5. [Communication Protocols](#communication-protocols)
6. [Iterative LLM Interaction Model](#iterative-llm-interaction-model)
7. [Tool Execution Workflow](#tool-execution-workflow)
8. [Tool System](#tool-system)
   - [Tool Architecture: Internal vs External](#tool-architecture-internal-vs-external)
   - [Internal Tools (Reflection-Based Discovery)](#internal-tools-reflection-based-discovery)
   - [External Tools (Widely Available)](#external-tools-widely-available)
   - [Shell Command Integration and External Tool Fallbacks](#shell-command-integration-and-external-tool-fallbacks)
9. [Session Management](#session-management)
10. [Strategic Modes](#strategic-modes)
11. [Configuration](#configuration)
12. [Usage Examples](#usage-examples)
13. [Extension Guide](#extension-guide)
14. [Troubleshooting](#troubleshooting)
15. [System Architecture Summary](#system-architecture-summary-iterative-llm-collaboration)

---

## Current System Configuration: Pessimistic Mode Only + Session Isolation

**⚠️ IMPORTANT: This system is currently configured to use ONLY the Pessimistic Strategy with complete session isolation.**

As documented in the architecture, while the system supports multiple strategies (Pessimistic, Optimistic, Balanced), the current implementation exclusively uses the **Pessimistic Strategy** for all operations. Additionally, the system now enforces **strict session directory isolation** to ensure security.

### Key System Features:

- **Maximum Safety**: Conservative execution with extensive validation
- **Backend Development Focus**: Every response includes specific backend development guidance
- **Consistent Behavior**: Predictable, cautious approach to all queries
- **Risk Mitigation**: Assumes worst-case scenarios and plans comprehensive fallbacks
- **Session Isolation**: Each session operates within `/cache/[sessionId]/` with no escape possible
- **Internal Tools Only**: All tools are internal implementations with no external dependencies

### Session Isolation Security

**Complete Directory Isolation**: 
- Every session operates within its own `/cache/[sessionId]/` directory
- No tool or operation can escape the session boundary, not even one level down
- All file operations, downloads, and command executions are confined to the session space
- Path validation prevents directory traversal attempts (`../`, absolute paths, etc.)
- ExternalCommandExecutor working directory is always set to session-safe paths

**Security Benefits**:
- **Host System Protection**: Sessions cannot access or modify host system files
- **Session Separation**: Different sessions cannot interfere with each other
- **Audit Trail**: All session operations are logged and contained for security review
- **Risk Containment**: Even if tools malfunction, damage is limited to session directory

### Why Pessimistic Mode Only?

1. **Backend Development Emphasis**: The pessimistic strategy is specifically tuned to provide concrete backend development steps
2. **Production Readiness**: Conservative approach suitable for production-level development guidance
3. **Comprehensive Analysis**: Thorough risk assessment and validation before recommending actions
4. **Specific Guidance**: Prohibits generic responses, forcing detailed, actionable recommendations
5. **Security First**: Session isolation ensures all operations are secure and contained

### System Behavior

- **CLI Interface**: All queries automatically use pessimistic strategy
- **No Strategy Selection**: Users cannot choose different strategies (by design)
- **Consistent Output**: All responses follow pessimistic strategy patterns
- **Backend-Focused Prompts**: System prompts emphasize backend development guidance
- **Session-Aware Operations**: All tools receive session context and operate within boundaries

---

## Overview

**Ollama Agent Suite** is a sophisticated AI-powered application framework designed to orchestrate intelligent conversations between Large Language Models (LLMs) and backend systems to solve complex user queries. The system acts as a bridge between human requests and AI-driven problem-solving capabilities, providing a structured, extensible, and highly maintainable solution for automated task execution.

### Key Operating Principles

**🔄 Iterative LLM Interaction**: The system engages in continuous back-and-forth dialogue with the LLM until the original user prompt is completely satisfied. This ensures thorough analysis and comprehensive solutions.

**🔧 Tool Augmentation Strategy**: Since LLMs can only handle programming and reasoning tasks directly, the system supplements them with internal and external tools for real-world operations (file access, network operations, command execution).

**📋 JSON-Driven Orchestration**: All tool execution happens via structured JSON responses from the LLM, with each response containing a "next step" that guides the system's actions.

**✅ Completion-Driven Iteration**: The system continues iterating until the LLM explicitly confirms that the original user prompt has been fully addressed, ensuring no partial or incomplete solutions.

### What It Does

The application accepts user queries of any nature and uses advanced AI agents to:
- **Analyze** the problem and determine the optimal solution approach
- **Plan** multi-step execution strategies
- **Execute** actions using a comprehensive tool ecosystem
- **Communicate** results through strict JSON contracts
- **Learn** from interactions to improve future responses

### Key Value Propositions

- **Universal Problem Solving**: Handles any type of query through intelligent agent orchestration
- **Extensible Tool Ecosystem**: Easily add new capabilities without modifying core logic
- **Strict Contract Compliance**: Ensures reliable AI-backend communication through JSON schemas
- **Session Isolation**: Maintains separate execution environments for concurrent operations
- **Strategic Flexibility**: Multiple execution modes for different complexity levels
- **Comprehensive Traceability**: Full logging and audit trail of all operations

---

## Core Concepts

### 1. AI Agent Orchestration

The system employs sophisticated AI agents that interact with backend systems through well-defined contracts. Each agent is specialized for specific types of reasoning and execution:

- **Strategic Agents**: Primary decision-makers that analyze queries and plan execution
- **Tool-Specialized Agents**: Execute specific operations (code analysis, file operations, etc.)
- **Communication Agents**: Manage AI-to-system interactions through JSON schemas

### 2. Backend Integration Philosophy

The application serves as an intelligent middleware that:
- Receives human queries in natural language
- Translates them into structured execution plans
- Coordinates between AI reasoning and system operations
- Provides actionable backend development guidance
- Maintains strict separation between AI logic and system execution

### 3. Iterative LLM Interaction Model

**The system operates on a fundamental principle: continuous iteration with the LLM until the initial user prompt is completely satisfied.**

#### Core Interaction Loop
The system engages in a persistent back-and-forth dialogue with the LLM:

1. **Initial Prompt Processing**: User query is analyzed by the LLM
2. **Task Assessment**: LLM determines if the prompt is fully satisfied
3. **Tool Execution Decision**: If not complete, LLM identifies required tools/actions
4. **System Tool Execution**: Our system executes the requested tools
5. **Result Integration**: Tool results are fed back to the LLM
6. **Iteration Continuation**: Process repeats until LLM confirms task completion

#### LLM Capability Boundaries
**Important**: LLMs can only handle programming and reasoning tasks directly. They cannot:
- Access file systems
- Download files or repositories
- Execute system commands
- Perform network operations
- Manipulate external resources

#### Tool Augmentation Strategy
**This is why we supplement the LLM with internal and external tools:**

- **Internal Tools**: File system operations, code analysis, mathematical calculations
- **External Tools**: GitHub downloads, repository analysis, command execution
- **System Integration**: Bridging the gap between AI reasoning and real-world operations

#### JSON-Driven Tool Orchestration
**All tool execution happens via structured JSON responses from the LLM:**

```json
{
  "reasoning": "Analysis of what needs to be done",
  "taskComplete": false,
  "nextStep": "Use GitHubDownloader tool to fetch repository",
  "requiresTool": true,
  "tool": "GitHubDownloader",
  "parameters": {
    "repoUrl": "https://github.com/user/repo",
    "sessionId": "session-123"
  }
}
```

**Key Workflow Characteristics:**
- **Next Step Driven**: Every LLM response includes a specific next action
- **Tool Supplementation**: System executes tools the LLM cannot perform
- **Result Feedback**: Tool outputs are immediately fed back to the LLM
- **Completion Tracking**: LLM continuously assesses if the original prompt is satisfied
- **Iterative Refinement**: Process continues until the LLM confirms full completion

### 3. Contract-Based Communication

All interactions between AI agents and backend systems follow strict JSON contracts:

```json
{
  "reasoning": "Step-by-step analysis of the problem",
  "taskComplete": false,
  "stepCompleted": true,
  "nextStep": "Specific executable backend action",
  "requiresTool": true,
  "tool": {
    "name": "GitHubDownloader",
    "parameters": {
      "repoUrl": "https://github.com/user/repo"
    }
  },
  "confidence": 0.85,
  "assumptions": ["List of working assumptions"],
  "risks": ["Potential failure points"],
  "response": "Current status and findings"
}
```

---

## Architecture

The application follows **Clean Architecture** principles with strict layer separation:

```
┌─────────────────────────────────────────────────────────────┐
│                    Interface Layer                          │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │   CLI Client    │  │   Web API       │                 │
│  └─────────────────┘  └─────────────────┘                 │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                         │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │  Orchestrator   │  │  Mode Registry  │                 │
│  └─────────────────┘  └─────────────────┘                 │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │ Planning Service│  │ Session Manager │                 │
│  └─────────────────┘  └─────────────────┘                 │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                    Domain Layer                             │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │    Agents       │  │   Strategies    │                 │
│  └─────────────────┘  └─────────────────┘                 │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │     Tools       │  │  Execution      │                 │
│  └─────────────────┘  └─────────────────┘                 │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                Infrastructure Layer                         │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │ Strategic Agent │  │  Tool Repository│                 │
│  └─────────────────┘  └─────────────────┘                 │
│  ┌─────────────────┐  ┌─────────────────┐                 │
│  │ Ollama Client   │  │ File System     │                 │
│  └─────────────────┘  └─────────────────┘                 │
└─────────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

1. **Interface Layer**: Entry points (CLI, Web API) - handles user input/output
2. **Application Layer**: Orchestration, business logic, use cases
3. **Domain Layer**: Core contracts, entities, business rules (pure, no dependencies)
4. **Infrastructure Layer**: External system adapters, concrete implementations

---

## Components

## Components

### 1. Strategic Agent (`StrategicAgent.cs`)

The core AI orchestration component that:

- **Analyzes user queries** using configurable strategies (Pessimistic, Optimistic, Balanced)
- **Manages conversation state** across multiple interaction rounds
- **Executes tools** based on AI decisions
- **Maintains session isolation** for concurrent operations
- **Provides structured JSON responses** following strict contracts

```csharp
public class StrategicAgent : IAgent
{
    // Core capabilities
    public async Task<string> AnswerAsync(string prompt, string? sessionId = null)
    public async Task<string> AnswerWithSchemaAsync(string prompt, string? sessionId = null)
    
    // Session management
    private void InitializeSession(string sessionId)
    private void SaveConversationState(string sessionId)
    
    // Tool execution
    private string ExecuteTool(string toolName, Dictionary<string, string> parameters, string sessionId)
}
```

### 2. Tool Repository (`ToolRepository.cs`)

Centralized registry for all available tools:

- **Dynamic tool registration** during application startup
- **Capability-based tool discovery** (e.g., find all tools with "code:analyze" capability)
- **Thread-safe operations** using concurrent collections
- **Extensible architecture** for adding new tools

```csharp
public class ToolRepository : IToolRepository
{
    public void RegisterTool(ITool tool)
    public ITool? GetToolByName(string name)
    public IEnumerable<ITool> FindToolsByCapability(string capability)
    public IEnumerable<ITool> GetAllTools()
}
```

### 3. Session File System (`SessionFileSystem.cs`)

Provides isolated execution environments:

- **Session-specific directories** for each conversation
- **Comprehensive logging** of all interactions and artifacts
- **File operation abstractions** for tools to manipulate files safely
- **Automatic cleanup** and session management

### 4. Planning Service (`PlanningService.cs`)

Coordinates complex multi-step operations:

- **Strategy selection** based on query complexity
- **Step-by-step execution planning** with fallback strategies
- **Resource estimation** and optimization
- **Progress tracking** across execution phases

---

## Complete Class Architecture Analysis

### Domain Layer (Pure Contracts & Business Logic)

#### Core Interfaces and Contracts

**`IAgent` Interface** (`src/Ollama.Domain/Agents/IAgent.cs`)
```csharp
public interface IAgent
{
    string Answer(string prompt, string? sessionId = null);
    string Think(string prompt);
    string Think(string prompt, string? sessionId);
    object Plan(string prompt);
    object Plan(string prompt, string? sessionId);
    object Act(string instruction);
}
```
- **Purpose**: Defines the fundamental contract for all AI agents in the system
- **Responsibilities**: 
  - `Answer()`: Primary method for generating responses to user queries
  - `Think()`: Internal reasoning and analysis capabilities
  - `Plan()`: Strategic planning and multi-step operation design
  - `Act()`: Execution of specific instructions or commands
- **Design Philosophy**: Separation of cognitive functions (thinking vs. execution)

**`ICommandExecutorPort` Interface** (`src/Ollama.Domain/Agents/IAgent.cs`)
```csharp
public interface ICommandExecutorPort
{
    CommandResult Run(string command, string? workingDirectory = null);
}
public record CommandResult(bool Success, string StdOut = "", string StdErr = "");
```
- **Purpose**: Port for external command execution (following hexagonal architecture)
- **Isolation**: Keeps domain layer pure by abstracting system dependencies
- **Usage**: Enables agents to execute system commands without direct OS coupling

**`ITool` Interface** (`src/Ollama.Domain/Tools/ITool.cs`)
```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    IEnumerable<string> Capabilities { get; }
    bool RequiresNetwork { get; }
    bool RequiresFileSystem { get; }
    Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default);
    Task<decimal> EstimateCostAsync(ToolContext context);
    Task<bool> DryRunAsync(ToolContext context);
}
```
- **Purpose**: Unified interface for all executable tools in the system
- **Capabilities System**: Self-describing tools with capability tags for dynamic discovery
- **Resource Requirements**: Explicit declaration of system dependencies
- **Cost Estimation**: Built-in cost analysis for resource planning
- **Dry Run Support**: Risk-free execution testing

**`IToolRepository` Interface** (`src/Ollama.Domain/Tools/IToolRepository.cs`)
```csharp
public interface IToolRepository
{
    void RegisterTool(ITool tool);
    ITool? GetToolByName(string name);
    IEnumerable<ITool> FindToolsByCapability(string capability);
    IEnumerable<ITool> GetAllTools();
}
```
- **Purpose**: Central registry and discovery mechanism for tools
- **Registration Pattern**: Runtime tool registration for extensibility
- **Capability-Based Discovery**: Find tools by what they can do, not what they are
- **Type Safety**: Strongly-typed tool retrieval with null safety

#### Strategy and Execution Patterns

**`IModeStrategy` Interface** (`src/Ollama.Domain/Strategies/IModeStrategy.cs`)
```csharp
public interface IModeStrategy
{
    StrategyType Type { get; }
    bool CanHandle(ExecutionContext context);
    Dictionary<string, object> Execute(ExecutionContext context);
}
```
- **Purpose**: Strategy pattern implementation for different execution modes
- **Self-Selection**: Strategies can evaluate their own applicability
- **Context-Driven**: Execution decisions based on rich context information
- **Flexible Return**: Dictionary-based results for varied strategy outputs

**`IAgentStrategy` Interface** (`src/Ollama.Domain/Strategies/IAgentStrategy.cs`)
```csharp
public interface IAgentStrategy
{
    string Name { get; }
    string GetSystemPrompt();
    bool ShouldRetry(int attemptNumber, Exception exception);
}
```
- **Purpose**: Configurable agent behavior strategies (Pessimistic, Optimistic, Balanced)
- **Prompt Engineering**: Strategy-specific system prompts for different AI behaviors
- **Retry Logic**: Intelligent failure recovery based on strategy type
- **Behavioral Customization**: Different risk tolerance and execution approaches

**`ExecutionNode` & `ExecutionNodeType`** (`src/Ollama.Domain/Execution/`)
```csharp
public enum ExecutionNodeType
{
    UserQuery, InterceptorAnalysis, CommandExecution, AgentResponse, FinalResult
}

public class ExecutionNode
{
    public ExecutionNodeType Type { get; }
    public string Content { get; }
    public DateTime Timestamp { get; }
    public List<ExecutionNode> Children { get; }
    public void AddChild(ExecutionNode child);
}
```
- **Purpose**: Tree structure for tracking execution flow and decision points
- **Audit Trail**: Complete record of reasoning and execution steps
- **Hierarchical Tracking**: Parent-child relationships show decision branching
- **Temporal Ordering**: Timestamp-based execution tracking

#### Communication and Service Contracts

**`ISessionFileSystem` Interface** (`src/Ollama.Domain/Services/ISessionFileSystem.cs`)
```csharp
public interface ISessionFileSystem
{
    string GetSessionRoot(string sessionId);
    string GetCurrentDirectory(string sessionId);
    string ChangeDirectory(string sessionId, string relativePath);
    void WriteFile(string sessionId, string relativePath, string content);
    string ReadFile(string sessionId, string relativePath);
    bool FileExists(string sessionId, string relativePath);
    void DeleteFile(string sessionId, string relativePath);
    IEnumerable<string> ListFiles(string sessionId, string? directoryPath = null);
    void ClearSession(string sessionId);
}
```
- **Purpose**: Session-isolated file system operations for secure execution
- **Security Model**: Path validation and session boundary enforcement
- **Abstraction Layer**: Clean separation between business logic and file I/O
- **Session Management**: Automatic cleanup and isolation

**`LLMRequestSchema` & `LLMResponseSchema`** (`src/Ollama.Domain/Models/Communication/`)
```csharp
public class LLMRequestSchema
{
    public string SessionId { get; set; }
    public string UserQuery { get; set; }
    public RequestContext Context { get; set; }
    public List<ToolInfo> AvailableTools { get; set; }
    public string Strategy { get; set; }
    public List<InteractionHistory> PreviousInteractions { get; set; }
}

public class LLMResponseSchema
{
    public AnalysisSection Analysis { get; set; }
    public NextStepSection NextStep { get; set; }
    public ConfidenceSection Confidence { get; set; }
    public ContinuationSection Continuation { get; set; }
}
```
- **Purpose**: Structured communication contracts between AI and backend systems
- **Type Safety**: Strongly-typed schemas prevent communication errors
- **Rich Context**: Comprehensive context passing for informed AI decisions
- **Progressive Disclosure**: Continuation support for multi-step operations

### Application Layer (Orchestration & Use Cases)

#### Strategy Orchestration System

**`StrategyOrchestrator`** (`src/Ollama.Application/Orchestrator/StrategyOrchestrator.cs`)
```csharp
public sealed class StrategyOrchestrator
{
    private readonly ModeRegistry _registry;
    private readonly Dictionary<string, Dictionary<string, object>> _sessions = new();
    
    public string ExecuteQuery(string query, string? mode = null)
    public Dictionary<string, object>? GetSession(string sessionId)
    public IEnumerable<string> GetAllSessionIds()
    public void ClearSession(string sessionId)
}
```
- **Purpose**: Central coordination point for all query execution
- **Strategy Selection**: Intelligent mode selection based on query characteristics
- **Session Management**: Multi-session support with state tracking
- **Execution Flow**: Coordinates between mode selection and actual execution
- **State Persistence**: Maintains session state across multiple interactions

**`ModeRegistry`** (`src/Ollama.Application/Modes/ModeRegistry.cs`)
```csharp
public sealed class ModeRegistry
{
    private readonly Dictionary<StrategyType, IModeStrategy> _strategies;
    
    public ModeRegistry(IEnumerable<IModeStrategy> strategies)
    public IModeStrategy GetStrategy(StrategyType type)
    public IModeStrategy SelectBestStrategy(ExecutionContext context)
    public IEnumerable<StrategyType> GetAvailableStrategyTypes()
}
```
- **Purpose**: Registry and selection mechanism for execution strategies
- **Dynamic Registration**: Runtime strategy registration via dependency injection
- **Best-Fit Selection**: Intelligent strategy selection based on query complexity
- **Fallback Logic**: Graceful degradation to simpler strategies when needed
- **Strategy Enumeration**: Discovery of available execution modes

#### Execution Mode Implementations

**`SingleQueryMode`** (`src/Ollama.Application/Modes/SingleQueryMode.cs`)
```csharp
public sealed class SingleQueryMode : IModeStrategy
{
    public StrategyType Type => StrategyType.SingleQuery;
    public bool CanHandle(ExecutionContext ctx) // Simple queries, direct responses
    public Dictionary<string, object> Execute(ExecutionContext ctx)
}
```
- **Purpose**: Simplest execution mode for straightforward queries
- **Use Cases**: Direct questions, simple calculations, basic information requests
- **Characteristics**: Single agent, minimal orchestration, fast response
- **Decision Logic**: Handles queries that don't require complex reasoning or tool usage

**`CollaborativeMode`** (`src/Ollama.Application/Modes/CollaborativeMode.cs`)
```csharp
public sealed class CollaborativeMode : IModeStrategy
{
    private readonly IAgent _thinker;
    private readonly IAgent _coder;
    private readonly CollaborationContextService _contextService;
    
    public StrategyType Type => StrategyType.Collaborative;
    public bool CanHandle(ExecutionContext ctx) // Multi-step, coding tasks
    public Dictionary<string, object> Execute(ExecutionContext ctx)
}
```
- **Purpose**: Multi-agent collaboration for complex tasks
- **Agent Specialization**: Dedicated thinker and coder agents with specific roles
- **Context Sharing**: Collaborative context service for inter-agent communication
- **Use Cases**: Code analysis, multi-step problem solving, development tasks
- **Coordination**: Orchestrates handoffs between thinking and execution phases

**`IntelligentMode`** (`src/Ollama.Application/Modes/IntelligentMode.cs`)
```csharp
public sealed class IntelligentMode : IModeStrategy
{
    private readonly IAgent _agent;
    private readonly AgentSwitchService _agentSwitchService;
    private readonly ExecutionTreeBuilder _treeBuilder;
    
    public StrategyType Type => StrategyType.Intelligent;
    public bool CanHandle(ExecutionContext ctx) // Complex, adaptive queries
    public Dictionary<string, object> Execute(ExecutionContext ctx)
}
```
- **Purpose**: Most sophisticated mode with adaptive execution and tree building
- **Dynamic Adaptation**: Can switch agents and strategies during execution
- **Execution Tracking**: Complete execution tree for audit and debugging
- **Complex Reasoning**: Handles queries requiring multi-step reasoning and planning
- **Tool Integration**: Seamless integration with tool ecosystem for complex operations

#### Application Services

**`ExecutionTreeBuilder`** (`src/Ollama.Application/Services/ExecutionTreeBuilder.cs`)
```csharp
public sealed class ExecutionTreeBuilder
{
    private ExecutionNode? _root;
    private ExecutionNode? _cursor;
    
    public ExecutionTreeBuilder Begin(string query)
    public ExecutionTreeBuilder AddAnalysis(string content)
    public ExecutionTreeBuilder AddCommand(string content)
    public ExecutionTreeBuilder AddResponse(string content)
    public ExecutionTreeBuilder Finish(string result)
    public ExecutionNode Build()
}
```
- **Purpose**: Fluent builder pattern for execution tree construction
- **State Management**: Maintains cursor position for sequential tree building
- **Type Safety**: Strongly-typed methods for different node types
- **Immutable Result**: Returns complete execution tree for analysis
- **Audit Trail**: Creates comprehensive record of execution flow

**`AgentSwitchService`** (`src/Ollama.Application/Services/AgentSwitchService.cs`)
```csharp
public sealed class AgentSwitchService
{
    private readonly Dictionary<string, object> _registry = new();
    
    public void Register(string role, object agent)
    public T Resolve<T>(string role) where T : class
}
```
- **Purpose**: Dynamic agent registration and role-based resolution
- **Role-Based Access**: Agents accessed by functional role rather than type
- **Runtime Registration**: Flexible agent registration during application startup
- **Type Safety**: Generic resolution with compile-time type checking
- **Service Locator Pattern**: Centralized agent discovery mechanism

**`CollaborationContextService`** (`src/Ollama.Application/Services/CollaborationContextService.cs`)
```csharp
public sealed class CollaborationContextService
{
    // Manages shared context between collaborating agents
    // Handles state transfer and communication protocols
    // Maintains conversation history across agent handoffs
}
```
- **Purpose**: Manages shared state and communication between collaborating agents
- **Context Preservation**: Maintains conversation continuity across agent switches
- **State Synchronization**: Ensures all agents have access to current context
- **Communication Protocol**: Standardized inter-agent communication patterns

### Infrastructure Layer (External Adapters & Implementations)

#### AI Agent Implementations

**`StrategicAgent`** (`src/Ollama.Infrastructure/Agents/StrategicAgent.cs`)
```csharp
public class StrategicAgent : IAgent
{
    private readonly IAgentStrategy _strategy;
    private readonly ISessionFileSystem _sessionFileSystem;
    private readonly IToolRepository _toolRepository;
    private readonly BuiltInOllamaClient _ollamaClient;
    private readonly ILLMCommunicationService _communicationService;
    
    // Core AI interaction methods
    public async Task<string> AnswerAsync(string prompt, string? sessionId = null)
    public async Task<string> AnswerWithSchemaAsync(string prompt, string? sessionId = null)
    
    // Session and conversation management
    private void InitializeSession(string sessionId)
    private void SaveConversationState(string sessionId)
    private void AddConversationEntry(string sessionId, string role, string content)
    
    // Tool execution and orchestration
    private string ExecuteTool(string toolName, Dictionary<string, string> parameters, string sessionId)
    private string? TryAlternativeApproach(string originalTool, Dictionary<string, string> parameters, string sessionId, int retryAttempt)
    
    // LLM communication
    private async Task<string> CallLLMAsync(string prompt, string sessionId)
    private async Task<string> CallLLMWithSchemaAsync(LLMRequestSchema requestSchema, string sessionId)
}
```
- **Purpose**: Primary implementation of the IAgent interface with full strategic capabilities
- **Strategy Pattern**: Uses configurable strategies for different behavioral modes
- **Session Management**: Complete session lifecycle management with state persistence
- **Tool Integration**: Seamless tool execution with retry logic and fallback strategies
- **Communication**: Dual communication modes (traditional and schema-based)
- **State Persistence**: Comprehensive logging and conversation state management
- **Error Handling**: Robust error handling with alternative approaches and retries

**`IntelligentAgent`** (`src/Ollama.Infrastructure/Agents/IntelligentAgent.cs`)
```csharp
public class IntelligentAgent : IAgent
{
    // Specialized implementation for intelligent mode operations
    // Enhanced reasoning capabilities and dynamic tool selection
    // Advanced execution planning and adaptive behavior
}
```
- **Purpose**: Specialized agent implementation for complex reasoning and adaptive execution
- **Enhanced Capabilities**: Advanced reasoning patterns and dynamic decision-making
- **Tool Orchestration**: Sophisticated tool selection and execution planning
- **Adaptive Behavior**: Can modify approach based on intermediate results

**`UniversalAgentAdapter`** (`src/Ollama.Infrastructure/Agents/UniversalAgentAdapter.cs`)
```csharp
public sealed class UniversalAgentAdapter : IAgent
{
    private readonly string _model;
    private readonly bool _streaming;
    
    public UniversalAgentAdapter(string modelName, bool streaming = true)
    public string Answer(string prompt) => $"[{_model}] answer: {prompt}";
    public string Think(string prompt) => $"[{_model}] think: {prompt}";
    public object Plan(string prompt) => new { steps = Array.Empty<object>() };
    public object Act(string instruction) => new { ok = true, instruction };
}
```
- **Purpose**: Lightweight adapter for basic agent functionality during development/testing
- **Model Abstraction**: Wraps different LLM models with consistent interface
- **Development Support**: Simplified implementation for testing and prototyping
- **Configuration**: Model-specific configuration and streaming support

#### Client and Communication Infrastructure

**`BuiltInOllamaClient`** (`src/Ollama.Infrastructure/Clients/BuiltInOllamaClient.cs`)
```csharp
public class BuiltInOllamaClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BuiltInOllamaClient> _logger;
    private readonly OllamaSettings _ollamaSettings;
    
    public async Task<string> ChatAsync(string model, List<(string role, string content)> messages)
    public async Task<string> GenerateAsync(string model, string prompt)
}
```
- **Purpose**: Direct HTTP client for Ollama API communication
- **Protocol Support**: Both chat and generate API endpoints
- **Configuration**: Configurable endpoints, timeouts, and retry logic
- **Error Handling**: Comprehensive error handling and logging
- **Performance**: Optimized HTTP communication with connection pooling
- **JSON Processing**: Robust JSON serialization/deserialization for API communication

**`PythonLlmClient`** (`src/Ollama.Infrastructure/Clients/PythonLlmClient.cs`)
```csharp
public class PythonLlmClient : IPythonLlmClient
{
    // Integrates with Python subsystem for specialized LLM operations
    // Provides bridge between .NET application and Python AI libraries
    // Handles complex AI operations that require Python ecosystem
}
```
- **Purpose**: Bridge to Python-based AI operations and specialized libraries
- **Ecosystem Integration**: Access to Python AI/ML ecosystem from .NET
- **Specialized Operations**: Complex AI operations not available in .NET
- **Process Management**: Handles Python process lifecycle and communication

**`RealLLMCommunicationService`** (`src/Ollama.Infrastructure/Services/RealLLMCommunicationService.cs`)
```csharp
public class RealLLMCommunicationService : ILLMCommunicationService
{
    // Implements structured communication protocols with LLMs
    // Handles schema validation and response parsing
    // Manages contract-based AI-backend communication
}
```
- **Purpose**: Production implementation of structured LLM communication
- **Schema Management**: Request/response schema creation and validation
- **Protocol Implementation**: Contract-based communication protocols
- **Response Parsing**: Robust parsing of structured AI responses
- **Error Recovery**: Handles malformed responses and communication failures

#### Tool Implementations

**`GitHubRepositoryDownloader`** (`src/Ollama.Infrastructure/Tools/GitHubRepositoryDownloader.cs`)
```csharp
public class GitHubRepositoryDownloader : ITool
{
    public string Name => "GitHubDownloader";
    public string Description => "Downloads GitHub repositories as ZIP archives";
    public IEnumerable<string> Capabilities => new[] { "repo:download", "github:clone" };
    public bool RequiresNetwork => true;
    public bool RequiresFileSystem => true;
    
    public async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
}
```
- **Purpose**: Downloads and extracts GitHub repositories for analysis
- **Capabilities**: Repository cloning and archive extraction
- **Error Handling**: Network error recovery and repository validation
- **Session Integration**: Downloads to session-specific directories
- **Format Support**: ZIP archive handling and directory structure preservation

**`FileSystemAnalyzer`** (`src/Ollama.Infrastructure/Tools/FileSystemAnalyzer.cs`)
```csharp
public class FileSystemAnalyzer : ITool
{
    public string Name => "FileSystemAnalyzer";
    public string Description => "Analyzes file system structure, file types, and sizes";
    public IEnumerable<string> Capabilities => new[] { "fs:analyze", "repo:structure" };
    public bool RequiresNetwork => false;
    public bool RequiresFileSystem => true;
    
    public async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
}
```
- **Purpose**: Comprehensive file system analysis and reporting
- **Analysis Types**: File size analysis, directory structure mapping, type classification
- **Performance**: Efficient directory traversal with configurable depth limits
- **Filtering**: Configurable file size thresholds and type filters
- **Reporting**: Structured analysis results with size statistics and recommendations

**`CodeAnalyzer`** (`src/Ollama.Infrastructure/Tools/CodeAnalyzer.cs`)
```csharp
public class CodeAnalyzer : ITool
{
    public string Name => "CodeAnalyzer";
    public string Description => "Analyzes code files for structure, patterns, and potential improvements";
    public IEnumerable<string> Capabilities => new[] { "code:analyze", "code:quality", "code:pattern" };
    public bool RequiresNetwork => false;
    public bool RequiresFileSystem => true;
    
    public async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
}
```
- **Purpose**: Static code analysis and quality assessment
- **Analysis Types**: Syntax analysis, structure analysis, complexity metrics
- **Language Support**: Multi-language code analysis capabilities
- **Quality Metrics**: Code complexity, maintainability, and pattern analysis
- **Recommendations**: Actionable improvement suggestions and best practices

**`ExternalCommandExecutor`** (`src/Ollama.Infrastructure/Tools/ExternalCommandExecutor.cs`)
```csharp
public class ExternalCommandExecutor : ITool
{
    public string Name => "ExternalCommandExecutor";
    public string Description => "Executes external command-line tools and scripts";
    public IEnumerable<string> Capabilities => new[] { "command:execute", "system:external", "fallback:operations" };
    public bool RequiresNetwork => false;
    public bool RequiresFileSystem => true;
    
    public async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
}
```
- **Purpose**: Secure execution of external commands and scripts
- **Security**: Command validation and execution sandboxing
- **Working Directory**: Session-aware working directory management
- **Timeout Management**: Configurable execution timeouts
- **Output Capture**: Complete stdout/stderr capture and processing
- **Error Handling**: Process error detection and reporting

**`MathEvaluator`** (`src/Ollama.Infrastructure/Tools/MathEvaluator.cs`)
```csharp
public class MathEvaluator : ITool
{
    public string Name => "MathEvaluator";
    public string Description => "Evaluates mathematical expressions safely";
    public IEnumerable<string> Capabilities => new[] { "math:evaluate", "arithmetic:calculate" };
    public bool RequiresNetwork => false;
    public bool RequiresFileSystem => false;
    
    public async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
}
```
- **Purpose**: Safe mathematical expression evaluation
- **Security**: Expression validation to prevent code injection
- **Operators**: Support for standard mathematical operators and functions
- **Precision**: High-precision decimal arithmetic
- **Error Handling**: Mathematical error detection and reporting

#### Service Implementations

**`SessionFileSystem`** (`src/Ollama.Infrastructure/Services/SessionFileSystem.cs`)
```csharp
public class SessionFileSystem : ISessionFileSystem
{
    private readonly ILogger<SessionFileSystem> _logger;
    private readonly ConcurrentDictionary<string, string> _sessionCurrentDirectories = new();
    private readonly string _baseCacheDirectory;
    
    // Session management
    public string GetSessionRoot(string sessionId)
    public string GetCurrentDirectory(string sessionId)
    public string ChangeDirectory(string sessionId, string relativePath)
    
    // File operations
    public void WriteFile(string sessionId, string relativePath, string content)
    public string ReadFile(string sessionId, string relativePath)
    public bool FileExists(string sessionId, string relativePath)
    public void DeleteFile(string sessionId, string relativePath)
    public IEnumerable<string> ListFiles(string sessionId, string? directoryPath = null)
    
    // Session lifecycle
    public void ClearSession(string sessionId)
    public void ClearAllSessions()
}
```
- **Purpose**: Session-isolated file system with security boundaries
- **Security Model**: Path validation prevents directory traversal attacks
- **Session Isolation**: Each session gets isolated directory tree
- **Working Directory**: Per-session current directory management
- **File Operations**: Complete file system abstraction with safety checks
- **Cleanup**: Automatic session cleanup and resource management
- **Logging**: Comprehensive operation logging for audit and debugging

**`PlanningService`** (`src/Ollama.Infrastructure/Services/PlanningService.cs`)
```csharp
public class PlanningService : IPlanningService
{
    private readonly IPythonLlmClient _pythonClient;
    private readonly IToolRepository _toolRepository;
    private readonly IModelRegistryService _modelRegistry;
    private readonly ILogger<PlanningService> _logger;
    private const string PLANNING_MODEL = "llama3.3:70b-instruct-q3_K_M";
    
    public async Task<ExecutionPlan> CreatePlanAsync(string query, string strategy)
    public async Task<ExecutionPlan> OptimizePlanAsync(ExecutionPlan plan)
    public async Task<bool> ValidatePlanAsync(ExecutionPlan plan)
}
```
- **Purpose**: Intelligent planning and strategy optimization
- **Model Integration**: Uses specialized planning models for complex reasoning
- **Plan Optimization**: Iterative plan improvement and resource optimization
- **Validation**: Plan feasibility checking and resource validation
- **Strategy Integration**: Different planning approaches based on execution strategy
- **Resource Management**: Tool availability and resource requirement analysis

**`ModelRegistryService`** (`src/Ollama.Infrastructure/Services/ModelRegistryService.cs`)
```csharp
public class ModelRegistryService : IModelRegistryService
{
    // Manages available AI models and their capabilities
    // Handles model discovery, registration, and selection
    // Provides model metadata and capability information
}
```
- **Purpose**: Central registry for AI model management and discovery
- **Model Discovery**: Automatic detection of available models
- **Capability Mapping**: Model capability and specialization tracking
- **Selection Logic**: Intelligent model selection based on task requirements
- **Performance Monitoring**: Model performance and resource usage tracking

#### Strategy Implementations

**`PessimisticAgentStrategy`** (`src/Ollama.Infrastructure/Strategies/PessimisticAgentStrategy.cs`)
```csharp
public class PessimisticAgentStrategy : IAgentStrategy
{
    public string Name => "Pessimistic";
    
    public string GetSystemPrompt()
    {
        // Returns comprehensive system prompt emphasizing:
        // - Thorough validation and risk assessment
        // - Multiple fallback strategies
        // - Detailed error handling
        // - Conservative resource estimation
        // - Backend development guidance requirements
    }
    
    public bool ShouldRetry(int attemptNumber, Exception exception)
    {
        // Conservative retry logic with extensive validation
        // Multiple retry attempts with escalating strategies
        // Detailed error analysis and alternative approaches
    }
}
```
- **Purpose**: Conservative execution strategy with extensive validation
- **Risk Management**: Assumes worst-case scenarios and plans accordingly
- **Retry Logic**: Multiple retry attempts with escalating fallback strategies
- **Validation**: Extensive input validation and precondition checking
- **Backend Focus**: Emphasizes specific backend development guidance
- **Error Handling**: Comprehensive error handling and recovery mechanisms

### Interface Layer (Entry Points)

**`Program`** (`src/Ollama.Interface.Cli/Program.cs`)
```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        // Application startup and configuration
        // Command line argument parsing
        // Dependency injection container setup
        // Service registration and configuration
        // Main execution loop
    }
}
```
- **Purpose**: Main entry point for CLI application
- **Argument Parsing**: Comprehensive command-line argument processing
- **Configuration**: Application configuration and environment setup
- **Service Bootstrap**: Dependency injection and service registration
- **Error Handling**: Top-level error handling and user feedback

### Bootstrap Layer (Dependency Injection)

**`ServiceRegistration`** (`src/Ollama.Bootstrap/Composition/ServiceRegistration.cs`)
```csharp
public static class ServiceRegistration
{
    public static IServiceCollection AddOllamaServices(this IServiceCollection services)
    {
        // Core services registration
        services.AddSingleton<ExecutionTreeBuilder>();
        services.AddSingleton<AgentSwitchService>();
        services.AddSingleton<CollaborationContextService>();
        
        // Agent implementations
        services.AddSingleton<IAgent, StrategicAgent>();
        services.AddSingleton<IAgent, IntelligentAgent>();
        
        // Tool registrations
        services.AddSingleton<ITool, GitHubRepositoryDownloader>();
        services.AddSingleton<ITool, FileSystemAnalyzer>();
        services.AddSingleton<ITool, CodeAnalyzer>();
        services.AddSingleton<ITool, ExternalCommandExecutor>();
        services.AddSingleton<ITool, MathEvaluator>();
        
        // Strategy implementations
        services.AddSingleton<IModeStrategy, SingleQueryMode>();
        services.AddSingleton<IModeStrategy, CollaborativeMode>();
        services.AddSingleton<IModeStrategy, IntelligentMode>();
        
        // Infrastructure services
        services.AddSingleton<ISessionFileSystem, SessionFileSystem>();
        services.AddSingleton<IPlanningService, PlanningService>();
        services.AddSingleton<IToolRepository, ToolRepository>();
        
        return services;
    }
}
```
- **Purpose**: Centralized dependency injection configuration
- **Service Lifetime Management**: Appropriate singleton/transient/scoped registrations
- **Interface Binding**: Clean separation between contracts and implementations
- **Tool Auto-Discovery**: Automatic tool registration and discovery
- **Strategy Registration**: All execution strategies registered for dynamic selection
- **Configuration**: Binds configuration objects to dependency injection container

---

## System Behavior Analysis

### Iterative LLM Interaction Model

The Ollama Agent Suite implements a sophisticated iterative interaction model where the system continuously engages with the LLM until the user's initial prompt is completely satisfied.

#### The Core Iteration Loop

**Fundamental Principle**: The system never stops until the LLM confirms the original user prompt has been fully addressed.

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   User Query    │───▶│  LLM Analysis   │───▶│ Task Complete?  │
└─────────────────┘    └─────────────────┘    └─────────┬───────┘
                                                        │
                                               ┌────────▼────────┐
                                               │      YES        │
                                               │  Final Response │
                                               └─────────────────┘
                                                        │
                                               ┌────────▼────────┐
                                               │       NO        │
                                               │ Tool Required?  │
                                               └─────────┬───────┘
                                                        │
                                               ┌────────▼────────┐
                                               │   System Tool   │
                                               │   Execution     │
                                               └─────────┬───────┘
                                                        │
                                               ┌────────▼────────┐
                                               │ Feed Results    │
                                               │ Back to LLM     │
                                               └─────────┬───────┘
                                                        │
                                                        ▼
                                                ┌─────────────────┐
                                                │ Next Iteration  │◀─┐
                                                │ (with context)  │  │
                                                └─────────────────┘  │
                                                        │            │
                                                        └────────────┘
```

**Key Iteration Characteristics:**
- 🔄 **Continuous Loop**: Never stops until LLM confirms completion
- 📊 **Context Accumulation**: Each iteration builds on previous results  
- 🎯 **Goal Oriented**: Always focused on original user prompt satisfaction
- 🔧 **Tool Augmented**: System provides capabilities LLM lacks
- ✅ **Quality Assured**: LLM validates completeness before finishing

#### Detailed Iteration Workflow

1. **Initial Query Processing**
   - User submits natural language query
   - **System Prompt Decoration**: LLM receives comprehensive tool inventory via reflection-based discovery
   - **Tool Information Provided**: Detailed parameters, capabilities, and usage examples for all internal tools
   - **External Tool Notification**: Awareness of available system commands (git, python, powershell, etc.)
   - LLM analyzes requirements and creates initial execution plan with full knowledge of capabilities

2. **Completion Assessment**
   - LLM evaluates: "Is the original user prompt fully satisfied?"
   - If YES: Task marked complete, final response provided
   - If NO: Identifies next required action/tool

3. **Tool Requirement Analysis**
   - LLM recognizes its limitations (cannot access files, network, system commands)
   - Identifies specific tools needed to progress toward goal
   - Provides structured JSON response with tool selection and parameters

4. **System Tool Execution**
   - Our system receives the JSON response
   - Extracts tool name and parameters
   - Executes the requested tool in isolated session environment
   - Captures complete tool output and any errors

5. **Result Integration and Feedback**
   - Tool execution results are immediately fed back to the LLM
   - LLM receives both the original context AND the new tool results
   - LLM analyzes if this new information helps satisfy the original prompt

6. **Iterative Continuation**
   - Process repeats from step 2 (Completion Assessment)
   - Each iteration builds upon previous results
   - LLM maintains context of all previous actions and results
   - Continues until original prompt is fully addressed

#### Why This Iterative Approach Is Essential

**LLM Limitations Require System Augmentation:**

```
❌ LLM CANNOT:                    ✅ OUR SYSTEM CAN:
- Download GitHub repositories   - GitHubDownloader tool
- Analyze file structures        - FileSystemAnalyzer tool  
- Execute system commands        - ExternalCommandExecutor tool
- Read local files              - CodeAnalyzer tool
- Perform network operations    - HTTP client integration
- Access external APIs          - Tool framework
```

**Example Iteration Sequence:**

```
User: "Download and analyze the architecture of https://github.com/user/project"

Iteration 1:
LLM: "I need to download the repository first"
→ System executes GitHubDownloader
→ Repository downloaded to session directory

Iteration 2:  
LLM: "Now I need to analyze the file structure"
→ System executes FileSystemAnalyzer  
→ Directory structure mapped

Iteration 3:
LLM: "I should examine key configuration files"
→ System executes CodeAnalyzer on specific files
→ Code structure analyzed

Iteration 4:
LLM: "Based on all analysis, here's the architecture assessment..."
→ Task marked complete with comprehensive analysis
```

#### JSON-Driven Tool Orchestration Details

**Every tool execution follows this pattern:**

1. **LLM provides structured JSON response:**
```json
{
  "reasoning": "I need to download the repository to analyze its architecture",
  "taskComplete": false,
  "nextStep": "Download the GitHub repository using GitHubDownloader",
  "requiresTool": true,
  "tool": "GitHubDownloader",
  "parameters": {
    "repoUrl": "https://github.com/user/project",
    "sessionId": "current-session-id"
  }
}
```

2. **System parses JSON and executes tool:**
   - Validates tool name and parameters
   - Executes tool in session-isolated environment
   - Captures complete output and any errors
   - Logs all operations for audit trail

3. **Results fed back to LLM:**
   - Tool output appended to conversation context
   - LLM receives both original query context AND new results
   - LLM can make informed decisions about next steps

4. **Completion evaluation:**
   - LLM assesses if original prompt is now satisfied
   - If complete: provides final comprehensive response
   - If incomplete: identifies next required action

#### Session Continuity and Context Preservation

**Critical Features:**
- **Complete Context Retention**: Every iteration maintains full conversation history
- **Tool Result Accumulation**: All tool outputs are preserved and available to LLM
- **Progress Tracking**: System tracks how each iteration contributes to the goal
- **Error Recovery**: Failed tools don't break the iteration; LLM can choose alternatives
- **Comprehensive Logging**: Complete audit trail of all LLM decisions and tool executions

### Request Processing Flow

The system follows a sophisticated multi-phase execution model:

#### Phase 1: Request Ingestion and Context Building
1. **CLI Entry Point** (`Program.cs`) receives user query with optional parameters
2. **Argument Parser** extracts query, strategy mode, verbose flags, cache controls
3. **Configuration Loader** builds application context from `appsettings.json`
4. **Service Container** initializes all registered services and dependencies
5. **Session Generator** creates unique session ID for request isolation

#### Phase 2: Strategy Selection and Mode Determination
1. **StrategyOrchestrator** receives query and optional mode preference
2. **ModeRegistry** evaluates available strategies against query characteristics
3. **Context Analysis** examines query complexity, length, keywords, and requirements
4. **Strategy Selection Logic**:
   - **Explicit Mode**: If user specifies mode (`--agent pessimistic`), use directly
   - **Intelligent Selection**: Evaluate query against strategy capabilities
   - **Fallback Chain**: Intelligent → Collaborative → SingleQuery
5. **ExecutionContext** created with selected strategy and enriched metadata

#### Phase 3: Agent Initialization and Strategy Application
1. **Agent Factory** creates appropriate agent instance based on selected strategy
2. **Strategy Configuration**: 
   - **PessimisticAgentStrategy**: Conservative, extensive validation, backend-focused
   - **OptimisticAgentStrategy**: Fast execution, minimal validation
   - **BalancedAgentStrategy**: Moderate approach balancing speed and safety
3. **Session Initialization**:
   - Create isolated session directory under `cache/[sessionId]/`
   - Initialize conversation history tracking
   - Set up session-specific working directory
   - Load strategy-specific system prompts

#### Phase 4: Query Analysis and Planning
1. **Strategic Agent** receives query and begins analysis
2. **LLM Communication Service** formats request according to communication schema
3. **System Prompt Generation**: Strategy-specific prompts emphasizing:
   - JSON-only responses with strict schema compliance
   - Backend development guidance requirements
   - Tool usage guidelines and capabilities
   - Risk assessment and validation protocols
4. **Tool Discovery**: Agent enumerates available tools and their capabilities
5. **Initial Assessment**: LLM analyzes query and determines execution approach

#### Phase 5: Execution Planning and Tool Selection
1. **Planning Service** creates detailed execution plan based on query complexity
2. **Tool Repository** queried for capabilities matching identified requirements
3. **Resource Validation**: Check tool requirements (network, filesystem, etc.)
4. **Execution Strategy**: Determine single-step vs. multi-step execution approach
5. **Fallback Planning**: Identify alternative approaches for potential failures

#### Phase 6: Tool Execution and Result Processing
1. **Tool Orchestration**: Execute selected tools in planned sequence
2. **Session File System**: Provide isolated execution environment for tools
3. **Progress Tracking**: Build execution tree documenting each decision and action
4. **Error Handling**: Implement retry logic and alternative approaches
5. **Result Aggregation**: Combine tool outputs into coherent analysis

#### Phase 7: Response Synthesis and Backend Guidance
1. **Result Analysis**: LLM processes tool outputs and execution results
2. **Backend Guidance Generation**: Create specific, actionable development steps
3. **Next Step Formulation**: Define concrete backend actions (file creation, API endpoints, etc.)
4. **Response Formatting**: Structure response according to JSON schema requirements
5. **Conversation State**: Update session history with complete interaction record

#### Phase 8: Response Delivery and Session Management
1. **Response Validation**: Ensure JSON schema compliance and completeness
2. **Session Persistence**: Save conversation state and artifacts
3. **CLI Output**: Format response for user consumption
4. **Session Cleanup**: Apply retention policies and cleanup temporary resources
5. **Audit Logging**: Record complete interaction for debugging and analysis

### Decision Making Algorithms

#### Strategy Selection Algorithm
```
function SelectStrategy(query, userPreference):
    if userPreference is specified:
        return GetStrategy(userPreference)
    
    complexity = AnalyzeQueryComplexity(query)
    keywords = ExtractKeywords(query)
    
    if complexity > HIGH_THRESHOLD or 
       keywords.contains(["analyze", "repository", "github.com", "complex"]):
        return IntelligentMode
    
    if complexity > MEDIUM_THRESHOLD or
       keywords.contains(["code", "implement", "create", "build"]):
        return CollaborativeMode
    
    return SingleQueryMode
```

#### Tool Selection Algorithm
```
function SelectTools(query, availableTools):
    requiredCapabilities = AnalyzeRequiredCapabilities(query)
    selectedTools = []
    
    for capability in requiredCapabilities:
        candidateTools = toolRepository.FindToolsByCapability(capability)
        bestTool = SelectBestTool(candidateTools, query)
        selectedTools.add(bestTool)
    
    return OptimizeToolSequence(selectedTools)
```

#### Retry and Fallback Logic
```
function ExecuteWithRetry(tool, parameters, maxRetries):
    for attempt in 1 to maxRetries:
        try:
            result = tool.RunAsync(parameters)
            if result.Success:
                return result
        catch exception:
            if attempt < maxRetries:
                alternativeApproach = GetAlternativeApproach(tool, attempt)
                if alternativeApproach exists:
                    return ExecuteWithRetry(alternativeApproach, parameters, maxRetries - attempt)
        
        delay = CalculateExponentialBackoff(attempt)
        wait(delay)
    
    throw ExecutionFailedException("All retry attempts exhausted")
```

### Communication Protocol Implementation

#### JSON Schema Validation
The system enforces strict JSON communication through multi-layer validation:

1. **Schema Definition**: TypeScript-style interfaces define expected structure
2. **Runtime Validation**: JSON.NET validates structure and types
3. **Business Logic Validation**: Custom validators check field relationships
4. **Error Recovery**: Malformed responses trigger retry with enhanced prompts

#### LLM Communication Flow
```
User Query → Schema Generation → System Prompt Creation → LLM Request → 
Response Parsing → Schema Validation → Business Logic Execution → 
Tool Coordination → Result Synthesis → Response Formatting → User Output
```

#### Conversation State Management
- **Session Isolation**: Each conversation maintains separate state
- **History Tracking**: Complete audit trail of all interactions
- **Context Preservation**: Rich context passed between interaction rounds
- **State Persistence**: Conversation state survives application restarts

### Error Handling and Recovery Patterns

#### Hierarchical Error Handling
1. **Tool Level**: Individual tool error handling and reporting
2. **Agent Level**: Agent-wide error recovery and alternative strategies
3. **Strategy Level**: Strategy-specific error handling patterns
4. **Application Level**: Top-level error handling and user feedback

#### Failure Recovery Strategies
- **Tool Failures**: Alternative tool selection and retry logic
- **Communication Failures**: Enhanced prompts and schema guidance
- **Resource Failures**: Graceful degradation and resource optimization
- **Network Failures**: Offline mode and cached responses

#### Progressive Fallback Chain
```
Primary Tool → Alternative Tool → External Command → Manual Guidance → User Intervention
```

---

## Expected System Behavior

### Ideal Operating Conditions

#### Performance Characteristics
- **Response Time**: 5-30 seconds for simple queries, 1-5 minutes for complex operations
- **Accuracy**: 90%+ correct tool selection and execution for well-defined queries
- **Reliability**: 99%+ success rate for basic operations, graceful degradation for edge cases
- **Scalability**: Support for 10+ concurrent sessions without performance degradation

#### Quality Metrics
- **Backend Guidance Quality**: Specific, actionable development steps with file/method details
- **Tool Selection Accuracy**: Optimal tool selection for 95%+ of appropriate queries
- **Error Recovery Rate**: Successful recovery from 80%+ of recoverable failures
- **Session Isolation**: 100% isolation between concurrent sessions

### Response Quality Standards

#### Backend Development Guidance
Every response should include:
- **Specific File Paths**: Exact locations for code changes
- **Method/Class Names**: Precise implementation targets
- **Implementation Steps**: Step-by-step development instructions
- **Configuration Changes**: Detailed configuration modifications
- **Testing Guidance**: Unit test and validation recommendations

#### Tool Execution Results
- **Complete Analysis**: Comprehensive results from tool execution
- **Contextual Interpretation**: AI-generated insights from raw tool outputs
- **Actionable Recommendations**: Specific next steps based on findings
- **Risk Assessment**: Identified potential issues and mitigation strategies

### User Experience Expectations

#### Command Line Interface
```bash
# Simple query - should complete in seconds
dotnet run -- query "What is dependency injection?" --agent pessimistic

# Complex query - should provide progress updates
dotnet run -- query "Download and analyze https://github.com/user/repo" --verbose

# Development guidance - should provide specific backend steps
dotnet run -- query "Create user authentication system" --agent pessimistic
```

#### Response Format Consistency
- **JSON Schema Compliance**: All agent responses follow strict JSON contracts
- **Progressive Disclosure**: Complex operations show progress and intermediate results
- **Error Transparency**: Clear error messages with actionable recovery suggestions
- **Audit Trail**: Complete session history available for review

#### Session Management
- **Automatic Session Creation**: Unique session per query execution
- **State Persistence**: Session state preserved across application restarts
- **Session Cleanup**: Automatic cleanup based on retention policies
- **Session Recovery**: Ability to resume interrupted sessions

### Integration Behavior

#### Tool Ecosystem Integration
- **Dynamic Discovery**: Tools automatically discovered and registered at startup
- **Capability Matching**: Intelligent tool selection based on query requirements
- **Resource Management**: Efficient resource utilization across tool executions
- **Failure Isolation**: Tool failures don't affect other tools or overall system

#### External System Integration
- **Ollama Service**: Seamless integration with local Ollama LLM service
- **File System**: Secure, session-isolated file system operations
- **Network Resources**: Efficient network resource utilization for downloads
- **Command Execution**: Safe execution of external commands with proper sandboxing

#### Configuration Management
- **Environment-Specific**: Different configurations for development/production
- **Dynamic Reconfiguration**: Configuration changes without application restart
- **Validation**: Configuration validation at startup with clear error messages
- **Defaults**: Sensible defaults for all configuration parameters

---

## System Architecture Philosophy

### Design Principles

#### Clean Architecture Implementation
The system strictly follows clean architecture principles:

1. **Dependency Inversion**: High-level modules don't depend on low-level modules
2. **Interface Segregation**: Clients depend only on interfaces they use
3. **Single Responsibility**: Each class has one reason to change
4. **Open/Closed**: Open for extension, closed for modification
5. **Liskov Substitution**: Derived classes must be substitutable for base classes

#### Hexagonal Architecture Patterns
- **Ports and Adapters**: Clear separation between core logic and external concerns
- **Domain Isolation**: Pure domain logic with no external dependencies
- **Adapter Pattern**: External systems accessed through adapters implementing domain ports
- **Testability**: Core logic testable without external dependencies

#### Strategic Design Patterns

**Strategy Pattern**: Different execution modes (Single, Collaborative, Intelligent)
```csharp
public interface IModeStrategy
{
    StrategyType Type { get; }
    bool CanHandle(ExecutionContext context);
    Dictionary<string, object> Execute(ExecutionContext context);
}
```

**Command Pattern**: Tool execution with undo/redo capabilities
```csharp
public interface ITool
{
    Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken);
    Task<bool> DryRunAsync(ToolContext context);
    Task<decimal> EstimateCostAsync(ToolContext context);
}
```

**Observer Pattern**: Execution tree building and progress tracking
```csharp
public class ExecutionTreeBuilder
{
    public ExecutionTreeBuilder AddAnalysis(string content);
    public ExecutionTreeBuilder AddCommand(string content);
    public ExecutionTreeBuilder AddResponse(string content);
}
```

**Factory Pattern**: Agent and tool creation based on requirements
```csharp
public class AgentSwitchService
{
    public void Register(string role, object agent);
    public T Resolve<T>(string role) where T : class;
}
```

### Scalability Architecture

#### Horizontal Scaling Considerations
- **Stateless Design**: Core agents are stateless for easy scaling
- **Session Isolation**: Sessions can be distributed across multiple instances
- **Tool Parallelization**: Independent tools can execute in parallel
- **Resource Pooling**: Shared resources (HTTP clients, etc.) for efficiency

#### Vertical Scaling Optimizations
- **Memory Management**: Efficient memory usage with proper disposal patterns
- **CPU Optimization**: Asynchronous processing and parallel execution
- **I/O Optimization**: Batched file operations and connection pooling
- **Cache Strategy**: Intelligent caching of tool results and LLM responses

#### Performance Monitoring Points
- **Request Processing Time**: End-to-end query processing duration
- **Tool Execution Time**: Individual tool performance metrics
- **LLM Response Time**: AI model response latency tracking
- **Memory Usage**: Session memory consumption and cleanup effectiveness
- **Resource Utilization**: CPU, memory, disk, and network usage patterns

### Security Architecture

#### Session Isolation Security
- **Directory Traversal Prevention**: Path validation prevents escaping session boundaries
- **Resource Quotas**: Limits on disk usage, execution time, and network usage
- **Process Isolation**: External commands execute in controlled environment
- **Input Validation**: All user inputs validated and sanitized

#### AI Safety Measures
- **Prompt Injection Prevention**: System prompts protected from user manipulation
- **Output Validation**: AI outputs validated against expected schemas
- **Command Filtering**: External commands filtered through allowlist
- **Resource Limits**: Execution time and resource usage limits

#### Data Protection
- **Session Encryption**: Sensitive session data encrypted at rest
- **Audit Logging**: Complete audit trail of all operations
- **Data Retention**: Configurable data retention and cleanup policies
- **Access Control**: Role-based access to system functionality

### Extensibility Framework

#### Plugin Architecture
- **Tool Plugin System**: Dynamic tool loading and registration
- **Strategy Plugin System**: Custom execution strategies
- **Agent Plugin System**: Specialized agent implementations
- **Communication Plugin System**: Alternative LLM communication protocols

#### Configuration Extensibility
- **Environment-Specific**: Multiple configuration environments
- **Feature Flags**: Runtime feature enabling/disabling
- **Resource Configuration**: Configurable resource limits and timeouts
- **Behavior Configuration**: Configurable retry logic and fallback strategies

#### Integration Points
- **LLM Provider Abstraction**: Support for multiple LLM providers
- **Tool Framework**: Standardized tool development framework
- **Strategy Framework**: Framework for custom execution strategies
- **Communication Framework**: Pluggable communication protocols

### 1. JSON Contract Schema

All AI-backend communication follows a strict JSON schema:

#### Request Schema
```json
{
  "sessionId": "unique-session-identifier",
  "userQuery": "Natural language query",
  "context": {
    "currentStep": 1,
    "workingDirectory": "/path/to/session",
    "previousResults": {}
  },
  "availableTools": [
    {
      "name": "GitHubDownloader",
      "description": "Downloads GitHub repositories",
      "parameters": ["repoUrl", "sessionId"],
      "capabilities": ["repo:download", "github:clone"]
    }
  ],
  "strategy": "Pessimistic",
  "previousInteractions": []
}
```

#### Response Schema
```json
{
  "analysis": {
    "summary": "Comprehensive analysis of the request",
    "complexity": "medium",
    "estimatedSteps": 3,
    "requiredCapabilities": ["repo:download", "fs:analyze"]
  },
  "nextStep": {
    "stepNumber": 1,
    "action": "Download GitHub repository for analysis",
    "toolName": "GitHubDownloader",
    "parameters": {
      "repoUrl": "https://github.com/user/repo",
      "sessionId": "session-123"
    },
    "expectedOutcome": "Repository downloaded and ready for analysis"
  },
  "confidence": {
    "overallConfidence": 0.85,
    "uncertaintyFactors": ["Repository may be private", "Network connectivity required"]
  },
  "continuation": {
    "requiresUserConfirmation": false,
    "isComplete": false,
    "progressPercentage": 10,
    "nextExpectedInput": "Analysis of downloaded repository"
  }
}
```

### 2. Tool Execution Protocol

Tools follow a standardized execution pattern:

1. **Parameter Validation**: Ensure all required parameters are provided
2. **Capability Check**: Verify the tool can handle the requested operation
3. **Execution**: Perform the operation with proper error handling
4. **Result Packaging**: Return structured results with success/failure status

```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    IEnumerable<string> Capabilities { get; }
    bool RequiresNetwork { get; }
    bool RequiresFileSystem { get; }
    
    Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default);
}
```

### Tool Execution Workflow

#### The Tool Augmentation Strategy

**Core Principle**: LLMs excel at reasoning and programming tasks but cannot perform real-world operations. Our system bridges this gap through intelligent tool integration.

#### What LLMs Can Handle Directly:
- ✅ **Code Analysis**: Understanding and explaining code structures
- ✅ **Algorithm Design**: Creating logical solution approaches  
- ✅ **Text Processing**: Analyzing and generating textual content
- ✅ **Pattern Recognition**: Identifying patterns in data/code
- ✅ **Problem Decomposition**: Breaking complex tasks into steps
- ✅ **Documentation Generation**: Creating comprehensive explanations

#### What Requires Tool Augmentation:
- 🔧 **File System Access**: Reading, writing, analyzing local files
- 🔧 **Network Operations**: Downloading repositories, API calls
- 🔧 **Command Execution**: Running system commands, git operations
- 🔧 **External Integrations**: GitHub API, package managers, build systems
- 🔧 **Resource Management**: Session isolation, cleanup, security boundaries

#### Tool Selection and Execution Process

**1. LLM Tool Request Generation**
```json
{
  "reasoning": "To analyze the repository structure, I need to first download it",
  "taskComplete": false,
  "nextStep": "Download repository and then analyze its structure", 
  "requiresTool": true,
  "tool": "GitHubDownloader",
  "parameters": {
    "repoUrl": "https://github.com/example/repo",
    "sessionId": "session-abc123"
  }
}
```

**2. System Tool Execution**
- Parse JSON response and validate tool request
- Execute tool with provided parameters in isolated session
- Capture complete output, errors, and execution metadata
- Log operation for audit and debugging

**3. Result Integration**
- Tool output immediately appended to conversation context
- LLM receives enriched context for next iteration
- Maintains complete history of all tool executions
- Enables informed decision-making for subsequent steps

**4. Continuation Decision**
- LLM evaluates if tool results help satisfy original prompt
- Determines if additional tools/iterations are needed
- Either continues iteration or marks task complete

#### Example Tool Execution Sequence

**User Query**: "Analyze the testing strategy in https://github.com/user/project"

**Iteration 1 - Repository Download:**
```
LLM Decision: Need to download repository first
Tool Executed: GitHubDownloader
Tool Output: Repository downloaded to /session/downloads/project/
LLM Assessment: Repository available, now need structure analysis
```

**Iteration 2 - Structure Analysis:**
```
LLM Decision: Analyze project structure to find test files
Tool Executed: FileSystemAnalyzer  
Tool Output: Found /tests/, /spec/, package.json with test scripts
LLM Assessment: Test directories identified, need to examine test files
```

**Iteration 3 - Test File Analysis:**
```
LLM Decision: Examine specific test files to understand strategy
Tool Executed: CodeAnalyzer on test files
Tool Output: Jest framework, unit tests, integration tests, coverage setup
LLM Assessment: Sufficient information gathered, can provide analysis
```

**Final Response:**
```
LLM Conclusion: Task complete - comprehensive testing strategy analysis provided
Includes: Test framework details, coverage approach, test organization
```

#### Tool Integration Benefits

**For Users:**
- **Seamless Experience**: No need to manually execute tools or commands
- **Comprehensive Analysis**: LLM can access and analyze real data
- **Iterative Refinement**: System continues until full satisfaction
- **Error Recovery**: Failed operations don't stop progress

**For System:**
- **Security Isolation**: All tool execution in session boundaries
- **Audit Trail**: Complete logging of all operations
- **Resource Management**: Proper cleanup and resource limits
- **Extensibility**: Easy addition of new tools and capabilities

**For Development:**
- **Backend Focus**: Tools specifically designed for development tasks
- **Real-World Integration**: Actual file access, repository analysis, command execution
- **Contextual Intelligence**: LLM makes informed decisions based on real data
- **Progressive Discovery**: Each tool execution reveals new information for analysis

#### Completion Criteria and Iteration Termination

**How the System Determines Task Completion:**

The iterative process continues until the LLM explicitly confirms that the original user prompt has been fully satisfied. This is determined through:

**1. LLM Self-Assessment**
```json
{
  "reasoning": "I have successfully downloaded, analyzed, and provided comprehensive insights about the repository architecture",
  "taskComplete": true,
  "taskCompleted": true,
  "nextStep": "Task is complete - provided full architecture analysis",
  "requiresTool": false,
  "response": "Complete analysis of the repository architecture..."
}
```

**2. Completion Validation Criteria**
- ✅ **Original Intent Satisfied**: LLM confirms the user's original question is answered
- ✅ **Sufficient Data Gathered**: All necessary information has been collected via tools
- ✅ **Analysis Complete**: LLM has processed all available data
- ✅ **Actionable Guidance Provided**: Backend development steps or insights delivered
- ✅ **No Further Tools Needed**: LLM sees no additional operations required

**3. Quality Assurance Checks**
- **Completeness**: Does the response fully address the user's query?
- **Specificity**: Are the recommendations concrete and actionable?
- **Evidence-Based**: Is the analysis supported by actual tool execution results?
- **Backend Focus**: Does the response provide specific development guidance?

**4. Iteration Continuation Triggers**
The system continues iterating when:
- ❌ `taskComplete: false` in LLM response
- ❌ `requiresTool: true` indicating more work needed
- ❌ LLM identifies additional information needed
- ❌ Error recovery requires alternative approaches
- ❌ User prompt not fully addressed

**5. Session Completion Process**
When iteration ends:
1. **Final Response Generation**: LLM provides comprehensive final answer
2. **Session Summary**: Complete overview of all actions taken
3. **Artifact Preservation**: All tool outputs and analysis saved
4. **Next Steps Documentation**: Future development recommendations
5. **Audit Trail Completion**: Full log of iteration process

#### Example Completion Assessment

**Successful Completion:**
```
User: "Analyze the security measures in this repository"

Final LLM Assessment:
{
  "taskComplete": true,
  "reasoning": "I have downloaded the repository, analyzed all security-related files, examined authentication mechanisms, reviewed dependency security, and identified specific areas for improvement. The original prompt is fully satisfied.",
  "response": "Complete security analysis: [detailed findings]",
  "nextStep": "Implement recommended security improvements in authentication module"
}

✅ ITERATION ENDS - Task fully completed
```

**Incomplete Task (Continues Iteration):**
```
User: "Analyze the security measures in this repository"

Current LLM Assessment:
{
  "taskComplete": false,
  "reasoning": "I have downloaded the repository but still need to examine the authentication files and dependency configurations",
  "requiresTool": true,
  "tool": "CodeAnalyzer",
  "nextStep": "Analyze authentication modules for security patterns"
}

🔄 ITERATION CONTINUES - More analysis needed
```

### Tool Architecture: Internal Tools Only

**⚠️ IMPORTANT CHANGE: All tools are now internal implementations only. External tool dependencies have been eliminated.**

The system provides the LLM with comprehensive tool information through **internal tool reflection and session-aware operation**. All tools are:

- **Self-Contained**: No external dependencies or system tool requirements
- **Session-Aware**: All tools operate within session boundaries for security
- **Reflection-Discovered**: Tools are automatically discovered and documented via reflection
- **Boundary-Validated**: All operations are validated against session directory limits

#### Internal Tools (Reflection-Based Discovery + Session Isolation)

**⚠️ IMPORTANT: This is NOT a complete list of internal tools.** The system uses **reflection-based tool discovery** to dynamically enumerate all available tools at startup and provides this information to the LLM in the initial system prompt.

**Dynamic Tool Information Generation Process**:
```csharp
// System automatically discovers tools via reflection
public Task<string> GetInitialPromptWithDynamicToolsAsync(IToolRepository toolRepository)
{
    // 1. Enumerate all registered ITool implementations
    var tools = toolRepository.GetAllTools();
    
    // 2. Extract capabilities via reflection  
    foreach (var tool in tools)
    {
        var capabilities = tool.Capabilities;
        var parameters = ExtractParametersViaReflection(tool);
        var requirements = GetToolRequirements(tool);
        var sessionSupport = ValidateSessionSupport(tool);
        
        // 3. Generate detailed usage documentation
        var toolDocumentation = GenerateToolDocumentation(tool, capabilities, parameters, sessionSupport);
        
        // 4. Add to system prompt
        promptBuilder.AppendToolInformation(toolDocumentation);
    }
    
    // 5. Update system prompt with current tool inventory
    return UpdatedSystemPromptWithToolDetails();
}
```

**Session-Aware Tool Implementation**:
All tools now receive session context and validate operations:
```csharp
public async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
{
    // 1. Validate session context
    if (string.IsNullOrEmpty(context.SessionId))
    {
        return new ToolResult { Success = false, ErrorMessage = "Session ID required" };
    }

    // 2. Validate all paths are within session boundaries
    if (!_sessionFileSystem.IsWithinSessionBoundary(context.SessionId, requestedPath))
    {
        return new ToolResult { Success = false, ErrorMessage = "Path outside session boundaries" };
    }

    // 3. Use session-safe working directory
    var safeWorkingDir = _sessionFileSystem.GetSafeWorkingDirectory(context.SessionId);
    
    // 4. Perform operation within session constraints
    return await PerformSessionSafeOperation(context);
}
```

**Tool Insufficiency Detection**:
The LLM now has enhanced capability to identify missing or insufficient tools:

```json
{
  "reasoning": "Analysis requires specialized database migration tool which is not available",
  "taskComplete": false,
  "nextStep": "Request implementation of DatabaseMigrationTool to handle Entity Framework migrations",
  "requiresTool": true,
  "tool": "MISSING_TOOL",
  "parameters": {
    "requiredToolName": "DatabaseMigrationTool",
    "requiredCapabilities": ["ef:migrate", "db:schema", "migration:generate"],
    "reason": "Need to analyze and generate database migrations for user authentication system"
  },
  "confidence": 0.2,
  "assumptions": ["Database migration tool would be required for this task"],
  "risks": ["Cannot complete database-related tasks without proper migration tools"],
  "response": "I need a DatabaseMigrationTool to handle Entity Framework migrations. Current tools are insufficient for database schema operations."
}
```

**Generated Tool Documentation Example**:
```
• GitHubDownloader: Downloads GitHub repositories for analysis
  - Purpose: Repository downloading and local caching within session boundaries
  - Capabilities: repo:download, github:clone
  - Parameters: 
    * repoUrl (string, required): GitHub repository URL
    * sessionId (string, auto-provided): Session identifier for isolation
  - Requirements: Network access, session isolation enforced
  - Session Safety: All downloads confined to /cache/[sessionId]/downloads/
  - Usage Example: "Use GitHubDownloader tool with repoUrl=https://github.com/user/repo"
  - Success Criteria: Repository successfully downloaded to session directory
  - Error Handling: Network failures trigger retry, path validation prevents escape
```

**System Prompt Integration**:
All tool information is automatically integrated into the system prompt that the LLM receives, ensuring it has complete and current knowledge of available capabilities without manual maintenance. The prompt is regenerated before each session to include current tool inventory.

**Current Internal Tools** (discovered dynamically):

#### 1. GitHubDownloader
- **Purpose**: Downloads GitHub repositories for analysis within session boundaries
- **Capabilities**: `repo:download`, `github:clone`
- **Parameters**: `repoUrl` (auto-receives `sessionId`)
- **Requirements**: Network access
- **Session Safety**: Downloads to `/cache/[sessionId]/downloads/`
- **Boundary Validation**: All extracted files validated against session limits

#### 2. FileSystemAnalyzer
- **Purpose**: Analyzes directory structures and file sizes within session
- **Capabilities**: `fs:analyze`, `repo:structure`
- **Parameters**: `directoryPath` (must be within session), `includeSubdirectories`, `minimumFileSize`
- **Requirements**: File system access (session-limited)
- **Session Safety**: Can only analyze paths within `/cache/[sessionId]/`
- **Boundary Validation**: Rejects paths outside session boundaries

#### 3. CodeAnalyzer
- **Purpose**: Analyzes source code for patterns and structure (session-contained files)
- **Capabilities**: `code:analyze`, `code:quality`, `code:pattern`
- **Parameters**: Works on data from session context, no direct file access
- **Requirements**: File system access (session-limited)
- **Session Safety**: Operates on session-contained file analysis data
- **Boundary Validation**: No direct file system access, uses session context

#### 4. ExternalCommandExecutor
- **Purpose**: Executes system commands and scripts within session working directory
- **Capabilities**: `command:execute`, `system:external`
- **Parameters**: `command`, `workingDirectory` (session-validated), `timeoutSeconds`
- **Requirements**: System access (session-limited)
- **Session Safety**: Working directory always set to session-safe paths
- **Boundary Validation**: All working directories validated against session boundaries

#### 5. MathEvaluator
- **Purpose**: Evaluates mathematical expressions safely (no session requirements)
- **Capabilities**: `math:evaluate`, `arithmetic:calculate`
- **Parameters**: `expression`
- **Requirements**: None
- **Session Safety**: No file system interaction required
- **Boundary Validation**: Not applicable (stateless operation)

**Tool Registration Process**:
1. **Automatic Discovery**: System scans for `ITool` implementations
2. **Session Validation**: Tools verified for session isolation support
3. **Capability Extraction**: Reflection extracts tool capabilities and parameters
4. **Prompt Generation**: Creates detailed tool descriptions for LLM with session information
5. **Runtime Registration**: Tools registered in dependency injection container with session dependencies

#### Missing Tool Detection and Feedback

The LLM is equipped to identify when current tools are insufficient for a task and can request new tool implementations:

**Example: Missing Database Tool**:
```json
{
  "reasoning": "User requires database schema analysis but no database tools are available",
  "taskComplete": false,
  "nextStep": "Implement DatabaseAnalyzer tool with capabilities: schema:analyze, table:inspect, relation:map",
  "requiresTool": true,
  "tool": "MISSING_TOOL",
  "parameters": {
    "requiredToolName": "DatabaseAnalyzer",
    "requiredCapabilities": ["schema:analyze", "table:inspect", "relation:map"],
    "sessionSafetyRequirements": "Must operate within session boundaries for database files",
    "reason": "Need to analyze database schema and relationships for Entity Framework model generation"
  },
  "confidence": 0.1,
  "assumptions": ["Database analysis tool would be required for this task"],
  "risks": ["Cannot provide database guidance without appropriate tools"],
  "response": "I need a DatabaseAnalyzer tool to examine database schemas. Current tools are insufficient for database operations."
}
```

**Tool Sufficiency Validation**:
- LLM evaluates available tools against task requirements
- Identifies gaps in tool capabilities
- Requests specific tool implementations with detailed requirements
- Provides fallback strategies when tools are missing

#### Internal Tool Self-Sufficiency Strategy

**No External Dependencies**: The system no longer relies on external tools or system utilities. All capabilities are provided through internal tool implementations:

**Eliminated External Dependencies**:
- ❌ **PowerShell/Bash**: Replaced with internal command execution within session boundaries
- ❌ **Git CLI**: Git operations handled through internal HTTP-based repository downloads
- ❌ **Python/Node.js**: No external runtime dependencies required
- ❌ **System Package Managers**: All functionality provided through internal tools
- ❌ **External Utilities**: No reliance on curl, wget, grep, sed, awk, etc.

**Internal Implementation Benefits**:
- ✅ **Session Isolation**: All operations contained within session boundaries
- ✅ **Security**: No external process execution outside session control
- ✅ **Reliability**: No dependency on host system tool installation
- ✅ **Consistency**: Identical behavior across all host environments
- ✅ **Audit Trail**: Complete operation logging within session context

**Tool Gap Handling**:
When the LLM identifies missing capabilities, it can request new internal tool implementations:

```json
{
  "reasoning": "Task requires text processing capabilities not available in current tools",
  "taskComplete": false,
  "nextStep": "Request implementation of TextProcessorTool for regex operations and text manipulation",
  "requiresTool": true,
  "tool": "MISSING_TOOL",
  "parameters": {
    "requiredToolName": "TextProcessorTool",
    "requiredCapabilities": ["regex:match", "text:replace", "file:grep"],
    "sessionSafetyRequirements": "Must operate within session file boundaries",
    "internalImplementation": "Required - no external grep/sed dependencies",
    "reason": "Need text processing for configuration file analysis"
  },
  "confidence": 0.3,
  "assumptions": ["Text processing tool would enable configuration analysis"],
  "risks": ["Cannot perform text manipulation without appropriate tools"],
  "response": "I need a TextProcessorTool for text manipulation. Current tools don't provide regex or grep functionality."
}
```
- **Development Tools**: gcc, make, etc.

**Cross-Platform Tools**:
- **Git**: Version control across all platforms
- **Python**: If installed, provides extensive capabilities
- **Node.js/npm**: JavaScript runtime and package management
- **Docker**: Containerization and deployment
- **curl/wget**: HTTP operations and file downloads

#### LLM Tool Awareness Strategy

**Initial Prompt Decoration**:
The system automatically generates comprehensive tool information for the LLM's initial prompt:

```
AVAILABLE TOOLS AND THEIR USAGE:
================================

INTERNAL TOOLS (Detailed Parameters):
• GitHubDownloader: Downloads GitHub repositories
  - Parameters: repoUrl (string), sessionId (string)
  - Usage: "Use GitHubDownloader tool to download https://github.com/user/repo"

• FileSystemAnalyzer: Analyzes directory structures
  - Parameters: directoryPath (string), includeSubdirectories (bool), minimumFileSize (int)
  - Usage: "Use FileSystemAnalyzer tool to analyze directory ./downloads/repo"

• CodeAnalyzer: Analyzes source code
  - Parameters: filePath (string), analysisType (string)
  - Usage: "Use CodeAnalyzer tool to analyze file ./src/Program.cs"

• MathEvaluator: Evaluates mathematical expressions
  - Parameters: expression (string)
  - Usage: "Use MathEvaluator tool to calculate: 15 * 8 + 42"

• ExternalCommandExecutor: Executes system commands
  - Parameters: command (string), workingDirectory (optional), timeoutSeconds (optional)
  - Usage: "Use ExternalCommandExecutor tool to run: git clone https://github.com/user/repo"

EXTERNAL TOOLS (Available via ExternalCommandExecutor):
• Git: Version control operations
• Python: Programming and scripting (if installed)
• PowerShell/Bash: System operations and scripting
• Package Managers: npm, pip, apt, yum, etc. (if available)
• Development Tools: dotnet, node, docker, etc. (if installed)
• System Utilities: curl, wget, grep, find, etc.
```

**Dynamic Capability Assessment**:
- **Platform Detection**: System detects Windows vs Linux and adjusts available external tools
- **Installation Verification**: Can test for tool availability using command existence checks
- **Fallback Strategies**: Multiple approaches for achieving the same goal across platforms

### Tool Extension Pattern

To add a new tool:

1. **Implement ITool interface**:
```csharp
public class CustomTool : ITool
{
    public string Name => "CustomTool";
    public string Description => "Performs custom operations";
    public IEnumerable<string> Capabilities => new[] { "custom:operation" };
    public bool RequiresNetwork => false;
    public bool RequiresFileSystem => true;
    
    public async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

2. **Register during startup**:
```csharp
toolRepository.RegisterTool(new CustomTool());
```

3. **AI automatically discovers** the tool through reflection and includes it in available capabilities

### Shell Command Integration and External Tool Fallbacks

**Universal Problem Solving Through Command Line**:

Everything that cannot be solved using internal tools can potentially be solved using shell commands through the `ExternalCommandExecutor`. This provides virtually unlimited capability expansion based on the host system.

#### Platform-Specific Command Resolution

**Windows Environment**:
```powershell
# PowerShell commands for system operations
Get-Process | Where-Object {$_.Name -eq "ollama"}
Test-Path "C:\Program Files\Git\bin\git.exe"
Invoke-WebRequest -Uri "https://api.github.com/repos/user/repo" -OutFile "repo.json"

# Command Prompt fallbacks
dir /s /b *.cs | findstr "Controller"
git status
python --version
```

**Linux Environment**:
```bash
# Standard Linux tools
find . -name "*.cs" -type f | xargs grep -l "Controller"
ps aux | grep ollama
which git python3 docker
curl -s https://api.github.com/repos/user/repo | jq '.stars'

# Package management
apt list --installed | grep python
yum list installed | grep git
```

#### LLM Decision Making for Tool Selection

The LLM receives comprehensive information about all available capabilities and makes intelligent decisions:

**Internal Tool First**: 
- Use specialized internal tools when available for better control and error handling
- Example: `GitHubDownloader` instead of `git clone` for repository analysis

**External Command Fallback**:
- When internal tools can't handle the requirement
- When system-specific operations are needed
- When leveraging widely-available external tools

**Decision Tree Example**:
```
User Request: "Download and analyze GitHub repository"
├─ Check: GitHubDownloader available? → YES → Use internal tool
├─ Check: Git command available? → YES → Use "git clone" via ExternalCommandExecutor  
├─ Check: curl/wget available? → YES → Use HTTP download via ExternalCommandExecutor
└─ Fallback: Report inability to complete task

User Request: "Install Python package"
├─ Check: Python available? → Test with "python --version"
├─ If available: → Use "pip install package" via ExternalCommandExecutor
└─ If not available: → Guide user to install Python first
```

#### System Capability Discovery

The system can dynamically discover what external tools are available:

```csharp
// Example capability detection commands
"git --version"           // Git availability
"python --version"        // Python availability  
"docker --version"        // Docker availability
"npm --version"          // Node.js/npm availability
"dotnet --version"       // .NET CLI availability
```

**LLM Integration**:
- System can test for tool availability as part of planning
- Results inform LLM about what approaches are viable
- Enables platform-specific optimization strategies

#### Security and Sandboxing

**Command Validation**:
- Input sanitization to prevent command injection
- Working directory restrictions to session boundaries
- Timeout management for long-running operations
- Output size limits to prevent resource exhaustion

**Safe Command Patterns**:
```
SAFE: git status
SAFE: python script.py
SAFE: dotnet build

BLOCKED: rm -rf /
BLOCKED: del C:\Windows\System32
BLOCKED: sudo commands (unless explicitly allowed)
```

---

## Session Management

### Session Lifecycle

1. **Session Creation**: Unique session ID generated for each conversation
2. **Directory Setup**: Isolated working directory created under `cache/[sessionId]/`
3. **Context Initialization**: Session context and conversation history initialized
4. **Interaction Logging**: All interactions logged with timestamps and artifacts
5. **State Persistence**: Conversation state saved after each interaction
6. **Session Cleanup**: Automatic cleanup based on retention policies

### Session Structure

```
cache/
└── [sessionId]/
    ├── session_context.json          # Session metadata
    ├── conversation_history.json     # Full conversation log
    ├── session_summary.json         # AI-generated summary
    ├── next_steps.txt               # Planned next actions
    ├── interactions/                # Interaction artifacts
    │   ├── 20250828_143022_initial_system_prompt.txt
    │   ├── 20250828_143023_query.txt
    │   ├── 20250828_143024_response_schema.json
    │   └── 20250828_143025_conversation_context.txt
    └── tools/                       # Tool-specific outputs
        ├── github_downloads/
        ├── analysis_results/
        └── command_outputs/
```

### Session Context Schema

```json
{
  "sessionId": "7b058c39-d21e-4b0e-b170-2ea72a8e4e2e",
  "createdAt": "2025-08-28T20:43:51Z",
  "lastUpdated": "2025-08-28T20:45:15Z",
  "strategy": "Pessimistic",
  "initialQuery": "Download and analyze https://github.com/user/repo",
  "currentStep": 3,
  "totalSteps": 5,
  "workingDirectory": "/cache/session-id",
  "toolsUsed": ["GitHubDownloader", "FileSystemAnalyzer"],
  "status": "InProgress",
  "metadata": {
    "userAgent": "CLI/1.0",
    "model": "llama3.3:70b-instruct-q3_K_M"
  }
}
```

---

## Strategic Modes

### 1. Pessimistic Strategy

**Philosophy**: Assume worst-case scenarios and plan comprehensive fallbacks

**Characteristics**:
- Extensive validation before action
- Multiple fallback strategies
- Conservative resource estimation
- Detailed risk assessment
- Step-by-step confirmation

**Use Cases**:
- Production system modifications
- Critical data operations
- Security-sensitive tasks
- High-stakes deployments

**Prompt Features**:
- Emphasizes backend development guidance
- Requires specific, actionable next steps
- Prohibits generic responses
- Enforces detailed planning

### 2. Optimistic Strategy

**Philosophy**: Assume best-case scenarios and optimize for speed

**Characteristics**:
- Rapid execution with minimal validation
- Streamlined decision-making
- Aggressive resource utilization
- Simplified error handling

**Use Cases**:
- Rapid prototyping
- Development environments
- Exploratory analysis
- Quick iterations

### 3. Balanced Strategy

**Philosophy**: Pragmatic approach balancing speed and safety

**Characteristics**:
- Moderate validation and risk assessment
- Flexible fallback strategies
- Reasonable resource allocation
- Balanced error handling

**Use Cases**:
- General-purpose operations
- Standard development tasks
- Mixed environments
- Default operation mode

---

## Configuration

### Application Settings (`appsettings.json`)

```json
{
  "AppSettings": {
    "DefaultMode": "Intelligent"
  },
  "OllamaSettings": {
    "BaseUrl": "http://localhost:11434",
    "ApiEndpoint": "http://localhost:11434/api",
    "ChatEndpoint": "http://localhost:11434/api/chat",
    "DefaultModel": "llama3.3:70b-instruct-q3_K_M",
    "CoderModel": "llama3.3:70b-instruct-q3_K_M",
    "ThinkerModel": "llama3.3:70b-instruct-q3_K_M",
    "ConnectionTimeout": 30,
    "RequestTimeout": 120,
    "MaxRetries": 3,
    "RetryDelay": 1000
  },
  "AgentSettings": {
    "SessionTimeout": 3600,
    "MaxConcurrentSessions": 10,
    "EnableTracing": true,
    "LogLevel": "Information"
  },
  "ToolSettings": {
    "MaxExecutionTime": 300,
    "EnableCaching": true,
    "CacheExpirationMinutes": 60
  }
}
```

### Model Configuration

The system supports multiple models for different purposes:

- **DefaultModel**: General-purpose reasoning and analysis
- **CoderModel**: Code-specific operations and programming tasks
- **ThinkerModel**: Complex reasoning and planning tasks

### Environment-Specific Settings

- **Development** (`appsettings.Development.json`): Relaxed timeouts, verbose logging
- **Production** (`appsettings.Production.json`): Optimized performance, security hardening
- **Testing** (`appsettings.Testing.json`): Mocked services, deterministic behavior

---

## Usage Examples

### 1. Simple Query Processing

**Command**:
```bash
dotnet run --project src/Ollama.Interface.Cli -- query "Explain how OAuth 2.0 works" --agent pessimistic
```

**AI Response Flow**:
1. Analyzes query as educational/explanatory
2. Determines no tools required
3. Provides comprehensive OAuth 2.0 explanation
4. Suggests next steps for implementation

### 2. Repository Analysis

**Command**:
```bash
dotnet run --project src/Ollama.Interface.Cli -- query "Download and analyze https://github.com/user/repo and identify the largest files" --agent pessimistic --verbose
```

**Execution Flow**:
1. **Analysis Phase**: AI determines need for GitHub download and file analysis
2. **Tool Selection**: Chooses GitHubDownloader and FileSystemAnalyzer
3. **Execution Phase**: Downloads repository, analyzes file structure
4. **Result Synthesis**: Combines tool outputs into comprehensive analysis
5. **Next Steps**: Provides specific recommendations for code optimization

### 3. Complex Multi-Step Operation

**Command**:
```bash
dotnet run --project src/Ollama.Interface.Cli -- query "Create a user management system with authentication and CRUD operations" --agent pessimistic
```

**Strategic Response**:
1. **Planning**: Breaks down into database design, API endpoints, authentication
2. **Backend Guidance**: Provides specific file/class/method instructions
3. **Progressive Implementation**: Guides through each development phase
4. **Testing Strategy**: Recommends unit and integration test approaches

### 4. Complex Multi-Step Operation with Full Iteration Cycle

**Command**:
```bash
dotnet run --project src/Ollama.Interface.Cli -- query "Download and analyze the testing strategy in https://github.com/microsoft/vscode then recommend improvements" --verbose
```

**Complete Iteration Sequence**:

**Iteration 1 - Initial Analysis:**
```
LLM Response: {
  "reasoning": "User wants analysis of testing strategy in VS Code repository. I need to download it first.",
  "taskComplete": false,
  "nextStep": "Download the GitHub repository for analysis",
  "requiresTool": true,
  "tool": "GitHubDownloader",
  "parameters": {"repoUrl": "https://github.com/microsoft/vscode", "sessionId": "session-123"}
}

System Action: Downloads VS Code repository to session directory
Tool Output: "Repository downloaded successfully. 45,234 files extracted."
```

**Iteration 2 - Structure Analysis:**
```
LLM Response: {
  "reasoning": "Repository downloaded. Now I need to understand the project structure to locate test-related files.",
  "taskComplete": false,
  "nextStep": "Analyze directory structure to identify test files and configurations",
  "requiresTool": true,
  "tool": "FileSystemAnalyzer",
  "parameters": {"directoryPath": "./downloads/vscode", "includeSubdirectories": true}
}

System Action: Analyzes file system structure
Tool Output: "Found test directories: /test/, /src/vs/test/, package.json with test scripts, .github/workflows/ with CI configs"
```

**Iteration 3 - Test Configuration Analysis:**
```
LLM Response: {
  "reasoning": "Found test directories and configs. Need to examine the main test configuration and package.json scripts.",
  "taskComplete": false,
  "nextStep": "Analyze package.json and test configuration files to understand test strategy",
  "requiresTool": true,
  "tool": "CodeAnalyzer",
  "parameters": {"filePath": "./downloads/vscode/package.json", "analysisType": "configuration"}
}

System Action: Analyzes package.json test configurations
Tool Output: "Test scripts: unit tests with Mocha, integration tests, smoke tests, CI/CD pipeline tests"
```

**Iteration 4 - Test Framework Analysis:**
```
LLM Response: {
  "reasoning": "Identified test types and scripts. Now need to examine actual test files to understand testing patterns.",
  "taskComplete": false,
  "nextStep": "Analyze test files to understand testing patterns and coverage",
  "requiresTool": true,
  "tool": "CodeAnalyzer",
  "parameters": {"filePath": "./downloads/vscode/test/", "analysisType": "pattern"}
}

System Action: Analyzes test file patterns and structures
Tool Output: "Testing patterns: Unit tests with Mocha/Sinon, integration tests, UI automation with Playwright, performance tests"
```

**Iteration 5 - Completion Assessment:**
```
LLM Response: {
  "reasoning": "I have analyzed the repository structure, test configurations, test scripts, and testing patterns. I now have comprehensive information about VS Code's testing strategy and can provide detailed analysis with improvement recommendations.",
  "taskComplete": true,
  "nextStep": "Task complete - providing comprehensive testing strategy analysis with recommendations",
  "requiresTool": false,
  "response": "## VS Code Testing Strategy Analysis

### Current Testing Approach:
1. **Unit Testing**: Mocha framework with Sinon for mocking
2. **Integration Testing**: Cross-component testing with real VS Code instances  
3. **UI Automation**: Playwright for end-to-end testing
4. **Performance Testing**: Dedicated performance test suites
5. **CI/CD Integration**: GitHub Actions with multi-platform testing

### Recommended Improvements:
1. **Test Coverage Enhancement**: 
   - Add more edge case testing for extension APIs
   - Implement visual regression testing for UI components
   
2. **Performance Testing**: 
   - Add memory leak detection tests
   - Implement startup time benchmarking
   
3. **Backend Development Steps**:
   - Create `test/api/extension-host.test.js` for extension API testing
   - Add `scripts/performance-benchmark.js` for automated performance tracking
   - Implement test data factories in `test/utils/factories.js`
   
### Next Development Actions:
1. Create comprehensive test data generators
2. Implement API contract testing for extension interfaces
3. Add automated accessibility testing pipeline"
}

✅ ITERATION COMPLETE - Original prompt fully satisfied with actionable recommendations
```

**Final Result:**
- ✅ Repository successfully downloaded and analyzed
- ✅ Complete testing strategy assessment provided
- ✅ Specific improvement recommendations generated  
- ✅ Concrete backend development steps outlined
- ✅ All tool executions logged and preserved

---

## Extension Guide

### Adding New Tools

1. **Define Tool Interface**:
```csharp
public class DatabaseAnalyzer : ITool
{
    public string Name => "DatabaseAnalyzer";
    public string Description => "Analyzes database schema and performance";
    public IEnumerable<string> Capabilities => new[] { "db:analyze", "schema:review", "performance:check" };
    public bool RequiresNetwork => true;
    public bool RequiresFileSystem => false;
    
    public async Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
    {
        // Database analysis implementation
        var connectionString = context.Parameters["connectionString"].ToString();
        var analysisType = context.Parameters["analysisType"].ToString();
        
        // Perform analysis...
        
        return new ToolResult
        {
            Success = true,
            Output = analysisResult,
            ExecutionTime = TimeSpan.FromSeconds(5)
        };
    }
}
```

2. **Register Tool**:
```csharp
// In ServiceRegistration.cs or during bootstrap
services.AddSingleton<ITool, DatabaseAnalyzer>();
```

3. **AI Integration**: Tool automatically becomes available to AI agents through reflection

### Adding New Strategic Modes

1. **Define Strategy**:
```csharp
public class AggressiveStrategy : IAgentStrategy
{
    public string Name => "Aggressive";
    public string GetSystemPrompt() => "You are an aggressive AI agent that prioritizes speed over safety...";
    public bool ShouldRetry(int attemptNumber, Exception exception) => attemptNumber < 1; // Minimal retries
}
```

2. **Configure Agent**:
```csharp
services.AddSingleton<IAgentStrategy, AggressiveStrategy>();
```

### Extending Communication Protocols

1. **Define New Schema**:
```csharp
public class ExtendedResponseSchema : LLMResponseSchema
{
    public SecurityAssessment Security { get; set; }
    public PerformanceMetrics Performance { get; set; }
    public ComplianceChecks Compliance { get; set; }
}
```

2. **Update Communication Service**:
```csharp
public class EnhancedCommunicationService : ILLMCommunicationService
{
    public ExtendedResponseSchema ParseExtendedResponse(string json)
    {
        // Enhanced parsing logic
    }
}
```

---

## Troubleshooting

### Common Issues

#### 1. Ollama Connection Problems

**Symptoms**: Application hangs, timeout errors, "connection refused"

**Solutions**:
- Verify Ollama is running: `ollama serve`
- Check port availability: `netstat -an | findstr :11434`
- Test connectivity: `curl http://localhost:11434/api/tags`
- Validate model availability: `ollama list`

#### 2. Model Loading Issues

**Symptoms**: "Model not found", extremely slow responses

**Solutions**:
- Pull required model: `ollama pull llama3.3:70b-instruct-q3_K_M`
- Use smaller model for development: Update configuration to `llama3.1:8b`
- Check available models: `ollama list`
- Verify model size compatibility with system resources

#### 3. Session Management Problems

**Symptoms**: Session conflicts, cache corruption, permission errors

**Solutions**:
- Clear cache: Use `--nc` flag or manually delete `cache/` directory
- Check file permissions on cache directory
- Verify session isolation by examining `cache/[sessionId]/` structure
- Review session timeout settings in configuration

#### 4. Tool Execution Failures

**Symptoms**: Tool timeouts, permission errors, unexpected outputs

**Solutions**:
- Verify tool requirements (network, filesystem access)
- Check working directory permissions
- Validate tool parameters in session logs
- Review tool-specific error messages in `interactions/` directory

#### 5. AI Response Quality Issues

**Symptoms**: Generic responses, missing backend guidance, incorrect tool selection

**Solutions**:
- Verify pessimistic prompt configuration includes backend development guidance
- Check model capability - larger models provide better reasoning
- Review system prompt effectiveness in session logs
- Adjust strategy mode based on query complexity

### Diagnostic Commands

```bash
# Test basic connectivity
dotnet run --project src/Ollama.Interface.Cli -- query "Hello test" --agent pessimistic -nc

# Verbose execution with cache clearing
dotnet run --project src/Ollama.Interface.Cli -- query "Test query" --agent pessimistic --verbose -nc

# Check available models
curl http://localhost:11434/api/tags

# Verify configuration
cat config/appsettings.json | jq .OllamaSettings

# Session diagnostics
ls -la cache/[sessionId]/interactions/
```

### Log Analysis

Key log locations:
- **Application logs**: Console output with structured logging
- **Session interactions**: `cache/[sessionId]/interactions/`
- **Conversation history**: `cache/[sessionId]/conversation_history.json`
- **Tool outputs**: `cache/[sessionId]/tools/`

### Performance Optimization

1. **Model Selection**: Use appropriate model size for task complexity
2. **Session Management**: Implement session cleanup policies
3. **Tool Caching**: Enable caching for frequently used tools
4. **Concurrent Limits**: Configure maximum concurrent sessions
5. **Timeout Tuning**: Adjust timeouts based on model performance

---

## System Architecture Summary: Iterative LLM Collaboration

The Ollama Agent Suite represents a sophisticated **iterative AI collaboration system** that addresses the fundamental limitations of Large Language Models through intelligent tool orchestration and persistent iteration.

### Core System Philosophy

#### LLM Limitations and System Response

**LLM Programming Task Limitation**: 
- **Problem**: LLMs can only handle programming-related reasoning tasks and cannot directly interact with file systems, networks, or execute commands
- **Solution**: Comprehensive tool ecosystem that provides LLMs with hands-on capabilities

**Iterative Collaboration Model**:
- 🔄 **Continuous Back-and-Forth**: System and LLM "joggle" back and forth until the initial user prompt is completely satisfied
- 🎯 **Goal Persistence**: Never stops iterating until LLM explicitly confirms full completion of original request
- 📈 **Progressive Enhancement**: Each iteration builds upon previous results and accumulated context

#### Tool-Augmented Intelligence

**Internal Tool Discovery (Reflection-Based)**:
- **Dynamic Enumeration**: System uses reflection to automatically discover all registered `ITool` implementations
- **Automatic Documentation**: Tool capabilities, parameters, and usage examples generated via reflection
- **LLM Integration**: Complete tool inventory provided to LLM in initial system prompt
- **Extensible Architecture**: New tools automatically integrated without manual prompt updates

**External Tool Integration (Shell Command Fallback)**:
- **Universal Problem Solving**: Everything not solvable via internal tools handled through shell commands
- **Platform Awareness**: System adapts to Windows (PowerShell/cmd) vs Linux (bash/shell) environments
- **Tool Availability Detection**: Dynamic discovery of installed external tools (git, python, docker, etc.)
- **No Detailed Explanation Required**: LLM already understands standard tools (git, python, npm, etc.)

#### Execution Flow Architecture

**JSON Response-Driven Tool Execution**:
- **Structured Communication**: All LLM responses follow strict JSON schema with `nextStep` field
- **Tool Selection**: LLM identifies required tools and provides parameters via JSON response
- **System Execution**: Backend executes tools based on LLM's JSON instructions
- **Result Integration**: Tool outputs fed back to LLM for next iteration planning

**Iteration Until Completion**:
```
User Query → LLM Analysis → Tool Requirement → System Execution → 
Result Analysis → Completion Check → [Continue Loop OR Final Response]
```

**Completion Criteria**:
- LLM explicitly sets `taskComplete: true` in JSON response
- LLM confirms original user prompt is fully satisfied
- All identified requirements have been addressed
- User receives comprehensive final response with complete solution

### Technical Implementation Excellence

**Strategic Architecture**:
- **Pessimistic Strategy Only**: Conservative, backend-focused execution for all queries
- **Comprehensive Validation**: Extensive risk assessment and assumption tracking
- **Session Isolation**: Complete isolation between concurrent operations
- **Audit Trail**: Full traceability of all decisions and executions

**Communication Protocol**:
- **Contract-Based**: Strict JSON schemas enforce reliable AI-backend communication
- **Context Preservation**: Rich conversation history maintained across iterations
- **Error Recovery**: Malformed responses trigger retry with enhanced prompts
- **Progressive Disclosure**: Multi-step operations supported with continuation patterns

**Tool Ecosystem Benefits**:
- **Unlimited Extensibility**: Add new capabilities without core system modifications
- **Platform Independence**: Cross-platform operation with platform-specific optimizations
- **Security Sandboxing**: Session-isolated execution with command validation
- **Intelligent Fallbacks**: Multiple approaches for achieving the same goal

### Unique Value Proposition

**What Sets This System Apart**:

1. **Persistent Goal Achievement**: Unlike single-response AI systems, continues until user's original intent is completely fulfilled

2. **Reflection-Based Tool Discovery**: Automatically maintains current tool inventory without manual prompt engineering

3. **Hybrid Internal/External Tool Strategy**: Combines specialized internal tools with unlimited external command capabilities

4. **Conservative Execution Model**: Pessimistic strategy ensures thorough analysis and safe execution

5. **Complete Session Management**: Full isolation, logging, and state preservation for complex multi-step operations

**For Developers and System Integrators**:
The Ollama Agent Suite provides a robust foundation for building AI-powered automation solutions that can scale from simple query processing to complex multi-step operations across diverse domains. Its iterative model ensures reliable completion of complex tasks while maintaining architectural integrity and operational safety.

**The Bottom Line**: This system transforms LLMs from single-shot text generators into persistent, tool-augmented problem-solving agents that won't give up until your task is completely done.
