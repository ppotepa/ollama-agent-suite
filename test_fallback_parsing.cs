using System;
using System.Text.Json;
using Ollama.Infrastructure.Strategies;
using Microsoft.Extensions.Logging;

// Test script to verify multi-format fallback parsing
public class TestFallbackParsing
{
    public static void Main()
    {
        Console.WriteLine("Testing Multi-Format Fallback Parsing System");
        Console.WriteLine("=============================================");
        
        // Test cases with different problematic formats
        var testCases = new[]
        {
            // 1. Malformed JSON with unescaped braces (original issue)
            ("Malformed JSON", """
            {
              "taskCompleted": true,
              "response": "Here's a simple web API controller:

            using Microsoft.AspNetCore.Mvc;

            namespace MyApi.Controllers
            {
                [ApiController]
                [Route("[controller]")]
                public class UserController : ControllerBase
                {
                    // GET: /users
                    [HttpGet]
                    public IActionResult GetUsers()
                    {
                        return Ok("Users retrieved");
                    }
                }
            }",
              "nextStep": null
            }
            """),
            
            // 2. YAML-like format
            ("YAML Format", """
            taskCompleted: true
            response: |
              Here's your C# controller:
              ```csharp
              public class UserController : ControllerBase
              {
                  [HttpGet]
                  public IActionResult Get() => Ok();
              }
              ```
            nextStep: null
            """),
            
            // 3. Key-value format
            ("Key-Value Format", """
            taskCompleted=true
            response=Generated a simple controller class
            nextStep=null
            """),
            
            // 4. Markdown-like format
            ("Markdown Format", """
            ## Task Status
            Complete
            
            ## Response
            Here's your web API controller:
            
            ```csharp
            [ApiController]
            public class UserController : ControllerBase
            {
                [HttpGet]
                public IActionResult GetUsers() => Ok();
            }
            ```
            """),
            
            // 5. Plain text
            ("Plain Text", """
            The task is complete. Here's a simple controller:
            
            using Microsoft.AspNetCore.Mvc;
            
            [ApiController]
            public class UserController : ControllerBase
            {
                [HttpGet]
                public IActionResult Get() => Ok("Hello");
            }
            """)
        };

        // Create a mock logger
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<PessimisticAgentStrategy>();
        var strategy = new PessimisticAgentStrategy(logger);
        
        foreach (var (name, testResponse) in testCases)
        {
            Console.WriteLine($"\n--- Testing {name} ---");
            Console.WriteLine($"Input: {testResponse.Substring(0, Math.Min(100, testResponse.Length))}...");
            
            try
            {
                var result = strategy.ValidateResponse(testResponse, "test-session");
                
                if (result.Contains("\"taskCompleted\""))
                {
                    var parsed = JsonSerializer.Deserialize<JsonElement>(result);
                    var taskCompleted = parsed.GetProperty("taskCompleted").GetBoolean();
                    var response = parsed.GetProperty("response").GetString();
                    
                    Console.WriteLine($"✅ Success! Task completed: {taskCompleted}");
                    Console.WriteLine($"   Response: {response?.Substring(0, Math.Min(50, response.Length ?? 0))}...");
                }
                else
                {
                    Console.WriteLine($"❌ Failed: {result.Substring(0, Math.Min(100, result.Length))}...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception: {ex.Message}");
            }
        }
        
        Console.WriteLine("\n=== Multi-Format Fallback System Test Complete ===");
    }
}
