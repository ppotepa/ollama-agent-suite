using System.Text.Json;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Ollama.Domain.Schemas;

namespace Ollama.Domain.Services;

/// <summary>
/// Service for mapping raw LLM responses to standardized schemas
/// Handles conversion from provider-specific formats to generic schemas
/// </summary>
public interface ILLMResponseMapper
{
    /// <summary>
    /// Map a raw chat response to standardized chat schema
    /// </summary>
    /// <param name="rawResponse">Raw response from LLM provider</param>
    /// <param name="provider">Provider name (ollama, lmstudio)</param>
    /// <param name="model">Model name used</param>
    /// <returns>Standardized chat response schema</returns>
    ChatResponseSchema MapChatResponse(string rawResponse, string provider, string model);

    /// <summary>
    /// Map a raw generation response to standardized generation schema
    /// </summary>
    /// <param name="rawResponse">Raw response from LLM provider</param>
    /// <param name="provider">Provider name (ollama, lmstudio)</param>
    /// <param name="model">Model name used</param>
    /// <param name="prompt">Original prompt</param>
    /// <returns>Standardized generation response schema</returns>
    GenerationResponseSchema MapGenerationResponse(string rawResponse, string provider, string model, string prompt);

    /// <summary>
    /// Map raw model list to standardized model info schemas
    /// </summary>
    /// <param name="rawModels">Raw model list from provider</param>
    /// <param name="provider">Provider name (ollama, lmstudio)</param>
    /// <returns>List of standardized model info schemas</returns>
    List<ModelInfoSchema> MapModelList(List<string> rawModels, string provider);

    /// <summary>
    /// Map raw health check to standardized health schema
    /// </summary>
    /// <param name="isHealthy">Basic health status</param>
    /// <param name="provider">Provider name (ollama, lmstudio)</param>
    /// <param name="additionalInfo">Additional health information</param>
    /// <returns>Standardized health response schema</returns>
    HealthResponseSchema MapHealthResponse(bool isHealthy, string provider, Dictionary<string, object>? additionalInfo = null);

    /// <summary>
    /// Create standardized error response from exception
    /// </summary>
    /// <param name="exception">Exception that occurred</param>
    /// <param name="provider">Provider name (ollama, lmstudio)</param>
    /// <param name="operation">Operation that failed</param>
    /// <returns>Standardized error response schema</returns>
    ErrorResponseSchema MapErrorResponse(Exception exception, string provider, string operation);

    /// <summary>
    /// Create unified response wrapper for any response type
    /// </summary>
    /// <param name="data">Response data</param>
    /// <param name="responseType">Type of response</param>
    /// <param name="rawResponse">Raw response string</param>
    /// <param name="provider">Provider name</param>
    /// <returns>Unified response schema</returns>
    UnifiedResponseSchema CreateUnifiedResponse(object data, string responseType, string rawResponse, string provider);
}

/// <summary>
/// Implementation of LLM response mapper
/// </summary>
public class LLMResponseMapper : ILLMResponseMapper
{
    private readonly ILogger<LLMResponseMapper> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public LLMResponseMapper(ILogger<LLMResponseMapper> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public ChatResponseSchema MapChatResponse(string rawResponse, string provider, string model)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogDebug("Mapping chat response from {Provider} using model {Model}", provider, model);

            var schema = new ChatResponseSchema
            {
                Content = ExtractContentFromResponse(rawResponse, provider),
                Role = "assistant",
                Metadata = new LLMResponseMetadata
                {
                    Model = model,
                    Provider = provider,
                    ProcessingTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                },
                Status = new ResponseStatus
                {
                    Success = true,
                    StatusCode = "OK",
                    Message = "Chat response processed successfully"
                }
            };

            // Try to extract additional metadata based on provider
            if (provider.ToLower() == "ollama")
            {
                PopulateOllamaMetadata(schema, rawResponse);
            }
            else if (provider.ToLower() == "lmstudio")
            {
                PopulateLMStudioMetadata(schema, rawResponse);
            }

            _logger.LogDebug("Successfully mapped chat response from {Provider}", provider);
            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map chat response from {Provider}", provider);
            return CreateErrorChatResponse(ex, provider, model);
        }
    }

    public GenerationResponseSchema MapGenerationResponse(string rawResponse, string provider, string model, string prompt)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogDebug("Mapping generation response from {Provider} using model {Model}", provider, model);

            var schema = new GenerationResponseSchema
            {
                GeneratedText = ExtractContentFromResponse(rawResponse, provider),
                Prompt = prompt,
                Metadata = new LLMResponseMetadata
                {
                    Model = model,
                    Provider = provider,
                    ProcessingTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds
                },
                Status = new ResponseStatus
                {
                    Success = true,
                    StatusCode = "OK",
                    Message = "Generation response processed successfully"
                },
                Completion = new CompletionDetails
                {
                    Done = true,
                    Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    StopReason = "completed"
                }
            };

            // Try to extract additional metadata based on provider
            if (provider.ToLower() == "ollama")
            {
                PopulateOllamaGenerationMetadata(schema, rawResponse);
            }
            else if (provider.ToLower() == "lmstudio")
            {
                PopulateLMStudioGenerationMetadata(schema, rawResponse);
            }

            _logger.LogDebug("Successfully mapped generation response from {Provider}", provider);
            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map generation response from {Provider}", provider);
            return CreateErrorGenerationResponse(ex, provider, model, prompt);
        }
    }

    public List<ModelInfoSchema> MapModelList(List<string> rawModels, string provider)
    {
        try
        {
            _logger.LogDebug("Mapping model list from {Provider} with {Count} models", provider, rawModels.Count);

            var modelSchemas = rawModels.Select(modelName => new ModelInfoSchema
            {
                Name = modelName,
                DisplayName = FormatDisplayName(modelName),
                Provider = provider,
                Status = new ModelStatus
                {
                    Available = true,
                    HealthStatus = "healthy"
                },
                Capabilities = new ModelCapabilities
                {
                    SupportsChat = true,
                    SupportsCompletion = true,
                    SupportsSystemMessages = true
                }
            }).ToList();

            _logger.LogDebug("Successfully mapped {Count} models from {Provider}", modelSchemas.Count, provider);
            return modelSchemas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map model list from {Provider}", provider);
            return new List<ModelInfoSchema>();
        }
    }

    public HealthResponseSchema MapHealthResponse(bool isHealthy, string provider, Dictionary<string, object>? additionalInfo = null)
    {
        try
        {
            var schema = new HealthResponseSchema
            {
                Healthy = isHealthy,
                Provider = provider,
                Status = new ResponseStatus
                {
                    Success = true,
                    StatusCode = isHealthy ? "OK" : "UNHEALTHY",
                    Message = isHealthy ? "Provider is healthy" : "Provider health check failed"
                },
                Services = new List<ServiceHealth>
                {
                    new ServiceHealth
                    {
                        Name = $"{provider}_api",
                        Status = isHealthy ? "healthy" : "unhealthy",
                        LastCheck = DateTime.UtcNow,
                        Details = additionalInfo ?? new Dictionary<string, object>()
                    }
                }
            };

            return schema;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to map health response from {Provider}", provider);
            return new HealthResponseSchema
            {
                Healthy = false,
                Provider = provider,
                Status = new ResponseStatus
                {
                    Success = false,
                    StatusCode = "ERROR",
                    Message = "Health check mapping failed",
                    Errors = new List<string> { ex.Message }
                }
            };
        }
    }

    public ErrorResponseSchema MapErrorResponse(Exception exception, string provider, string operation)
    {
        return new ErrorResponseSchema
        {
            Status = new ResponseStatus
            {
                Success = false,
                StatusCode = "ERROR",
                Message = "LLM operation failed",
                Errors = new List<string> { exception.Message }
            },
            Metadata = new LLMResponseMetadata
            {
                Provider = provider
            },
            Error = new ErrorDetails
            {
                Code = exception.GetType().Name,
                Message = exception.Message,
                Type = "LLMError",
                StackTrace = exception.StackTrace ?? string.Empty
            },
            Context = new ErrorContext
            {
                Operation = operation,
                Timestamp = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid().ToString()
            },
            Recovery = new RecoveryInfo
            {
                Retryable = IsRetryableException(exception),
                Suggestions = GetRecoverySuggestions(exception, provider)
            }
        };
    }

    public UnifiedResponseSchema CreateUnifiedResponse(object data, string responseType, string rawResponse, string provider)
    {
        return new UnifiedResponseSchema
        {
            ResponseType = responseType,
            Data = data,
            RawResponse = rawResponse,
            ProcessedAt = DateTime.UtcNow,
            Metadata = new LLMResponseMetadata
            {
                Provider = provider
            },
            Status = new ResponseStatus
            {
                Success = true,
                StatusCode = "OK",
                Message = "Response processed successfully"
            },
            Processing = new ProcessingInfo
            {
                ParsingMethod = "standard",
                ValidationPassed = true,
                ProcessingDuration = TimeSpan.Zero
            }
        };
    }

    private string ExtractContentFromResponse(string rawResponse, string provider)
    {
        try
        {
            if (string.IsNullOrEmpty(rawResponse))
                return string.Empty;

            // Try to parse as JSON first
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(rawResponse);

            if (provider.ToLower() == "ollama")
            {
                if (jsonElement.TryGetProperty("message", out var messageElement) &&
                    messageElement.TryGetProperty("content", out var contentElement))
                {
                    return contentElement.GetString() ?? rawResponse;
                }
                if (jsonElement.TryGetProperty("response", out var responseElement))
                {
                    return responseElement.GetString() ?? rawResponse;
                }
            }
            else if (provider.ToLower() == "lmstudio")
            {
                if (jsonElement.TryGetProperty("choices", out var choicesElement) &&
                    choicesElement.ValueKind == JsonValueKind.Array &&
                    choicesElement.GetArrayLength() > 0)
                {
                    var firstChoice = choicesElement[0];
                    if (firstChoice.TryGetProperty("message", out var messageElement) &&
                        messageElement.TryGetProperty("content", out var contentElement))
                    {
                        return contentElement.GetString() ?? rawResponse;
                    }
                }
            }

            return rawResponse;
        }
        catch
        {
            // If JSON parsing fails, return the raw response
            return rawResponse;
        }
    }

    private void PopulateOllamaMetadata(ChatResponseSchema schema, string rawResponse)
    {
        try
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(rawResponse);
            
            if (jsonElement.TryGetProperty("eval_count", out var evalCountElement))
            {
                schema.Metadata.TokensUsed = evalCountElement.GetInt32();
            }
            
            if (jsonElement.TryGetProperty("eval_duration", out var evalDurationElement))
            {
                schema.Metadata.ProcessingTimeMs = (int)(evalDurationElement.GetInt64() / 1_000_000); // nanoseconds to milliseconds
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to populate Ollama metadata");
        }
    }

    private void PopulateLMStudioMetadata(ChatResponseSchema schema, string rawResponse)
    {
        try
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(rawResponse);
            
            if (jsonElement.TryGetProperty("usage", out var usageElement))
            {
                if (usageElement.TryGetProperty("total_tokens", out var totalTokensElement))
                {
                    schema.Metadata.TokensUsed = totalTokensElement.GetInt32();
                }
                
                if (usageElement.TryGetProperty("prompt_tokens", out var promptTokensElement) &&
                    usageElement.TryGetProperty("completion_tokens", out var completionTokensElement))
                {
                    schema.Usage.PromptTokens = promptTokensElement.GetInt32();
                    schema.Usage.CompletionTokens = completionTokensElement.GetInt32();
                    schema.Usage.TotalTokens = schema.Usage.PromptTokens + schema.Usage.CompletionTokens;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to populate LM Studio metadata");
        }
    }

    private void PopulateOllamaGenerationMetadata(GenerationResponseSchema schema, string rawResponse)
    {
        try
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(rawResponse);
            
            if (jsonElement.TryGetProperty("eval_count", out var evalCountElement))
            {
                schema.Completion.EvalCount = evalCountElement.GetInt32();
            }
            
            if (jsonElement.TryGetProperty("eval_duration", out var evalDurationElement))
            {
                schema.Completion.EvalDuration = evalDurationElement.GetInt64();
            }
            
            if (jsonElement.TryGetProperty("total_duration", out var totalDurationElement))
            {
                schema.Completion.TotalDuration = totalDurationElement.GetInt64();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to populate Ollama generation metadata");
        }
    }

    private void PopulateLMStudioGenerationMetadata(GenerationResponseSchema schema, string rawResponse)
    {
        try
        {
            var jsonElement = JsonSerializer.Deserialize<JsonElement>(rawResponse);
            
            if (jsonElement.TryGetProperty("usage", out var usageElement))
            {
                if (usageElement.TryGetProperty("total_tokens", out var totalTokensElement))
                {
                    schema.Usage.TotalTokens = totalTokensElement.GetInt32();
                }
                
                if (usageElement.TryGetProperty("prompt_tokens", out var promptTokensElement))
                {
                    schema.Usage.PromptTokens = promptTokensElement.GetInt32();
                }
                
                if (usageElement.TryGetProperty("completion_tokens", out var completionTokensElement))
                {
                    schema.Usage.CompletionTokens = completionTokensElement.GetInt32();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to populate LM Studio generation metadata");
        }
    }

    private ChatResponseSchema CreateErrorChatResponse(Exception exception, string provider, string model)
    {
        return new ChatResponseSchema
        {
            Content = string.Empty,
            Status = new ResponseStatus
            {
                Success = false,
                StatusCode = "ERROR",
                Message = "Failed to process chat response",
                Errors = new List<string> { exception.Message }
            },
            Metadata = new LLMResponseMetadata
            {
                Provider = provider,
                Model = model
            }
        };
    }

    private GenerationResponseSchema CreateErrorGenerationResponse(Exception exception, string provider, string model, string prompt)
    {
        return new GenerationResponseSchema
        {
            GeneratedText = string.Empty,
            Prompt = prompt,
            Status = new ResponseStatus
            {
                Success = false,
                StatusCode = "ERROR",
                Message = "Failed to process generation response",
                Errors = new List<string> { exception.Message }
            },
            Metadata = new LLMResponseMetadata
            {
                Provider = provider,
                Model = model
            }
        };
    }

    private string FormatDisplayName(string modelName)
    {
        // Convert model names to more readable format
        return modelName.Replace(":", " ").Replace("-", " ").Replace("_", " ");
    }

    private bool IsRetryableException(Exception exception)
    {
        return exception is HttpRequestException || 
               exception is TaskCanceledException ||
               exception is SocketException;
    }

    private List<string> GetRecoverySuggestions(Exception exception, string provider)
    {
        var suggestions = new List<string>();

        if (exception is HttpRequestException)
        {
            suggestions.Add($"Check if {provider} service is running");
            suggestions.Add("Verify network connectivity");
            suggestions.Add("Check endpoint configuration");
        }
        else if (exception is TaskCanceledException)
        {
            suggestions.Add("Increase request timeout");
            suggestions.Add("Try with a smaller prompt");
        }
        else if (exception is JsonException)
        {
            suggestions.Add("Enable response parsing fallbacks");
            suggestions.Add("Check model output format");
        }

        return suggestions;
    }
}
