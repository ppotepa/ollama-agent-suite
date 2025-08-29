using System.Text.Json;
using Ollama.Domain.Schemas;
using Ollama.Domain.Services;
using Ollama.Domain.Factories;
using Ollama.Domain.Extensions;

namespace Ollama.Interface.Cli;

/// <summary>
/// Demonstration of the new generic response schema system
/// Shows how to use standardized responses across different LLM providers
/// </summary>
public static class SchemaSystemDemo
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void DemonstrateSchemaSystem()
    {
        Console.WriteLine("=== LLM Generic Response Schema System Demo ===\n");
        Console.WriteLine("🎯 Unified response formats for Ollama and LM Studio\n");

        // Demo 1: Chat Response Schema
        DemonstrateChatResponseSchema();

        // Demo 2: Generation Response Schema
        DemonstrateGenerationResponseSchema();

        // Demo 3: Model Info Schema
        DemonstrateModelInfoSchema();

        // Demo 4: Health Response Schema
        DemonstrateHealthResponseSchema();

        // Demo 5: Error Response Schema
        DemonstrateErrorResponseSchema();

        // Demo 6: Streaming Response Schema
        DemonstrateStreamingResponseSchema();

        // Demo 7: Unified Response Wrapper
        DemonstrateUnifiedResponseSchema();

        // Demo 8: Response Mapping
        DemonstrateResponseMapping();

        Console.WriteLine("\n✅ Schema System Benefits:");
        Console.WriteLine("🔹 Consistent response structure across all LLM providers");
        Console.WriteLine("🔹 Rich metadata and usage statistics");
        Console.WriteLine("🔹 Standardized error handling and recovery");
        Console.WriteLine("🔹 Type-safe response parsing");
        Console.WriteLine("🔹 Built-in validation and serialization");
        Console.WriteLine("🔹 Backward compatibility with existing clients");
        Console.WriteLine("\n🚀 Ready for production use with any LLM backend!");
    }

    private static void DemonstrateChatResponseSchema()
    {
        Console.WriteLine("📤 1. Chat Response Schema");
        Console.WriteLine("   Provider-agnostic chat completion responses\n");

        var chatResponse = new ChatResponseSchema
        {
            ResponseId = "chat_001",
            Content = "Hello! I'm an AI assistant. How can I help you today?",
            Role = "assistant",
            ConversationId = "conv_123",
            Status = new ResponseStatus
            {
                Success = true,
                StatusCode = "OK",
                Message = "Chat completed successfully"
            },
            Metadata = new LLMResponseMetadata
            {
                Model = "qwen2.5:7b-instruct-q4_K_M",
                Provider = "ollama",
                TokensUsed = 25,
                ProcessingTimeMs = 1250,
                Temperature = 0.7,
                FinishReason = "stop"
            },
            Context = new ChatContext
            {
                MessageCount = 2,
                TotalTokens = 45,
                SystemPrompt = "You are a helpful AI assistant.",
                Temperature = 0.7,
                ContextLength = 4096
            },
            Usage = new TokenUsage
            {
                PromptTokens = 20,
                CompletionTokens = 25,
                TotalTokens = 45
            }
        };

        Console.WriteLine(JsonSerializer.Serialize(chatResponse, JsonOptions));
        Console.WriteLine("\n" + new string('─', 80) + "\n");
    }

    private static void DemonstrateGenerationResponseSchema()
    {
        Console.WriteLine("📝 2. Generation Response Schema");
        Console.WriteLine("   Single prompt completion responses\n");

        var generationResponse = new GenerationResponseSchema
        {
            ResponseId = "gen_001",
            GeneratedText = "The capital of France is Paris, a beautiful city known for its art, culture, and history.",
            Prompt = "What is the capital of France?",
            Status = new ResponseStatus
            {
                Success = true,
                StatusCode = "OK",
                Message = "Generation completed successfully"
            },
            Metadata = new LLMResponseMetadata
            {
                Model = "gpt-4o-mini",
                Provider = "lmstudio",
                TokensUsed = 32,
                ProcessingTimeMs = 890,
                Temperature = 0.3,
                MaxTokens = 100
            },
            Completion = new CompletionDetails
            {
                Done = true,
                StopReason = "completed",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                EvalCount = 32,
                EvalDuration = 890_000_000, // nanoseconds
                TotalDuration = 1_100_000_000
            },
            Usage = new GenerationUsage
            {
                PromptTokens = 8,
                CompletionTokens = 24,
                TotalTokens = 32,
                TokensPerSecond = 36.0
            }
        };

        Console.WriteLine(JsonSerializer.Serialize(generationResponse, JsonOptions));
        Console.WriteLine("\n" + new string('─', 80) + "\n");
    }

    private static void DemonstrateModelInfoSchema()
    {
        Console.WriteLine("🤖 3. Model Info Schema");
        Console.WriteLine("   Standardized model information\n");

        var modelInfo = new ModelInfoSchema
        {
            Name = "qwen2.5:7b-instruct-q4_K_M",
            DisplayName = "Qwen 2.5 7B Instruct Q4_K_M",
            Provider = "ollama",
            Version = "2.5",
            Size = 4_800_000_000, // bytes
            Parameters = new ModelParameters
            {
                ContextLength = 32768,
                MaxTokens = 2048,
                DefaultTemperature = 0.7,
                SupportedFormats = new List<string> { "text", "chat" },
                Architecture = "transformer",
                Quantization = "Q4_K_M"
            },
            Capabilities = new ModelCapabilities
            {
                SupportsChat = true,
                SupportsCompletion = true,
                SupportsStreaming = true,
                SupportsFunctionCalling = false,
                SupportsSystemMessages = true,
                SupportedLanguages = new List<string> { "en", "zh", "es", "fr", "de" },
                SpecialFeatures = new List<string> { "instruction-following", "code-generation" }
            },
            Status = new ModelStatus
            {
                Available = true,
                Loaded = true,
                LastUsed = DateTime.UtcNow.AddMinutes(-5),
                LoadTime = TimeSpan.FromSeconds(12.5),
                MemoryUsage = 4_800_000_000,
                HealthStatus = "healthy"
            },
            Metadata = new Dictionary<string, object>
            {
                { "family", "qwen" },
                { "license", "apache-2.0" },
                { "download_url", "https://ollama.ai/qwen2.5:7b-instruct-q4_K_M" }
            }
        };

        Console.WriteLine(JsonSerializer.Serialize(modelInfo, JsonOptions));
        Console.WriteLine("\n" + new string('─', 80) + "\n");
    }

    private static void DemonstrateHealthResponseSchema()
    {
        Console.WriteLine("🏥 4. Health Response Schema");
        Console.WriteLine("   Comprehensive health monitoring\n");

        var healthResponse = new HealthResponseSchema
        {
            ResponseId = "health_001",
            Healthy = true,
            Provider = "ollama",
            Version = "0.3.12",
            Status = new ResponseStatus
            {
                Success = true,
                StatusCode = "OK",
                Message = "All systems operational"
            },
            Services = new List<ServiceHealth>
            {
                new ServiceHealth
                {
                    Name = "ollama_api",
                    Status = "healthy",
                    LastCheck = DateTime.UtcNow,
                    ResponseTimeMs = 45,
                    Details = new Dictionary<string, object>
                    {
                        { "endpoint", "http://localhost:11434" },
                        { "models_loaded", 3 }
                    }
                }
            },
            Performance = new PerformanceMetrics
            {
                AverageResponseTimeMs = 1250.5,
                RequestsPerSecond = 2.5,
                ErrorRate = 0.02,
                Uptime = TimeSpan.FromHours(48),
                TotalRequests = 1250,
                SuccessfulRequests = 1225,
                FailedRequests = 25
            },
            Resources = new ResourceUsage
            {
                Memory = new MemoryInfo
                {
                    UsedBytes = 8_589_934_592, // 8GB
                    AvailableBytes = 7_516_192_768, // 7GB
                    TotalBytes = 16_106_127_360, // 15GB
                    UsagePercentage = 53.3
                },
                CpuUsagePercent = 23.5,
                Gpu = new GpuInfo
                {
                    Available = true,
                    UtilizationPercent = 75.0,
                    MemoryUsedBytes = 10_737_418_240, // 10GB
                    MemoryTotalBytes = 12_884_901_888, // 12GB
                    TemperatureCelsius = 68.5,
                    Name = "NVIDIA RTX 4080"
                }
            }
        };

        Console.WriteLine(JsonSerializer.Serialize(healthResponse, JsonOptions));
        Console.WriteLine("\n" + new string('─', 80) + "\n");
    }

    private static void DemonstrateErrorResponseSchema()
    {
        Console.WriteLine("❌ 5. Error Response Schema");
        Console.WriteLine("   Standardized error handling\n");

        var errorResponse = new ErrorResponseSchema
        {
            ResponseId = "error_001",
            Status = new ResponseStatus
            {
                Success = false,
                StatusCode = "MODEL_NOT_FOUND",
                Message = "The specified model is not available",
                Errors = new List<string> { "Model 'invalid-model' not found in registry" }
            },
            Error = new ErrorDetails
            {
                Code = "MODEL_NOT_FOUND",
                Message = "The specified model 'invalid-model' is not available on this provider",
                Type = "ClientError",
                Details = new Dictionary<string, object>
                {
                    { "requested_model", "invalid-model" },
                    { "available_models", new List<string> { "qwen2.5:7b", "llama3.1:8b" } }
                }
            },
            Context = new ErrorContext
            {
                Operation = "chat_completion",
                Endpoint = "/api/chat",
                Model = "invalid-model",
                Timestamp = DateTime.UtcNow,
                CorrelationId = "req_12345",
                Request = new Dictionary<string, object>
                {
                    { "model", "invalid-model" },
                    { "messages", "..." }
                }
            },
            Recovery = new RecoveryInfo
            {
                Retryable = false,
                FallbackAvailable = true,
                Suggestions = new List<string>
                {
                    "Use 'qwen2.5:7b' or 'llama3.1:8b' instead",
                    "Check available models with GET /api/tags",
                    "Verify model name spelling"
                },
                DocumentationUrl = "https://docs.ollama.ai/models"
            }
        };

        Console.WriteLine(JsonSerializer.Serialize(errorResponse, JsonOptions));
        Console.WriteLine("\n" + new string('─', 80) + "\n");
    }

    private static void DemonstrateStreamingResponseSchema()
    {
        Console.WriteLine("🌊 6. Streaming Response Schema");
        Console.WriteLine("   Real-time streaming responses\n");

        var streamingResponse = new StreamingResponseSchema
        {
            ResponseId = "stream_001",
            Done = false,
            Status = new ResponseStatus
            {
                Success = true,
                StatusCode = "STREAMING",
                Message = "Stream active"
            },
            Chunk = new StreamChunk
            {
                Content = "The weather",
                Delta = " today",
                Index = 5,
                Role = "assistant",
                Timestamp = DateTime.UtcNow,
                Usage = new ChunkUsage
                {
                    TokensInChunk = 2,
                    CompletionTokens = 12,
                    TotalTokens = 20
                }
            },
            Stream = new StreamInfo
            {
                StreamId = "stream_session_001",
                StartTime = DateTime.UtcNow.AddSeconds(-5),
                TotalChunks = 6,
                BytesTransferred = 45,
                AverageChunkSize = 7.5,
                TokensPerSecond = 15.2,
                ConnectionStatus = "active"
            },
            Metadata = new LLMResponseMetadata
            {
                Provider = "lmstudio",
                Model = "gpt-4o-mini"
            }
        };

        Console.WriteLine(JsonSerializer.Serialize(streamingResponse, JsonOptions));
        Console.WriteLine("\n" + new string('─', 80) + "\n");
    }

    private static void DemonstrateUnifiedResponseSchema()
    {
        Console.WriteLine("🔄 7. Unified Response Schema");
        Console.WriteLine("   Generic wrapper for any response type\n");

        var chatData = new { content = "Hello!", role = "assistant" };
        
        var unifiedResponse = new UnifiedResponseSchema
        {
            ResponseId = "unified_001",
            ResponseType = "chat_completion",
            Data = chatData,
            RawResponse = JsonSerializer.Serialize(chatData),
            ProcessedAt = DateTime.UtcNow,
            Status = new ResponseStatus
            {
                Success = true,
                StatusCode = "OK",
                Message = "Response processed successfully"
            },
            Metadata = new LLMResponseMetadata
            {
                Provider = "ollama",
                Model = "qwen2.5:7b"
            },
            Processing = new ProcessingInfo
            {
                ParsingAttempts = 1,
                ParsingMethod = "json_standard",
                FallbackUsed = false,
                ValidationPassed = true,
                ProcessingDuration = TimeSpan.FromMilliseconds(15),
                Transformations = new List<string> { "json_parse", "schema_mapping" },
                Warnings = new List<ProcessingWarning>()
            }
        };

        Console.WriteLine(JsonSerializer.Serialize(unifiedResponse, JsonOptions));
        Console.WriteLine("\n" + new string('─', 80) + "\n");
    }

    private static void DemonstrateResponseMapping()
    {
        Console.WriteLine("🗺️ 8. Response Mapping System");
        Console.WriteLine("   Converting raw LLM responses to schemas\n");

        // Simulate raw Ollama response
        var rawOllamaResponse = """
        {
            "message": {
                "role": "assistant",
                "content": "Paris is the capital of France."
            },
            "done": true,
            "eval_count": 24,
            "eval_duration": 850000000
        }
        """;

        // Simulate raw LM Studio response
        var rawLMStudioResponse = """
        {
            "choices": [
                {
                    "message": {
                        "role": "assistant",
                        "content": "Paris is the capital of France."
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
        """;

        Console.WriteLine("Raw Ollama Response:");
        Console.WriteLine(rawOllamaResponse);
        Console.WriteLine("\nMapped to ChatResponseSchema:");
        
        // This would be done by the LLMResponseMapper in practice
        var mappedOllamaResponse = new ChatResponseSchema
        {
            Content = "Paris is the capital of France.",
            Metadata = new LLMResponseMetadata
            {
                Provider = "ollama",
                TokensUsed = 24,
                ProcessingTimeMs = 850
            },
            Status = new ResponseStatus { Success = true, StatusCode = "OK" }
        };
        
        Console.WriteLine(JsonSerializer.Serialize(mappedOllamaResponse, JsonOptions));
        
        Console.WriteLine("\n" + new string('─', 40));
        Console.WriteLine("Raw LM Studio Response:");
        Console.WriteLine(rawLMStudioResponse);
        Console.WriteLine("\nMapped to ChatResponseSchema:");
        
        var mappedLMStudioResponse = new ChatResponseSchema
        {
            Content = "Paris is the capital of France.",
            Usage = new TokenUsage
            {
                PromptTokens = 8,
                CompletionTokens = 16,
                TotalTokens = 24
            },
            Metadata = new LLMResponseMetadata
            {
                Provider = "lmstudio",
                TokensUsed = 24,
                FinishReason = "stop"
            },
            Status = new ResponseStatus { Success = true, StatusCode = "OK" }
        };
        
        Console.WriteLine(JsonSerializer.Serialize(mappedLMStudioResponse, JsonOptions));
        Console.WriteLine("\n🎯 Same schema format regardless of provider!");
    }
}
