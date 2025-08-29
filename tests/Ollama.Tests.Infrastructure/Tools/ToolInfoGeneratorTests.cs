using NUnit.Framework;
using Moq;
using Ollama.Domain.Tools;
using Ollama.Domain.Tools.Attributes;
using Ollama.Infrastructure.Tools;
using System.Reflection;

namespace Ollama.Tests.Infrastructure.Tools
{
    [TestFixture]
    public class ToolInfoGeneratorTests
    {
        private Mock<IToolRepository> _mockRepository;

        [SetUp]
        public void Setup()
        {
            _mockRepository = new Mock<IToolRepository>();
        }

        [Test]
        public void GenerateToolInformation_WithNoTools_ReturnsWarningMessage()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllTools()).Returns(new List<ITool>());

            // Act
            var result = ToolInfoGenerator.GenerateToolInformation(_mockRepository.Object);

            // Assert
            Assert.That(result, Contains.Substring("⚠️  WARNING: No tools are currently available!"));
            Assert.That(result, Contains.Substring("MISSING_TOOL"));
        }

        [Test]
        public void GenerateToolInformation_WithBasicTool_ReturnsFormattedInformation()
        {
            // Arrange
            var mockTool = new Mock<ITool>();
            mockTool.Setup(t => t.Name).Returns("TestTool");
            mockTool.Setup(t => t.Description).Returns("A test tool");
            mockTool.Setup(t => t.Capabilities).Returns(new[] { "test:operation" });
            mockTool.Setup(t => t.RequiresNetwork).Returns(false);
            mockTool.Setup(t => t.RequiresFileSystem).Returns(true);

            _mockRepository.Setup(r => r.GetAllTools()).Returns(new[] { mockTool.Object });

            // Act
            var result = ToolInfoGenerator.GenerateToolInformation(_mockRepository.Object);

            // Assert
            Assert.That(result, Contains.Substring("• TestTool:"));
            Assert.That(result, Contains.Substring("Purpose: A test tool"));
            Assert.That(result, Contains.Substring("Network required: No"));
            Assert.That(result, Contains.Substring("File system required: Yes"));
            Assert.That(result, Contains.Substring("Parameters: []"));
        }

        [Test]
        public void ExtractToolInformation_WithAttributeDecoratedTool_ExtractsAllInformation()
        {
            // Arrange
            var decoratedTool = new TestToolWithAttributes();

            // Act
            var result = ToolInfoGenerator.ExtractToolInformation(decoratedTool);

            // Assert
            Assert.That(result.Name, Is.EqualTo("TestToolWithAttributes"));
            Assert.That(result.Description, Is.EqualTo("A test tool with full attributes"));
            Assert.That(result.DetailedDescription, Is.EqualTo("Detailed usage instructions for testing"));
            Assert.That(result.Category, Is.EqualTo("Testing"));
            Assert.That(result.Version, Is.EqualTo("1.0.0")); // Default version
            Assert.That(result.IsExperimental, Is.False); // Default value
            Assert.That(result.CreatedBy, Is.EqualTo("Unknown")); // Default value
            
            Assert.That(result.PrimaryUseCase, Is.EqualTo("Testing tool extraction"));
            Assert.That(result.SecondaryUseCases, Contains.Item("Unit testing"));
            Assert.That(result.SecondaryUseCases, Contains.Item("Integration testing"));
            Assert.That(result.ExampleInvocation, Is.EqualTo("TestToolWithAttributes with param1=\"value\""));
            Assert.That(result.ExpectedOutput, Is.EqualTo("Test results and status"));
            Assert.That(result.SafetyNotes, Is.EqualTo("Safe for testing environments only"));
            Assert.That(result.PerformanceNotes, Is.EqualTo("Fast execution in test scenarios"));
            
            Assert.That(result.RequiresNetwork, Is.False);
            Assert.That(result.RequiresFileSystem, Is.True);
            Assert.That(result.RequiresElevatedPrivileges, Is.False);
            
            Assert.That(result.Capabilities, Contains.Item("FileRead"));
            Assert.That(result.Capabilities, Contains.Item("MathCalculation"));
            Assert.That(result.FallbackStrategy, Is.EqualTo("Mock fallback for testing"));
            
            Assert.That(result.Parameters.Count, Is.EqualTo(2));
            Assert.That(result.Parameters.Any(p => p.Name == "param1" && p.IsRequired == true), Is.True);
            Assert.That(result.Parameters.Any(p => p.Name == "param2" && p.IsRequired == false), Is.True);
        }

        [Test]
        public void ExtractToolInformation_WithUndecorationedTool_UsesInference()
        {
            // Arrange
            var basicTool = new TestBasicTool();

            // Act
            var result = ToolInfoGenerator.ExtractToolInformation(basicTool);

            // Assert
            Assert.That(result.Name, Is.EqualTo("TestBasicTool"));
            Assert.That(result.Description, Is.EqualTo("Basic test tool"));
            Assert.That(result.Category, Is.EqualTo("Uncategorized"));
            Assert.That(result.PrimaryUseCase, Is.EqualTo("General purpose operations"));
            // TestBasicTool doesn't match any naming patterns, so no parameters should be inferred
            Assert.That(result.Parameters.Count, Is.EqualTo(0));
        }

        [Test]
        public void ExtractParameters_FromFileOperationTool_InfersCorrectParameters()
        {
            // Arrange
            var fileTool = new TestFileCopyTool();

            // Act
            var result = ToolInfoGenerator.ExtractToolInformation(fileTool);

            // Assert
            var parameters = result.Parameters;
            Assert.That(parameters.Any(p => p.Name == "path"), Is.True);
            Assert.That(parameters.Any(p => p.Name == "destination"), Is.True);
            Assert.That(parameters.Any(p => p.Name == "cd"), Is.True);
        }

        [Test]
        public void ExtractParameters_FromMathTool_InfersExpressionParameter()
        {
            // Arrange
            var mathTool = new TestMathTool();

            // Act
            var result = ToolInfoGenerator.ExtractToolInformation(mathTool);

            // Assert
            var parameters = result.Parameters;
            Assert.That(parameters.Any(p => p.Name == "expression"), Is.True);
        }

        [Test]
        public void ExtractCapabilities_WithAttributeCapabilities_ExtractsEnumFlags()
        {
            // Arrange
            var decoratedTool = new TestToolWithAttributes();

            // Act
            var result = ToolInfoGenerator.ExtractToolInformation(decoratedTool);

            // Assert
            Assert.That(result.Capabilities, Contains.Item("FileRead"));
            Assert.That(result.Capabilities, Contains.Item("MathCalculation"));
        }

        [Test]
        public void ExtractCapabilities_WithLegacyCapabilities_FallsBackToLegacy()
        {
            // Arrange
            var basicTool = new TestBasicTool();

            // Act
            var result = ToolInfoGenerator.ExtractToolInformation(basicTool);

            // Assert
            Assert.That(result.LegacyCapabilities, Contains.Item("test:basic"));
        }

        [Test]
        public void ExtractToolInformation_WithRealToolAttributes_ExtractsCorrectInformation()
        {
            // Arrange
            var realTool = new TestRealFileReadTool();

            // Act
            var result = ToolInfoGenerator.ExtractToolInformation(realTool);

            // Assert
            Assert.That(result.Name, Is.EqualTo("TestRealFileReadTool"));
            Assert.That(result.Category, Is.EqualTo("File Operations"));
            Assert.That(result.RequiresFileSystem, Is.True);
            Assert.That(result.RequiresNetwork, Is.False);
            Assert.That(result.Capabilities, Contains.Item("FileRead"));
            Assert.That(result.Parameters.Count, Is.GreaterThan(0));
        }

        [Test]
        public void ExtractToolInformation_WithAllToolTypes_HandlesVariousPatterns()
        {
            // Arrange
            var tools = new ITool[]
            {
                new TestMathTool(),
                new TestFileCopyTool(),
                new TestBasicTool()
            };

            // Act & Assert
            foreach (var tool in tools)
            {
                var result = ToolInfoGenerator.ExtractToolInformation(tool);
                Assert.That(result.Name, Is.Not.Empty);
                Assert.That(result.Category, Is.Not.Empty);
                Assert.That(result.Capabilities, Is.Not.Empty);
            }
        }

        [Test]
        public void GenerateToolInformation_WithAttributedTools_FormatsComprehensiveOutput()
        {
            // Arrange
            var tools = new ITool[]
            {
                new TestToolWithAttributes(),
                new TestMathTool(),
                new TestFileCopyTool()
            };
            var repository = new TestToolRepository(tools);

            // Act
            var result = ToolInfoGenerator.GenerateToolInformation(repository);

            // Assert
            Assert.That(result, Contains.Substring("INTERNAL TOOLS"));
            Assert.That(result, Contains.Substring("TestToolWithAttributes"));
            Assert.That(result, Contains.Substring("TestMathEvaluator"));
            Assert.That(result, Contains.Substring("TestFileCopyTool"));
            Assert.That(result, Contains.Substring("Purpose:"));
            Assert.That(result, Contains.Substring("Category:"));
            Assert.That(result, Contains.Substring("Capabilities:"));
        }

        [Test]
        public void ExtractToolInformation_WithComplexCapabilities_ExtractsAllFlags()
        {
            // Arrange
            var decoratedTool = new TestComplexCapabilitiesTool();

            // Act
            var result = ToolInfoGenerator.ExtractToolInformation(decoratedTool);

            // Assert
            Assert.That(result.Capabilities, Contains.Item("FileRead"));
            Assert.That(result.Capabilities, Contains.Item("FileWrite"));
            Assert.That(result.Capabilities, Contains.Item("NetworkDownload"));
            Assert.That(result.Capabilities, Contains.Item("CursorNavigation"));
        }
    }

    // Test tool implementations for testing

    [ToolDescription("A test tool with full attributes", "Detailed usage instructions for testing", "Testing")]
    [ToolUsage("Testing tool extraction",
        SecondaryUseCases = new[] { "Unit testing", "Integration testing" },
        RequiredParameters = new[] { "param1" },
        OptionalParameters = new[] { "param2" },
        ExampleInvocation = "TestToolWithAttributes with param1=\"value\"",
        ExpectedOutput = "Test results and status",
        RequiresFileSystem = true,
        RequiresNetwork = false,
        SafetyNotes = "Safe for testing environments only",
        PerformanceNotes = "Fast execution in test scenarios")]
    [ToolCapabilities(ToolCapability.FileRead | ToolCapability.MathCalculation,
        FallbackStrategy = "Mock fallback for testing")]
    public class TestToolWithAttributes : ITool
    {
        public string Name => "TestToolWithAttributes";
        public string Description => "A test tool with full attributes";
        public IEnumerable<string> Capabilities => new[] { "test:attributed" };
        public bool RequiresNetwork => false;
        public bool RequiresFileSystem => true;

        public Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ToolResult { Success = true, Output = "Test output" });
        }

        public Task<ToolResult> RunWithRetryAsync(ToolContext context, int maxRetries = 3, TimeSpan? retryDelay = null, CancellationToken cancellationToken = default)
        {
            return RunAsync(context, cancellationToken);
        }

        public Task<ToolResult> TryAlternativeAsync(ToolContext context, string failureReason, CancellationToken cancellationToken = default)
        {
            return RunAsync(context, cancellationToken);
        }

        public Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(true);
        }

        public Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m);
        }

        public IEnumerable<string> GetAlternativeMethods()
        {
            return new[] { "MockAlternative" };
        }
    }

    public class TestBasicTool : ITool
    {
        public string Name => "TestBasicTool";
        public string Description => "Basic test tool";
        public IEnumerable<string> Capabilities => new[] { "test:basic" };
        public bool RequiresNetwork => false;
        public bool RequiresFileSystem => false;

        public Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ToolResult { Success = true, Output = "Basic test output" });
        }

        public Task<ToolResult> RunWithRetryAsync(ToolContext context, int maxRetries = 3, TimeSpan? retryDelay = null, CancellationToken cancellationToken = default)
        {
            return RunAsync(context, cancellationToken);
        }

        public Task<ToolResult> TryAlternativeAsync(ToolContext context, string failureReason, CancellationToken cancellationToken = default)
        {
            return RunAsync(context, cancellationToken);
        }

        public Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(true);
        }

        public Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m);
        }

        public IEnumerable<string> GetAlternativeMethods()
        {
            return new[] { "BasicAlternative" };
        }
    }

    public class TestFileCopyTool : ITool
    {
        public string Name => "TestFileCopyTool";
        public string Description => "Test file copy tool";
        public IEnumerable<string> Capabilities => new[] { "file:copy" };
        public bool RequiresNetwork => false;
        public bool RequiresFileSystem => true;

        public Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ToolResult { Success = true, Output = "File copied" });
        }

        public Task<ToolResult> RunWithRetryAsync(ToolContext context, int maxRetries = 3, TimeSpan? retryDelay = null, CancellationToken cancellationToken = default)
        {
            return RunAsync(context, cancellationToken);
        }

        public Task<ToolResult> TryAlternativeAsync(ToolContext context, string failureReason, CancellationToken cancellationToken = default)
        {
            return RunAsync(context, cancellationToken);
        }

        public Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(true);
        }

        public Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m);
        }

        public IEnumerable<string> GetAlternativeMethods()
        {
            return new[] { "FileCopyAlternative" };
        }
    }

    public class TestMathTool : ITool
    {
        public string Name => "TestMathEvaluator";
        public string Description => "Test math evaluator";
        public IEnumerable<string> Capabilities => new[] { "math:evaluate" };
        public bool RequiresNetwork => false;
        public bool RequiresFileSystem => false;

        public Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ToolResult { Success = true, Output = "42" });
        }

        public Task<ToolResult> RunWithRetryAsync(ToolContext context, int maxRetries = 3, TimeSpan? retryDelay = null, CancellationToken cancellationToken = default)
        {
            return RunAsync(context, cancellationToken);
        }

        public Task<ToolResult> TryAlternativeAsync(ToolContext context, string failureReason, CancellationToken cancellationToken = default)
        {
            return RunAsync(context, cancellationToken);
        }

        public Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(true);
        }

        public Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m);
        }

        public IEnumerable<string> GetAlternativeMethods()
        {
            return new[] { "MathAlternative" };
        }
    }

    public class TestToolRepository : IToolRepository
    {
        private readonly ITool[] _tools;

        public TestToolRepository(params ITool[] tools)
        {
            _tools = tools;
        }

        public IEnumerable<ITool> GetAllTools() => _tools;

        public ITool? GetToolByName(string name) => _tools.FirstOrDefault(t => t.Name == name);

        public void RegisterTool(ITool tool)
        {
            // Not needed for tests
        }

        public IEnumerable<ITool> FindToolsByCapability(string capability)
        {
            return _tools.Where(t => t.Capabilities.Contains(capability));
        }
    }

    [ToolDescription("Test tool similar to real FileReadTool", "Testing reflection with real-like attributes", "File Operations")]
    [ToolUsage("Read files for testing", 
        RequiredParameters = new[] { "path" }, 
        OptionalParameters = new[] { "cd" },
        RequiresFileSystem = true,
        RequiresNetwork = false)]
    [ToolCapabilities(ToolCapability.FileRead | ToolCapability.CursorNavigation)]
    public class TestRealFileReadTool : ITool
    {
        public string Name => "TestRealFileReadTool";
        public string Description => "Test file read tool";
        public IEnumerable<string> Capabilities => new[] { "file:read", "test:read" };
        public bool RequiresNetwork => false;
        public bool RequiresFileSystem => true;

        public Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ToolResult { Success = true, Output = "Test file content" });
        }

        public Task<ToolResult> RunWithRetryAsync(ToolContext context, int maxRetries = 3, TimeSpan? retryDelay = null, CancellationToken cancellationToken = default)
        {
            return RunAsync(context, cancellationToken);
        }

        public Task<ToolResult> TryAlternativeAsync(ToolContext context, string failureReason, CancellationToken cancellationToken = default)
        {
            return RunAsync(context, cancellationToken);
        }

        public Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(true);
        }

        public Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m);
        }

        public IEnumerable<string> GetAlternativeMethods()
        {
            return new[] { "StreamRead", "BinaryRead" };
        }
    }

    [ToolDescription("Complex capabilities test tool", "Testing multiple capability flags", "Testing")]
    [ToolUsage("Test complex capability extraction")]
    [ToolCapabilities(ToolCapability.FileRead | ToolCapability.FileWrite | ToolCapability.NetworkDownload | ToolCapability.CursorNavigation)]
    public class TestComplexCapabilitiesTool : ITool
    {
        public string Name => "TestComplexCapabilitiesTool";
        public string Description => "Test tool with complex capabilities";
        public IEnumerable<string> Capabilities => new[] { "complex:test" };
        public bool RequiresNetwork => true;
        public bool RequiresFileSystem => true;

        public Task<ToolResult> RunAsync(ToolContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ToolResult { Success = true, Output = "Complex test output" });
        }

        public Task<ToolResult> RunWithRetryAsync(ToolContext context, int maxRetries = 3, TimeSpan? retryDelay = null, CancellationToken cancellationToken = default)
        {
            return RunAsync(context, cancellationToken);
        }

        public Task<ToolResult> TryAlternativeAsync(ToolContext context, string failureReason, CancellationToken cancellationToken = default)
        {
            return RunAsync(context, cancellationToken);
        }

        public Task<bool> DryRunAsync(ToolContext context)
        {
            return Task.FromResult(true);
        }

        public Task<decimal> EstimateCostAsync(ToolContext context)
        {
            return Task.FromResult(0.0m);
        }

        public IEnumerable<string> GetAlternativeMethods()
        {
            return new[] { "ComplexAlternative" };
        }
    }
}
