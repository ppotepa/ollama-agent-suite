using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ollama.Domain.Models.Communication;
using Ollama.Domain.Tools;
using Ollama.Domain.Services;

namespace Ollama.Infrastructure.Services;

public class RealLLMCommunicationService : ILLMCommunicationService
{
    private readonly ILogger<RealLLMCommunicationService> _logger;
    private readonly ILogger<LLMCommunicationService> _llmLogger;
    private readonly HttpClient _httpClient;
    private readonly string _llmEndpointUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public RealLLMCommunicationService(
        ILogger<RealLLMCommunicationService> logger,
        ILogger<LLMCommunicationService> llmLogger,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        _logger = logger;
        _llmLogger = llmLogger;
        _httpClient = httpClientFactory.CreateClient();
        _llmEndpointUrl = config["LLM:EndpointUrl"] ?? "http://localhost:11434/api/chat";
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public LLMRequestSchema CreateRequestSchema(string sessionId, string userQuery, IToolRepository toolRepository, string strategy, List<InteractionHistory>? previousInteractions = null)
    {
        // Use the same logic as the mock for schema creation
    return new LLMCommunicationService(_llmLogger).CreateRequestSchema(sessionId, userQuery, toolRepository, strategy, previousInteractions);
    }

    public async Task<LLMResponseSchema> SendRequestAsync(LLMRequestSchema request, CancellationToken cancellationToken = default)
    {
        var json = SerializeRequest(request);
        _logger.LogInformation("Sending LLM request to {Endpoint}", _llmEndpointUrl);
        _logger.LogDebug("Request JSON: {Json}", json);
        try
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_llmEndpointUrl, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Received LLM response: {Response}", responseBody);
            response.EnsureSuccessStatusCode();
            return ParseResponseSchema(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send LLM request");
            return new LLMCommunicationService(_llmLogger).CreateErrorResponse($"HTTP error: {ex.Message}");
        }
    }

    public LLMResponseSchema ParseResponseSchema(string jsonResponse)
    {
    return new LLMCommunicationService(_llmLogger).ParseResponseSchema(jsonResponse);
    }

    public (bool IsValid, List<string> ValidationErrors) ValidateResponseSchema(LLMResponseSchema response)
    {
    return new LLMCommunicationService(_llmLogger).ValidateResponseSchema(response);
    }

    public string SerializeRequest(LLMRequestSchema request)
    {
    return new LLMCommunicationService(_llmLogger).SerializeRequest(request);
    }

    public (string toolName, Dictionary<string, object> parameters, string expectedOutcome) ExtractExecutableAction(LLMResponseSchema response)
    {
    return new LLMCommunicationService(_llmLogger).ExtractExecutableAction(response);
    }
}
