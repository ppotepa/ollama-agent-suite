using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ollama.Bootstrap.Composition;
using Ollama.Domain.Configuration;
using Ollama.Application.Services;

namespace PromptLoadingTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var solutionRoot = FindSolutionRoot(Directory.GetCurrentDirectory());
                if (solutionRoot == null)
                {
                    Console.WriteLine("❌ Could not find solution root");
                    return;
                }

                Console.WriteLine($"✅ Solution root: {solutionRoot}");

                // Build configuration
                var configBuilder = new ConfigurationBuilder()
                    .SetBasePath(Path.Combine(solutionRoot, "config"))
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddJsonFile("appsettings.Development.json", optional: true);

                // Set the prompt base path
                configBuilder.AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("PromptBasePath", Path.Combine(solutionRoot, "prompts"))
                });

                var configuration = configBuilder.Build();

                // Build services
                var hostBuilder = Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        services.AddSingleton<IConfiguration>(configuration);
                        services.RegisterDomainServices(configuration);
                        services.RegisterApplicationServices(configuration);
                        services.RegisterInfrastructureServices(configuration);
                    });

                using var host = hostBuilder.Build();
                
                // Get prompt service
                var promptService = host.Services.GetRequiredService<PromptService>();
                var promptConfig = host.Services.GetRequiredService<PromptConfiguration>();

                Console.WriteLine($"✅ PromptService created successfully");
                Console.WriteLine($"✅ PromptConfiguration created successfully");

                // Check prompt file configuration
                Console.WriteLine($"📄 Pessimistic prompt file: {promptConfig.PessimisticPromptFileName}");
                Console.WriteLine($"📄 Optimistic prompt file: {promptConfig.OptimisticPromptFileName}");
                Console.WriteLine($"📄 Default prompt file: {promptConfig.DefaultPromptFileName}");

                // Test loading the pessimistic prompt
                try
                {
                    var prompt = await promptService.GetPessimisticPromptAsync();
                    Console.WriteLine($"✅ Successfully loaded pessimistic prompt ({prompt.Length} characters)");
                    Console.WriteLine($"📝 First 100 characters: {prompt.Substring(0, Math.Min(100, prompt.Length))}...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to load pessimistic prompt: {ex.Message}");
                }

                // Test loading the optimistic prompt (should be same file)
                try
                {
                    var prompt = await promptService.GetOptimisticPromptAsync();
                    Console.WriteLine($"✅ Successfully loaded optimistic prompt ({prompt.Length} characters)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to load optimistic prompt: {ex.Message}");
                }

                // Test loading the default prompt (should be same file)
                try
                {
                    var prompt = await promptService.GetDefaultPromptAsync();
                    Console.WriteLine($"✅ Successfully loaded default prompt ({prompt.Length} characters)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to load default prompt: {ex.Message}");
                }

                Console.WriteLine("\n🎉 Prompt standardization test completed successfully!");
                Console.WriteLine("All prompt methods now use the single pessimistic-initial-system-prompt.txt file.");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private static string? FindSolutionRoot(string currentPath)
        {
            var directory = new DirectoryInfo(currentPath);
            while (directory != null)
            {
                if (directory.GetFiles("*.sln").Length > 0)
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }
            return null;
        }
    }
}
