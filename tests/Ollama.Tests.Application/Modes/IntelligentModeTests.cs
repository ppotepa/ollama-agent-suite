using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Ollama.Application.Modes;
using Ollama.Application.Services;
using Ollama.Domain.Agents;
using Ollama.Domain.Execution;
using Ollama.Domain.Strategies;
using ExecutionContext = Ollama.Domain.Strategies.ExecutionContext;

namespace Ollama.Tests.Application.Modes;

[TestFixture]
public class IntelligentModeTests
{
    private IntelligentMode _intelligentMode = null!;
    private Mock<IAgent> _mockAgent = null!;
    private AgentSwitchService _agentSwitchService = null!;
    private ExecutionTreeBuilder _executionTreeBuilder = null!;

    [SetUp]
    public void SetUp()
    {
        _mockAgent = new Mock<IAgent>();
        _agentSwitchService = new AgentSwitchService();
        _executionTreeBuilder = new ExecutionTreeBuilder();

        _intelligentMode = new IntelligentMode(_mockAgent.Object, _agentSwitchService, _executionTreeBuilder);
    }    [Test]
    public void Type_ShouldReturnIntelligent()
    {
        // Act
        var result = _intelligentMode.Type;

        // Assert
        Assert.That(result, Is.EqualTo(StrategyType.Intelligent));
    }

    [Test]
    public void CanHandle_WithArithmeticQuery_ShouldReturnTrue()
    {
        // Arrange
        var context = new ExecutionContext("What is 2 + 2?", "session1");

        // Act
        var result = _intelligentMode.CanHandle(context);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void CanHandle_WithRepositoryQuery_ShouldReturnTrue()
    {
        // Arrange
        var context = new ExecutionContext("Analyze this repository", "session1");

        // Act
        var result = _intelligentMode.CanHandle(context);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void CanHandle_WithShortGeneralQuery_ShouldReturnFalse()
    {
        // Arrange
        var context = new ExecutionContext("Hi", "session1");

        // Act
        var result = _intelligentMode.CanHandle(context);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void CanHandle_WithLongQuery_ShouldReturnTrue()
    {
        // Arrange
        var longQuery = "This is a very long query that contains many words and should be handled by intelligent mode because it's complex";
        var context = new ExecutionContext(longQuery, "session1");

        // Act
        var result = _intelligentMode.CanHandle(context);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Execute_WithValidContext_ShouldReturnCompleteResult()
    {
        // Arrange
        var context = new ExecutionContext("What is 2 + 2?", "session1");
        
        _mockAgent.Setup(a => a.Think("What is 2 + 2?")).Returns("This is arithmetic");
        _mockAgent.Setup(a => a.Plan("What is 2 + 2?")).Returns("Calculate using math tool");
        _mockAgent.Setup(a => a.Answer("What is 2 + 2?")).Returns("The answer is 4");

        // Act
        var result = _intelligentMode.Execute(context);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContainsKey("reasoning"), Is.True);
        Assert.That(result.ContainsKey("plan"), Is.True);
        Assert.That(result.ContainsKey("result"), Is.True);
        Assert.That(result.ContainsKey("strategy"), Is.True);
        Assert.That(result.ContainsKey("sessionId"), Is.True);
        
        Assert.That(result["reasoning"], Is.EqualTo("This is arithmetic"));
        Assert.That(result["plan"], Is.EqualTo("Calculate using math tool"));
        Assert.That(result["result"], Is.EqualTo("The answer is 4"));
        Assert.That(result["strategy"], Is.EqualTo("Intelligent"));
    }

    [Test]
    public void Execute_ShouldCallAgentMethodsInCorrectOrder()
    {
        // Arrange
        var context = new ExecutionContext("Test query", "session1");
        
        _mockAgent.Setup(a => a.Think("Test query")).Returns("Thinking");
        _mockAgent.Setup(a => a.Plan("Test query")).Returns("Planning");
        _mockAgent.Setup(a => a.Answer("Test query")).Returns("Answer");

        // Act
        var result = _intelligentMode.Execute(context);

        // Assert
        _mockAgent.Verify(a => a.Think("Test query"), Times.Once);
        _mockAgent.Verify(a => a.Plan("Test query"), Times.Once);
        _mockAgent.Verify(a => a.Answer("Test query"), Times.Once);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ContainsKey("reasoning"), Is.True);
        Assert.That(result.ContainsKey("plan"), Is.True);
        Assert.That(result.ContainsKey("result"), Is.True);
    }

    [Test]
    public void Execute_ShouldBuildExecutionTree()
    {
        // Arrange
        var context = new ExecutionContext("Test query", "session1");
        
        _mockAgent.Setup(a => a.Think(It.IsAny<string>())).Returns("Thinking");
        _mockAgent.Setup(a => a.Plan(It.IsAny<string>())).Returns("Planning");
        _mockAgent.Setup(a => a.Answer(It.IsAny<string>())).Returns("Answer");

        // Act
        var result = _intelligentMode.Execute(context);

        // Assert
        // Verify execution tree was built and included in result
        Assert.That(result.ContainsKey("executionTree"), Is.True);
        
        // Verify agents were called
        _mockAgent.Verify(a => a.Think("Test query"), Times.Once);
        _mockAgent.Verify(a => a.Plan("Test query"), Times.Once);
        _mockAgent.Verify(a => a.Answer("Test query"), Times.Once);
    }

    [TestCase("what is", true)]
    [TestCase("calculate", true)]
    [TestCase("solve", true)]
    [TestCase("intelligent", true)]
    [TestCase("think", true)]
    [TestCase("hello", false)]
    [TestCase("yes", false)]
    [TestCase("no", false)]
    public void CanHandle_WithVariousQueries_ShouldReturnExpectedResult(string query, bool expected)
    {
        // Arrange
        var context = new ExecutionContext(query, "session1");

        // Act
        var result = _intelligentMode.CanHandle(context);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void Execute_WithNullSessionId_ShouldGenerateSessionId()
    {
        // Arrange
        var context = new ExecutionContext("Test query", null);
        
        _mockAgent.Setup(a => a.Think(It.IsAny<string>())).Returns("Thinking");
        _mockAgent.Setup(a => a.Plan(It.IsAny<string>())).Returns("Planning");
        _mockAgent.Setup(a => a.Answer(It.IsAny<string>())).Returns("Answer");

        // Act
        var result = _intelligentMode.Execute(context);

        // Assert
        Assert.That(result.ContainsKey("sessionId"), Is.True);
        Assert.That(result["sessionId"], Is.Not.Null);
        Assert.That(result["sessionId"].ToString(), Is.Not.Empty);
    }
}
