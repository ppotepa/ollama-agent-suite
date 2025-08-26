# OllamaAgentSuite

A modular agent runner built with strong SRP and SOLID principles for orchestrating intelligent agents in different collaboration styles.

## Architecture

This application follows clean architecture principles with clear layer separation:

- **Domain** → Pure contracts (interfaces, entities, strategies)
- **Application** → Orchestration logic, use-cases, services  
- **Infrastructure** → Adapters for agents, tools, file system, etc.
- **Interface** → CLI or HTTP front-end
- **Bootstrap** → DI container and wiring

## Features

The application supports three execution modes:

### 🔹 Single-Query Mode
- Uses one agent
- Minimal orchestration
- Example: `"Summarize this text"`

### 🔹 Collaborative Mode  
- Uses multiple agents (e.g., thinker + coder)
- Can involve tool adapters (CLI, repo ops, etc.)
- Example: `"Figure out how to update this project and then make the change"`

### 🔹 Intelligent Mode
- Has a thinking agent that dynamically builds an execution tree
- Decides when to swap or activate other agents
- More autonomous, flexible orchestration
- Example: `"Debug this system and fix what's broken"`

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- Ollama running locally (optional for full functionality)

### Build and Run

```bash
# Clone the repository
git clone <repository-url>
cd ollama-multiagent

# Build the solution
dotnet build OllamaAgentSuite.sln

# Run tests
dotnet test OllamaAgentSuite.sln

# Run the CLI application
dotnet run --project src/Ollama.Interface.Cli/Ollama.Interface.Cli.csproj "Your query here"

# Specify a particular mode
dotnet run --project src/Ollama.Interface.Cli/Ollama.Interface.Cli.csproj "Your query" "collaborative"
```

### Examples

```bash
# Single query mode (automatic selection)
dotnet run --project src/Ollama.Interface.Cli/Ollama.Interface.Cli.csproj "What is machine learning?"

# Collaborative mode (automatic selection based on keywords)
dotnet run --project src/Ollama.Interface.Cli/Ollama.Interface.Cli.csproj "Figure out the best approach and then implement it"

# Intelligent mode (automatic selection based on keywords)
dotnet run --project src/Ollama.Interface.Cli/Ollama.Interface.Cli.csproj "Debug this complex system autonomously"

# Force a specific mode
dotnet run --project src/Ollama.Interface.Cli/Ollama.Interface.Cli.csproj "Simple question" "intelligent"
```

## Project Structure

```
OllamaAgentSuite/
├── src/
│   ├── Ollama.Domain/           # Core domain logic
│   │   ├── Agents/              # Agent interfaces and ports
│   │   ├── Execution/           # Execution tree and node types
│   │   └── Strategies/          # Strategy patterns and contexts
│   ├── Ollama.Application/      # Application services and orchestration
│   │   ├── Modes/               # Strategy implementations
│   │   ├── Orchestrator/        # Main orchestration logic
│   │   └── Services/            # Application services
│   ├── Ollama.Infrastructure/   # External adapters and implementations
│   │   └── Agents/              # Agent implementations
│   ├── Ollama.Bootstrap/        # Dependency injection setup
│   │   └── Composition/         # Service registration
│   └── Ollama.Interface.Cli/    # Command-line interface
├── tests/                       # Unit tests
│   ├── Ollama.Tests.Domain/
│   ├── Ollama.Tests.Application/
│   └── Ollama.Tests.Infrastructure/
├── config/                      # Configuration files
└── docs/                        # Documentation
```

## Key Benefits

- **Traceability**: Reasoning steps and actions are recorded in an execution tree
- **Extensibility**: Add new agents, tools, or orchestration modes without changing existing core logic
- **Testability**: Domain & application code are free of I/O; infrastructure is pluggable
- **SRP Enforced**: Thinking, execution, orchestration, and context management are all separate

## Configuration

The application uses configuration files in the `config/` directory. See `config/appsettings.json` for Ollama connection settings and model configurations.

## Contributing

1. Follow the established clean architecture patterns
2. Ensure all tests pass before submitting changes
3. Add appropriate unit tests for new functionality
4. Follow SOLID principles and maintain clear separation of concerns

## License

[Add your license information here]
