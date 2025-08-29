using Microsoft.Extensions.DependencyInjection;
using Ollama.Domain.Contracts;
using Ollama.Domain.Schemas;
using Ollama.Domain.Services;
using Ollama.Domain.Factories;

namespace Ollama.Domain.Extensions;

/// <summary>
/// Enhanced LLM client interface that returns structured schemas instead of raw strings
/// </summary>
public interface IEnhancedLLMClient : ILLMClient
{
    /// <summary>
    /// Send a chat request and receive structured response
    /// </summary>
    Task<ChatResponseSchema> ChatSchemaAsync(string model, List<(string role, string content)> messages);

    /// <summary>
    /// Send a generation request and receive structured response
    /// </summary>
    Task<GenerationResponseSchema> GenerateSchemaAsync(string model, string prompt);

    /// <summary>
    /// Get available models as structured schemas
    /// </summary>
    Task<List<ModelInfoSchema>> GetModelSchemasAsync();

    /// <summary>
    /// Get health status as structured schema
    /// </summary>
    Task<HealthResponseSchema> GetHealthSchemaAsync();
}

/// <summary>
/// Extension wrapper that adds schema support to any ILLMClient
/// </summary>
public class EnhancedLLMClientWrapper : IEnhancedLLMClient
{
    private readonly ILLMClient _innerClient;
    private readonly ILLMResponseMapper _responseMapper;
    private readonly IResponseSchemaFactory _schemaFactory;

    public EnhancedLLMClientWrapper(
        ILLMClient innerClient,
        ILLMResponseMapper responseMapper,
        IResponseSchemaFactory schemaFactory)
    {
        _innerClient = innerClient;
        _responseMapper = responseMapper;
        _schemaFactory = schemaFactory;
    }

    public string ClientType => _innerClient.ClientType;

    // Existing ILLMClient methods (maintain backward compatibility)
    public async Task<string> ChatAsync(string model, List<(string role, string content)> messages)
    {
        return await _innerClient.ChatAsync(model, messages);
    }

    public async Task<string> GenerateAsync(string model, string prompt)
    {
        return await _innerClient.GenerateAsync(model, prompt);
    }

    public async Task<List<string>> GetAvailableModelsAsync()
    {
        return await _innerClient.GetAvailableModelsAsync();
    }

    public async Task<bool> IsHealthyAsync()
    {
        return await _innerClient.IsHealthyAsync();
    }

    // Enhanced schema-based methods
    public async Task<ChatResponseSchema> ChatSchemaAsync(string model, List<(string role, string content)> messages)
    {
        try
        {
            var rawResponse = await _innerClient.ChatAsync(model, messages);
            return _responseMapper.MapChatResponse(rawResponse, _innerClient.ClientType, model);
        }
        catch (Exception ex)
        {
            return new ChatResponseSchema
            {
                Status = new ResponseStatus 
                { 
                    Success = false, 
                    StatusCode = "ERROR",
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                },
                Content = "",
                Role = "assistant",
                Metadata = new LLMResponseMetadata 
                { 
                    Provider = _innerClient.ClientType, 
                    Model = model 
                },
                Context = new ChatContext(),
                Usage = new TokenUsage()
            };
        }
    }

    public async Task<GenerationResponseSchema> GenerateSchemaAsync(string model, string prompt)
    {
        try
        {
            var rawResponse = await _innerClient.GenerateAsync(model, prompt);
            return _responseMapper.MapGenerationResponse(rawResponse, _innerClient.ClientType, model, prompt);
        }
        catch (Exception ex)
        {
            return new GenerationResponseSchema
            {
                Status = new ResponseStatus 
                { 
                    Success = false, 
                    StatusCode = "ERROR",
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                },
                GeneratedText = "",
                Prompt = prompt,
                Metadata = new LLMResponseMetadata 
                { 
                    Provider = _innerClient.ClientType, 
                    Model = model 
                },
                Completion = new CompletionDetails
                {
                    Done = false,
                    StopReason = "error"
                },
                Usage = new GenerationUsage()
            };
        }
    }

    public async Task<List<ModelInfoSchema>> GetModelSchemasAsync()
    {
        try
        {
            var rawModels = await _innerClient.GetAvailableModelsAsync();
            return _responseMapper.MapModelList(rawModels, _innerClient.ClientType);
        }
        catch (Exception)
        {
            return new List<ModelInfoSchema>();
        }
    }

    public async Task<HealthResponseSchema> GetHealthSchemaAsync()
    {
        try
        {
            var isHealthy = await _innerClient.IsHealthyAsync();
            return _responseMapper.MapHealthResponse(isHealthy, _innerClient.ClientType);
        }
        catch (Exception ex)
        {
            var healthResponse = _schemaFactory.CreateHealthResponse(false, _innerClient.ClientType);
            healthResponse.Status.Message = $"Health check failed: {ex.Message}";
            healthResponse.Status.Errors.Add(ex.Message);
            return healthResponse;
        }
    }
}

/// <summary>
/// Static extension methods for ILLMClient
/// </summary>
public static class LLMClientSchemaExtensions
{
    /// <summary>
    /// Enhance an existing ILLMClient with schema support
    /// </summary>
    public static IEnhancedLLMClient WithSchemaSupport(
        this ILLMClient client,
        ILLMResponseMapper responseMapper,
        IResponseSchemaFactory schemaFactory)
    {
        return new EnhancedLLMClientWrapper(client, responseMapper, schemaFactory);
    }

    /// <summary>
    /// Get a chat response as schema from any ILLMClient
    /// </summary>
    public static async Task<ChatResponseSchema> GetChatSchemaAsync(
        this ILLMClient client,
        string model,
        List<(string role, string content)> messages,
        ILLMResponseMapper responseMapper)
    {
        var rawResponse = await client.ChatAsync(model, messages);
        return responseMapper.MapChatResponse(rawResponse, client.ClientType, model);
    }

    /// <summary>
    /// Get a generation response as schema from any ILLMClient
    /// </summary>
    public static async Task<GenerationResponseSchema> GetGenerationSchemaAsync(
        this ILLMClient client,
        string model,
        string prompt,
        ILLMResponseMapper responseMapper)
    {
        var rawResponse = await client.GenerateAsync(model, prompt);
        return responseMapper.MapGenerationResponse(rawResponse, client.ClientType, model, prompt);
    }
}

/// <summary>
/// Service collection extensions for registering schema services
/// </summary>
public static class SchemaServiceExtensions
{
    /// <summary>
    /// Add schema services to DI container
    /// </summary>
    public static IServiceCollection AddLLMSchemaServices(this IServiceCollection services)
    {
        services.AddSingleton<ILLMResponseMapper, LLMResponseMapper>();
        services.AddSingleton<IResponseSchemaFactory, ResponseSchemaFactory>();
        
        return services;
    }

    /// <summary>
    /// Add enhanced LLM client wrapper to DI container
    /// </summary>
    public static IServiceCollection AddEnhancedLLMClient<TClient>(this IServiceCollection services)
        where TClient : class, ILLMClient
    {
        services.AddScoped<IEnhancedLLMClient>(provider =>
        {
            var innerClient = provider.GetRequiredService<TClient>();
            var responseMapper = provider.GetRequiredService<ILLMResponseMapper>();
            var schemaFactory = provider.GetRequiredService<IResponseSchemaFactory>();
            
            return new EnhancedLLMClientWrapper(innerClient, responseMapper, schemaFactory);
        });
        
        return services;
    }
}
