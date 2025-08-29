# Dual LLM Client System Implementation

## Overview

The Ollama Agent Suite now supports multiple LLM backends through a unified interface. You can seamlessly switch between **Ollama** and **LM Studio** by changing a single configuration parameter.

## Configuration

### Switch to Ollama (Default)
```json
{
  "DefaultClient": "ollama",
  "OllamaSettings": {
    "BaseUrl": "http://localhost:11434",
    "DefaultModel": "llama3.1:8b-instruct-q4_K_M"
  }
}
```

### Switch to LM Studio
```json
{
  "DefaultClient": "lmstudio",
  "LMStudioSettings": {
    "BaseUrl": "http://localhost:1234",
    "DefaultModel": "llama-3.1-8b-instruct",
    "Temperature": 0.1,
    "MaxTokens": 2048
  }
}
```

## Architecture

### Common Interface
All LLM clients implement the `ILLMClient` interface:
- `ChatAsync()` - Chat with conversation context
- `GenerateAsync()` - Single generation request
- `GetAvailableModelsAsync()` - List available models
- `IsHealthyAsync()` - Health check

### Client Implementations
1. **OllamaLLMClient** - Connects to Ollama API
2. **LMStudioLLMClient** - Connects to LM Studio OpenAI-compatible API

### Factory Pattern
The `LLMClientFactory` creates the appropriate client based on configuration:
```csharp
ILLMClient client = factory.CreateClient(); // Returns Ollama or LM Studio client
```

### Unified Client Wrapper
The `UnifiedLLMClient` provides backward compatibility while using the new factory system.

## Usage Examples

### Basic Configuration Switch
Change `DefaultClient` in `appsettings.json`:
```json
{
  "DefaultClient": "ollama"     // Use Ollama
  "DefaultClient": "lmstudio"   // Use LM Studio
}
```

### Model Configuration
Each client has its own model settings:

**Ollama Models:**
```json
"OllamaSettings": {
  "DefaultModel": "llama3.1:8b-instruct-q4_K_M",
  "CoderModel": "llama3.1:8b-instruct-q4_K_M",
  "ThinkerModel": "llama3.1:8b-instruct-q4_K_M"
}
```

**LM Studio Models:**
```json
"LMStudioSettings": {
  "DefaultModel": "llama-3.1-8b-instruct",
  "CoderModel": "codellama-7b-instruct",
  "ThinkerModel": "llama-3.1-8b-instruct"
}
```

## Benefits

### 1. **Flexibility**
- Switch between LLM backends without code changes
- Different backends for different use cases
- Easy testing and comparison

### 2. **Backward Compatibility**
- Existing code works unchanged
- Same interface for all clients
- Seamless migration

### 3. **LM Studio Advantages**
- More model options and formats
- Better GPU utilization options
- Advanced quantization settings
- Fine-tuned model support

### 4. **Configuration-Driven**
- No hardcoded dependencies
- Runtime client selection
- Easy deployment configuration

## Getting Started

1. **Install LM Studio** (if using LM Studio client)
   - Download from [lmstudio.ai](https://lmstudio.ai/)
   - Start local server on port 1234

2. **Update Configuration**
   ```json
   {
     "DefaultClient": "lmstudio",
     "LMStudioSettings": {
       "BaseUrl": "http://localhost:1234",
       "DefaultModel": "your-model-name"
     }
   }
   ```

3. **Run Application**
   ```bash
   dotnet run --project src/Ollama.Interface.Cli --query "test query"
   ```

The application will automatically use the configured LLM client without any code changes!

## Troubleshooting

### Connection Issues
- **Ollama**: Ensure Ollama server is running on `http://localhost:11434`
- **LM Studio**: Ensure LM Studio server is running on `http://localhost:1234`

### Model Not Found
- Check available models with health check endpoints
- Verify model names in configuration match server models

### Configuration Errors
- Validate JSON syntax in `appsettings.json`
- Check that all required settings are present
- Ensure `DefaultClient` value is either "ollama" or "lmstudio"
