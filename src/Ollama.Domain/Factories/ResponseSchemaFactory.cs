using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Ollama.Domain.Schemas;

namespace Ollama.Domain.Factories;

/// <summary>
/// Factory for creating and validating response schemas
/// </summary>
public interface IResponseSchemaFactory
{
    /// <summary>
    /// Create a chat response schema
    /// </summary>
    ChatResponseSchema CreateChatResponse(string content, string model, string provider);

    /// <summary>
    /// Create a generation response schema
    /// </summary>
    GenerationResponseSchema CreateGenerationResponse(string content, string prompt, string model, string provider);

    /// <summary>
    /// Create a model info schema
    /// </summary>
    ModelInfoSchema CreateModelInfo(string name, string provider);

    /// <summary>
    /// Create a health response schema
    /// </summary>
    HealthResponseSchema CreateHealthResponse(bool healthy, string provider);

    /// <summary>
    /// Create an error response schema
    /// </summary>
    ErrorResponseSchema CreateErrorResponse(string message, string code, string provider);

    /// <summary>
    /// Create a streaming response schema
    /// </summary>
    StreamingResponseSchema CreateStreamingResponse(string content, bool done, string provider);

    /// <summary>
    /// Create a unified response wrapper
    /// </summary>
    UnifiedResponseSchema CreateUnifiedResponse<T>(T data, string responseType, string provider) where T : class;

    /// <summary>
    /// Validate a response schema
    /// </summary>
    ValidationResult ValidateSchema<T>(T schema) where T : BaseLLMResponseSchema;

    /// <summary>
    /// Convert any object to JSON with schema formatting
    /// </summary>
    string SerializeSchema<T>(T schema) where T : class;

    /// <summary>
    /// Deserialize JSON to specific schema type
    /// </summary>
    T? DeserializeSchema<T>(string json) where T : class;
}

/// <summary>
/// Implementation of response schema factory
/// </summary>
public class ResponseSchemaFactory : IResponseSchemaFactory
{
    private readonly JsonSerializerOptions _jsonOptions;

    public ResponseSchemaFactory()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public ChatResponseSchema CreateChatResponse(string content, string model, string provider)
    {
        return new ChatResponseSchema
        {
            ResponseId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Content = content,
            Role = "assistant",
            Status = new ResponseStatus
            {
                Success = true,
                StatusCode = "OK",
                Message = "Chat response created successfully"
            },
            Metadata = new LLMResponseMetadata
            {
                Model = model,
                Provider = provider,
                ProcessingTimeMs = 0
            },
            Context = new ChatContext
            {
                MessageCount = 1,
                Temperature = 0.7
            },
            Usage = new TokenUsage()
        };
    }

    public GenerationResponseSchema CreateGenerationResponse(string content, string prompt, string model, string provider)
    {
        return new GenerationResponseSchema
        {
            ResponseId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            GeneratedText = content,
            Prompt = prompt,
            Status = new ResponseStatus
            {
                Success = true,
                StatusCode = "OK",
                Message = "Generation response created successfully"
            },
            Metadata = new LLMResponseMetadata
            {
                Model = model,
                Provider = provider,
                ProcessingTimeMs = 0
            },
            Completion = new CompletionDetails
            {
                Done = true,
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                StopReason = "completed"
            },
            Usage = new GenerationUsage()
        };
    }

    public ModelInfoSchema CreateModelInfo(string name, string provider)
    {
        return new ModelInfoSchema
        {
            Name = name,
            DisplayName = FormatDisplayName(name),
            Provider = provider,
            Version = "unknown",
            Parameters = new ModelParameters
            {
                ContextLength = 2048,
                MaxTokens = 512,
                DefaultTemperature = 0.7,
                SupportedFormats = new List<string> { "text" }
            },
            Capabilities = new ModelCapabilities
            {
                SupportsChat = true,
                SupportsCompletion = true,
                SupportsSystemMessages = true,
                SupportedLanguages = new List<string> { "en" }
            },
            Status = new ModelStatus
            {
                Available = true,
                HealthStatus = "healthy"
            },
            Metadata = new Dictionary<string, object>()
        };
    }

    public HealthResponseSchema CreateHealthResponse(bool healthy, string provider)
    {
        return new HealthResponseSchema
        {
            ResponseId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Healthy = healthy,
            Provider = provider,
            Version = "1.0.0",
            Status = new ResponseStatus
            {
                Success = true,
                StatusCode = healthy ? "OK" : "UNHEALTHY",
                Message = healthy ? "Service is healthy" : "Service health check failed"
            },
            Metadata = new LLMResponseMetadata
            {
                Provider = provider
            },
            Services = new List<ServiceHealth>
            {
                new ServiceHealth
                {
                    Name = $"{provider}_api",
                    Status = healthy ? "healthy" : "unhealthy",
                    LastCheck = DateTime.UtcNow,
                    Details = new Dictionary<string, object>()
                }
            },
            Performance = new PerformanceMetrics
            {
                AverageResponseTimeMs = 0.0,
                RequestsPerSecond = 0.0,
                ErrorRate = 0.0,
                Uptime = TimeSpan.Zero
            },
            Resources = new ResourceUsage
            {
                Memory = new MemoryInfo(),
                CpuUsagePercent = 0.0,
                Gpu = new GpuInfo(),
                Disk = new DiskInfo(),
                Network = new NetworkInfo()
            }
        };
    }

    public ErrorResponseSchema CreateErrorResponse(string message, string code, string provider)
    {
        return new ErrorResponseSchema
        {
            ResponseId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Status = new ResponseStatus
            {
                Success = false,
                StatusCode = "ERROR",
                Message = "Error occurred during processing",
                Errors = new List<string> { message }
            },
            Metadata = new LLMResponseMetadata
            {
                Provider = provider
            },
            Error = new ErrorDetails
            {
                Code = code,
                Message = message,
                Type = "LLMError",
                Details = new Dictionary<string, object>()
            },
            Context = new ErrorContext
            {
                Operation = "unknown",
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString(),
                Request = new Dictionary<string, object>()
            },
            Recovery = new RecoveryInfo
            {
                Retryable = false,
                Suggestions = new List<string> { "Check error details and retry" },
                DocumentationUrl = ""
            }
        };
    }

    public StreamingResponseSchema CreateStreamingResponse(string content, bool done, string provider)
    {
        return new StreamingResponseSchema
        {
            ResponseId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Done = done,
            Status = new ResponseStatus
            {
                Success = true,
                StatusCode = "OK",
                Message = done ? "Streaming completed" : "Streaming in progress"
            },
            Metadata = new LLMResponseMetadata
            {
                Provider = provider
            },
            Chunk = new StreamChunk
            {
                Content = content,
                Delta = content,
                Index = 0,
                Role = "assistant",
                Timestamp = DateTime.UtcNow,
                FinishReason = done ? "stop" : null
            },
            Stream = new StreamInfo
            {
                StreamId = Guid.NewGuid().ToString(),
                StartTime = DateTime.UtcNow,
                EndTime = done ? DateTime.UtcNow : null,
                ConnectionStatus = done ? "completed" : "active"
            },
            FinalResponse = done ? content : ""
        };
    }

    public UnifiedResponseSchema CreateUnifiedResponse<T>(T data, string responseType, string provider) where T : class
    {
        return new UnifiedResponseSchema
        {
            ResponseId = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            ResponseType = responseType,
            Data = data,
            RawResponse = SerializeSchema(data),
            ProcessedAt = DateTime.UtcNow,
            Status = new ResponseStatus
            {
                Success = true,
                StatusCode = "OK",
                Message = "Unified response created successfully"
            },
            Metadata = new LLMResponseMetadata
            {
                Provider = provider
            },
            Processing = new ProcessingInfo
            {
                ParsingAttempts = 1,
                ParsingMethod = "direct",
                ValidationPassed = true,
                ProcessingDuration = TimeSpan.Zero,
                Transformations = new List<string>(),
                Warnings = new List<ProcessingWarning>()
            }
        };
    }

    public ValidationResult ValidateSchema<T>(T schema) where T : BaseLLMResponseSchema
    {
        var context = new ValidationContext(schema);
        var results = new List<ValidationResult>();
        
        var isValid = Validator.TryValidateObject(schema, context, results, true);
        
        if (isValid)
        {
            return ValidationResult.Success!;
        }
        
        var errorMessages = results.Select(r => r.ErrorMessage).Where(m => m != null);
        return new ValidationResult(string.Join("; ", errorMessages));
    }

    public string SerializeSchema<T>(T schema) where T : class
    {
        try
        {
            return JsonSerializer.Serialize(schema, _jsonOptions);
        }
        catch (Exception ex)
        {
            return $"{{\"error\": \"Failed to serialize schema: {ex.Message}\"}}";
        }
    }

    public T? DeserializeSchema<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private string FormatDisplayName(string name)
    {
        // Convert model names to more readable format
        return name.Replace(":", " ")
                  .Replace("-", " ")
                  .Replace("_", " ")
                  .Trim();
    }
}
