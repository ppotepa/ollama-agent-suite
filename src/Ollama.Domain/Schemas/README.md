# Generic Response Schemas for LLM Clients

This folder contains standardized response schemas that work across different Large Language Model (LLM) providers, ensuring consistent data structures regardless of whether you're using Ollama, LM Studio, or other backends.

## 🎯 Purpose

The schema system provides:
- **Unified Response Format**: Same structure for all LLM providers
- **Rich Metadata**: Comprehensive information about tokens, timing, and performance
- **Type Safety**: Strong typing for reliable parsing and validation
- **Error Standardization**: Consistent error handling across all backends
- **Backward Compatibility**: Works alongside existing string-based APIs

## 📁 Schema Files

### Core Response Types

| File | Purpose | Usage |
|------|---------|-------|
| `BaseLLMResponseSchema.cs` | Base class for all responses | Inherited by all schema types |
| `ChatResponseSchema.cs` | Chat completion responses | Interactive conversations |
| `GenerationResponseSchema.cs` | Single prompt completions | Text generation tasks |
| `StreamingResponseSchema.cs` | Real-time streaming responses | Live text generation |
| `UnifiedResponseSchema.cs` | Generic wrapper for any response | Flexible response handling |

### Information & Status Types

| File | Purpose | Usage |
|------|---------|-------|
| `ModelInfoSchema.cs` | Model information and capabilities | Model discovery and selection |
| `HealthResponseSchema.cs` | Service health and monitoring | System status checks |
| `ErrorResponseSchema.cs` | Standardized error responses | Error handling and recovery |

### Service Layer

| File | Purpose | Usage |
|------|---------|-------|
| `LLMResponseMapper.cs` | Maps raw responses to schemas | Converts provider responses |
| `ResponseSchemaFactory.cs` | Creates and validates schemas | Schema instantiation |
| `LLMSchemaExtensions.cs` | Extension methods and wrappers | Easy integration |

## 🚀 Quick Start

### 1. Basic Usage with Existing Clients

```csharp
// Using extension methods for quick schema conversion
var chatSchema = await llmClient.GetChatSchemaAsync(
    "qwen2.5:7b", 
    messages, 
    responseMapper);

Console.WriteLine($"Response: {chatSchema.Content}");
Console.WriteLine($"Tokens: {chatSchema.Usage.TotalTokens}");
Console.WriteLine($"Provider: {chatSchema.Metadata.Provider}");
```

### 2. Enhanced Client Wrapper

```csharp
// Wrap existing client for full schema support
var enhancedClient = existingClient.WithSchemaSupport(responseMapper, schemaFactory);

// Now get structured responses
var chatResponse = await enhancedClient.ChatSchemaAsync("qwen2.5:7b", messages);
var models = await enhancedClient.GetModelSchemasAsync();
var health = await enhancedClient.GetHealthSchemaAsync();
```

### 3. Dependency Injection Setup

```csharp
// In Program.cs or Startup.cs
services.AddLLMSchemaServices();
services.AddEnhancedLLMClient<OllamaLLMClient>();

// Then inject and use
public class MyService
{
    private readonly IEnhancedLLMClient _llmClient;
    
    public MyService(IEnhancedLLMClient llmClient)
    {
        _llmClient = llmClient;
    }
    
    public async Task<string> GetResponse(string prompt)
    {
        var response = await _llmClient.ChatSchemaAsync("qwen2.5:7b", 
            new[] { ("user", prompt) }.ToList());
        
        return response.Content;
    }
}
```

## 📊 Schema Examples

### Chat Response
```json
{
  "responseId": "chat_001",
  "timestamp": "2024-01-15T10:30:00Z",
  "content": "Hello! How can I help you today?",
  "role": "assistant",
  "status": {
    "success": true,
    "statusCode": "OK",
    "message": "Chat completed successfully"
  },
  "metadata": {
    "model": "qwen2.5:7b-instruct-q4_K_M",
    "provider": "ollama",
    "tokensUsed": 25,
    "processingTimeMs": 1250,
    "temperature": 0.7,
    "finishReason": "stop"
  },
  "usage": {
    "promptTokens": 20,
    "completionTokens": 25,
    "totalTokens": 45
  }
}
```

### Model Information
```json
{
  "name": "qwen2.5:7b-instruct-q4_K_M",
  "displayName": "Qwen 2.5 7B Instruct Q4_K_M",
  "provider": "ollama",
  "capabilities": {
    "supportsChat": true,
    "supportsCompletion": true,
    "supportsStreaming": true,
    "supportedLanguages": ["en", "zh", "es", "fr"]
  },
  "status": {
    "available": true,
    "loaded": true,
    "healthStatus": "healthy"
  }
}
```

### Health Status
```json
{
  "healthy": true,
  "provider": "ollama",
  "performance": {
    "averageResponseTimeMs": 1250.5,
    "requestsPerSecond": 2.5,
    "errorRate": 0.02
  },
  "resources": {
    "memory": {
      "usagePercentage": 53.3
    },
    "gpu": {
      "available": true,
      "utilizationPercent": 75.0
    }
  }
}
```

## 🔧 Configuration

### Response Mapper Configuration
```csharp
var mapper = new LLMResponseMapper(logger);

// Map different provider responses
var ollamaResponse = mapper.MapChatResponse(rawOllama, "ollama", "qwen2.5:7b");
var lmStudioResponse = mapper.MapChatResponse(rawLMStudio, "lmstudio", "gpt-4o-mini");
```

### Schema Factory Usage
```csharp
var factory = new ResponseSchemaFactory();

// Create standardized responses
var chatSchema = factory.CreateChatResponse("Hello!", "qwen2.5:7b", "ollama");
var errorSchema = factory.CreateErrorResponse("Model not found", "NOT_FOUND", "ollama");
var healthSchema = factory.CreateHealthResponse(true, "ollama");
```

## 🎨 Provider Mapping

The system automatically handles different response formats from various providers:

### Ollama Format
```json
{
  "message": {
    "role": "assistant",
    "content": "Response text"
  },
  "done": true,
  "eval_count": 24,
  "eval_duration": 850000000
}
```

### LM Studio Format (OpenAI-compatible)
```json
{
  "choices": [
    {
      "message": {
        "role": "assistant",
        "content": "Response text"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 8,
    "completion_tokens": 16,
    "total_tokens": 24
  }
}
```

Both are mapped to the same `ChatResponseSchema` structure!

## 🧪 Testing

Run the schema demo to see all response types in action:

```csharp
// In Program.cs
SchemaSystemDemo.DemonstrateSchemaSystem();
```

This will show examples of all schema types with realistic data.

## 🔍 Error Handling

All schemas include comprehensive error information:

```csharp
if (!response.Status.Success)
{
    Console.WriteLine($"Error: {response.Status.Message}");
    
    if (response is ErrorResponseSchema errorResponse)
    {
        Console.WriteLine($"Code: {errorResponse.Error.Code}");
        Console.WriteLine($"Retryable: {errorResponse.Recovery.Retryable}");
        
        foreach (var suggestion in errorResponse.Recovery.Suggestions)
        {
            Console.WriteLine($"Suggestion: {suggestion}");
        }
    }
}
```

## 🚀 Migration Guide

### From String Responses
```csharp
// Old way
var response = await client.ChatAsync("qwen2.5:7b", messages);
Console.WriteLine(response); // Just text

// New way (backward compatible)
var schemaResponse = await client.ChatSchemaAsync("qwen2.5:7b", messages);
Console.WriteLine(schemaResponse.Content); // Same text
Console.WriteLine($"Tokens: {schemaResponse.Usage.TotalTokens}"); // Plus metadata!
```

### Adding to Existing Projects
1. Add schema services to DI: `services.AddLLMSchemaServices()`
2. Wrap existing clients: `client.WithSchemaSupport(mapper, factory)`
3. Use schema methods alongside existing string methods
4. Gradually migrate to schema-based responses

## 🏗️ Architecture

```
ILLMClient (existing)
    ↓
EnhancedLLMClientWrapper
    ↓
ILLMResponseMapper → ResponseSchemas
    ↓
IResponseSchemaFactory ← Validation
```

The schema system sits on top of existing clients, providing enhanced functionality without breaking existing code.

## 🎯 Benefits

- **🔄 Provider Agnostic**: Same code works with any LLM backend
- **📊 Rich Metadata**: Get insights into token usage, timing, and performance
- **🛡️ Type Safety**: Compile-time checking and IntelliSense support
- **🚨 Better Errors**: Standardized error handling with recovery suggestions
- **📈 Monitoring**: Built-in health checks and performance metrics
- **🔧 Extensible**: Easy to add new schema types and providers
- **🔒 Backward Compatible**: Existing string-based code continues to work

Ready to standardize your LLM responses across all providers! 🎉
