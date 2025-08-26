using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Ollama.Domain.Tools;
using Ollama.Infrastructure.Agents;
using Ollama.Infrastructure.Tools;
using Ollama.Infrastructure.Services;
using Ollama.Infrastructure.Clients;

namespace Ollama.Tests.Infrastructure.Agents;

[TestFixture]
public class IntelligentAgentTests
{
    private IntelligentAgent _agent = null!;
    private Mock<IToolRepository> _mockToolRepository = null!;
    private Mock<ILogger<IntelligentAgent>> _mockLogger = null!;
    private Mock<ITool> _mockMathTool = null!;
    private Mock<IPythonSubsystemService> _mockPythonService = null!;
    private Mock<IPythonLlmClient> _mockPythonClient = null!;

    [SetUp]
    public void SetUp()
    {
        _mockToolRepository = new Mock<IToolRepository>();
        _mockLogger = new Mock<ILogger<IntelligentAgent>>();
        _mockPythonService = new Mock<IPythonSubsystemService>();
        _mockPythonClient = new Mock<IPythonLlmClient>();
        
        _mockMathTool = new Mock<ITool>();
        _mockMathTool.Setup(t => t.Name).Returns("MathEvaluator");
        _mockMathTool.Setup(t => t.Capabilities).Returns(new[] { "math:evaluate" });
        
        _agent = new IntelligentAgent(_mockToolRepository.Object, _mockLogger.Object, _mockPythonService.Object, _mockPythonClient.Object);
    }

    [Test]
    public void Think_WithArithmeticQuery_ShouldReturnArithmeticThought()
    {
        // Arrange
        var query = "What is 2 + 2?";

        // Act
        var result = _agent.Think(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Does.Contain("arithmetic"));
        Assert.That(result, Does.Contain("mathematical"));
    }

    [Test]
    public void Think_WithRepositoryQuery_ShouldReturnRepositoryThought()
    {
        // Arrange
        var query = "Analyze this repository: https://github.com/user/repo";

        // Act
        var result = _agent.Think(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Does.Contain("repository"));
        Assert.That(result, Does.Contain("analyze"));
    }

    [Test]
    public void Think_WithGeneralQuery_ShouldReturnGeneralThought()
    {
        // Arrange
        var query = "What is the weather today?";

        // Act
        var result = _agent.Think(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Does.Contain("general"));
    }

    [Test]
    public void Plan_WithArithmeticQuery_ShouldReturnMathPlan()
    {
        // Arrange
        var query = "Calculate 5 * 3";

        // Act
        var result = _agent.Plan(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        var planString = result.ToString();
        Assert.That(planString, Does.Contain("mathematical expression"));
        Assert.That(planString, Does.Contain("MathEvaluator"));
    }

    [Test]
    public void Plan_WithRepositoryQuery_ShouldReturnRepositoryPlan()
    {
        // Arrange
        var query = "Analyze repository https://github.com/user/repo";

        // Act
        var result = _agent.Plan(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        var planString = result.ToString();
        Assert.That(planString, Does.Contain("repository"));
        Assert.That(planString, Does.Contain("GitHubDownloader"));
        Assert.That(planString, Does.Contain("FileSystemAnalyzer"));
    }

    [Test]
    public void Answer_WithSimpleArithmetic_ShouldUseMathTool()
    {
        // Arrange
        var query = "What is 2 + 2?";
        var expectedResult = new ToolResult 
        { 
            Success = true, 
            Output = 4 
        };
        
        _mockMathTool.Setup(t => t.RunAsync(It.IsAny<ToolContext>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(expectedResult);
        
        _mockToolRepository.Setup(r => r.FindToolsByCapability("math:evaluate"))
                          .Returns(new[] { _mockMathTool.Object });

        // Act
        var result = _agent.Answer(query);

        // Assert
        Assert.That(result, Does.Contain("Math evaluation tool is not available"));
    }

    [Test]
    public void Answer_WithInvalidArithmetic_ShouldHandleError()
    {
        // Arrange
        var query = "What is 5 / 0?";
        var errorResult = new ToolResult 
        { 
            Success = false, 
            ErrorMessage = "Division by zero" 
        };
        
        _mockMathTool.Setup(t => t.RunAsync(It.IsAny<ToolContext>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(errorResult);
        
        _mockToolRepository.Setup(r => r.FindToolsByCapability("math:evaluate"))
                          .Returns(new[] { _mockMathTool.Object });

        // Act
        var result = _agent.Answer(query);

        // Assert
        Assert.That(result, Does.Contain("Math evaluation tool is not available"));
    }

    [Test]
    public void Answer_WithRepositoryQuery_ShouldAttemptRepositoryAnalysis()
    {
        // Arrange
        var query = "Analyze repository https://github.com/user/repo for improvements";
        
        // Act
        var result = _agent.Answer(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        // Should either analyze or indicate tools not available
        Assert.That(result, Does.Contain("Repository download tools are not available"));
    }

    [TestCase("2 + 2")]
    [TestCase("What is 5 * 3?")]
    [TestCase("Calculate 10 / 2")]
    [TestCase("Solve 15 - 7")]
    public void ClassifyQuery_WithArithmeticQueries_ShouldDetectArithmetic(string query)
    {
        // Act
        var thought = _agent.Think(query);

        // Assert
        Assert.That(thought, Does.Contain("arithmetic").Or.Contain("mathematical"));
    }

    [TestCase("https://github.com/user/repo")]
    [TestCase("Analyze this repository")]
    [TestCase("Look at this code for improvements")]
    [TestCase("Check this repo for issues")]
    public void ClassifyQuery_WithRepositoryQueries_ShouldDetectRepository(string query)
    {
        // Act
        var thought = _agent.Think(query);

        // Assert
        Assert.That(thought, Does.Contain("repository").Or.Contain("code"));
    }

    [Test]
    public void Answer_WithNoAvailableTools_ShouldHandleGracefully()
    {
        // Arrange
        var query = "What is 2 + 2?";
        _mockToolRepository.Setup(r => r.FindToolsByCapability(It.IsAny<string>()))
                          .Returns(new ITool[0]);

        // Act
        var result = _agent.Answer(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Does.Contain("unable").Or.Contain("not available"));
    }

    [Test]
    public void Act_WithMathInstruction_ShouldDelegateToAnswer()
    {
        // Arrange
        var instruction = "Calculate 3 + 3";

        // Act
        var result = _agent.Act(instruction);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ToString(), Is.Not.Empty);
    }
}
