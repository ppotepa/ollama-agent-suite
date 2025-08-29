using System.Text.Json.Serialization;

namespace Ollama.Domain.Schemas;

/// <summary>
/// Health check response schema for LLM providers
/// Provides consistent health monitoring across all backends
/// </summary>
public class HealthResponseSchema : BaseLLMResponseSchema
{
    [JsonPropertyName("healthy")]
    public bool Healthy { get; set; } = false;

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("services")]
    public List<ServiceHealth> Services { get; set; } = new();

    [JsonPropertyName("performance")]
    public PerformanceMetrics Performance { get; set; } = new();

    [JsonPropertyName("resources")]
    public ResourceUsage Resources { get; set; } = new();
}

/// <summary>
/// Health status of individual services
/// </summary>
public class ServiceHealth
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "unknown";

    [JsonPropertyName("lastCheck")]
    public DateTime LastCheck { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("responseTime")]
    public int ResponseTimeMs { get; set; } = 0;

    [JsonPropertyName("details")]
    public Dictionary<string, object> Details { get; set; } = new();

    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = new();
}

/// <summary>
/// Performance metrics
/// </summary>
public class PerformanceMetrics
{
    [JsonPropertyName("averageResponseTime")]
    public double AverageResponseTimeMs { get; set; } = 0.0;

    [JsonPropertyName("requestsPerSecond")]
    public double RequestsPerSecond { get; set; } = 0.0;

    [JsonPropertyName("errorRate")]
    public double ErrorRate { get; set; } = 0.0;

    [JsonPropertyName("uptime")]
    public TimeSpan Uptime { get; set; } = TimeSpan.Zero;

    [JsonPropertyName("totalRequests")]
    public long TotalRequests { get; set; } = 0;

    [JsonPropertyName("successfulRequests")]
    public long SuccessfulRequests { get; set; } = 0;

    [JsonPropertyName("failedRequests")]
    public long FailedRequests { get; set; } = 0;
}

/// <summary>
/// Resource usage information
/// </summary>
public class ResourceUsage
{
    [JsonPropertyName("memoryUsage")]
    public MemoryInfo Memory { get; set; } = new();

    [JsonPropertyName("cpuUsage")]
    public double CpuUsagePercent { get; set; } = 0.0;

    [JsonPropertyName("gpuUsage")]
    public GpuInfo Gpu { get; set; } = new();

    [JsonPropertyName("diskUsage")]
    public DiskInfo Disk { get; set; } = new();

    [JsonPropertyName("networkUsage")]
    public NetworkInfo Network { get; set; } = new();
}

/// <summary>
/// Memory usage details
/// </summary>
public class MemoryInfo
{
    [JsonPropertyName("used")]
    public long UsedBytes { get; set; } = 0;

    [JsonPropertyName("available")]
    public long AvailableBytes { get; set; } = 0;

    [JsonPropertyName("total")]
    public long TotalBytes { get; set; } = 0;

    [JsonPropertyName("percentage")]
    public double UsagePercentage { get; set; } = 0.0;
}

/// <summary>
/// GPU usage information
/// </summary>
public class GpuInfo
{
    [JsonPropertyName("available")]
    public bool Available { get; set; } = false;

    [JsonPropertyName("utilizationPercent")]
    public double UtilizationPercent { get; set; } = 0.0;

    [JsonPropertyName("memoryUsed")]
    public long MemoryUsedBytes { get; set; } = 0;

    [JsonPropertyName("memoryTotal")]
    public long MemoryTotalBytes { get; set; } = 0;

    [JsonPropertyName("temperature")]
    public double TemperatureCelsius { get; set; } = 0.0;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Disk usage information
/// </summary>
public class DiskInfo
{
    [JsonPropertyName("used")]
    public long UsedBytes { get; set; } = 0;

    [JsonPropertyName("available")]
    public long AvailableBytes { get; set; } = 0;

    [JsonPropertyName("total")]
    public long TotalBytes { get; set; } = 0;

    [JsonPropertyName("percentage")]
    public double UsagePercentage { get; set; } = 0.0;
}

/// <summary>
/// Network usage information
/// </summary>
public class NetworkInfo
{
    [JsonPropertyName("bytesReceived")]
    public long BytesReceived { get; set; } = 0;

    [JsonPropertyName("bytesSent")]
    public long BytesSent { get; set; } = 0;

    [JsonPropertyName("packetsReceived")]
    public long PacketsReceived { get; set; } = 0;

    [JsonPropertyName("packetsSent")]
    public long PacketsSent { get; set; } = 0;

    [JsonPropertyName("latency")]
    public double LatencyMs { get; set; } = 0.0;
}
