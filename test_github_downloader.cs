using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ollama.Bootstrap.Composition;
using Ollama.Bootstrap.Configuration;
using Ollama.Domain.Tools;

Console.WriteLine("🧪 Testing GitHubDownloader with enhanced logging...");

var builder = Host.CreateApplicationBuilder();

// Get the solution root directory
var currentDir = Directory.GetCurrentDirectory();
var solutionRoot = currentDir;

while (!Directory.GetFiles(solutionRoot, "*.sln").Any() && Directory.GetParent(solutionRoot) != null)
{
    solutionRoot = Directory.GetParent(solutionRoot)!.FullName;
}

var configPath = Path.Combine(solutionRoot, "config", "appsettings.json");
builder.Configuration.AddJsonFile(configPath, optional: false, reloadOnChange: true);

// Add logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Add services
builder.Services.AddOllamaConfiguration(builder.Configuration);
builder.Services.AddOllamaServices();

using var app = builder.Build();

var toolRepo = app.Services.GetRequiredService<IToolRepository>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Get the GitHubDownloader
var githubDownloader = toolRepo.GetTool("GitHubDownloader");

if (githubDownloader == null)
{
    Console.WriteLine("❌ GitHubDownloader not found!");
    return;
}

Console.WriteLine("✅ GitHubDownloader found, testing with enhanced logging...");

// Test parameters
var parameters = new Dictionary<string, string>
{
    ["repoUrl"] = "https://github.com/octocat/Hello-World",
    ["sessionId"] = Guid.NewGuid().ToString()
};

Console.WriteLine($"📞 Calling GitHubDownloader with parameters:");
foreach (var param in parameters)
{
    Console.WriteLine($"  {param.Key} = {param.Value}");
}

try
{
    var result = await githubDownloader.RunAsync(parameters);
    Console.WriteLine($"🎉 GitHubDownloader result: {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ GitHubDownloader failed: {ex.Message}");
}

Console.WriteLine("🧪 Test completed!");
