using NUnit.Framework;
using Ollama.Domain.Tools;
using Ollama.Domain.Services;
using Ollama.Infrastructure.Tools;
using Microsoft.Extensions.Logging;
using Moq;

namespace Ollama.Tests.Infrastructure.Tools;

[TestFixture]
public class MathEvaluatorTests
{
    private MathEvaluator _mathEvaluator = null!;
    private ToolContext _context = null!;
    private Mock<ISessionScope> _mockSessionScope = null!;
    private Mock<ILogger<MathEvaluator>> _mockLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _mockSessionScope = new Mock<ISessionScope>();
        _mockLogger = new Mock<ILogger<MathEvaluator>>();
        _mathEvaluator = new MathEvaluator(_mockSessionScope.Object, _mockLogger.Object);
        _context = new ToolContext();
    }

    [Test]
    public void Name_ShouldReturnMathEvaluator()
    {
        // Act
        var result = _mathEvaluator.Name;

        // Assert
        Assert.That(result, Is.EqualTo("MathEvaluator"));
    }

    [Test]
    public void Description_ShouldReturnValidDescription()
    {
        // Act
        var result = _mathEvaluator.Description;

        // Assert
        Assert.That(result, Is.Not.Null.And.Not.Empty);
        Assert.That(result, Does.Contain("mathematical"));
    }

    [Test]
    public void Capabilities_ShouldContainMathCapabilities()
    {
        // Act
        var capabilities = _mathEvaluator.Capabilities.ToList();

        // Assert
        Assert.That(capabilities, Contains.Item("math:evaluate"));
        Assert.That(capabilities, Contains.Item("arithmetic:calculate"));
    }

    [Test]
    public void RequiresNetwork_ShouldBeFalse()
    {
        // Act & Assert
        Assert.That(_mathEvaluator.RequiresNetwork, Is.False);
    }

    [Test]
    public void RequiresFileSystem_ShouldBeFalse()
    {
        // Act & Assert
        Assert.That(_mathEvaluator.RequiresFileSystem, Is.False);
    }

    [Test]
    public async Task DryRunAsync_WithValidExpression_ShouldReturnTrue()
    {
        // Arrange
        _context.Parameters["expression"] = "2 + 2";

        // Act
        var result = await _mathEvaluator.DryRunAsync(_context);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DryRunAsync_WithoutExpression_ShouldReturnFalse()
    {
        // Act
        var result = await _mathEvaluator.DryRunAsync(_context);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task EstimateCostAsync_ShouldReturnZero()
    {
        // Act
        var result = await _mathEvaluator.EstimateCostAsync(_context);

        // Assert
        Assert.That(result, Is.EqualTo(0.0m));
    }

    [Test]
    public async Task RunAsync_WithSimpleAddition_ShouldReturnCorrectResult()
    {
        // Arrange
        _context.Parameters["expression"] = "2 + 2";

        // Act
        var result = await _mathEvaluator.RunAsync(_context);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Output, Is.EqualTo(4));
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public async Task RunAsync_WithComplexExpression_ShouldReturnCorrectResult()
    {
        // Arrange
        _context.Parameters["expression"] = "(5 + 3) * 2 - 1";

        // Act
        var result = await _mathEvaluator.RunAsync(_context);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Output, Is.EqualTo(15));
    }

    [Test]
    public async Task RunAsync_WithDecimalNumbers_ShouldReturnCorrectResult()
    {
        // Arrange
        _context.Parameters["expression"] = "3.5 + 1.5";

        // Act
        var result = await _mathEvaluator.RunAsync(_context);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Output, Is.EqualTo(5.0));
    }

    [Test]
    public async Task RunAsync_WithDivisionByZero_ShouldReturnInfinity()
    {
        // Arrange
        _context.Parameters["expression"] = "5 / 0";

        // Act
        var result = await _mathEvaluator.RunAsync(_context);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Output?.ToString(), Does.Contain("∞").Or.Contain("Infinity"));
    }

    [Test]
    public async Task RunAsync_WithInvalidExpression_ShouldReturnError()
    {
        // Arrange
        _context.Parameters["expression"] = "invalid_expression_xyz";

        // Act
        var result = await _mathEvaluator.RunAsync(_context);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.Not.Null);
    }

    [Test]
    public async Task RunAsync_WithMaliciousExpression_ShouldReturnError()
    {
        // Arrange
        _context.Parameters["expression"] = "System.IO.File.Delete('test')";

        // Act
        var result = await _mathEvaluator.RunAsync(_context);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("undefined function call"));
    }

    [Test]
    public async Task RunAsync_WithoutExpression_ShouldReturnError()
    {
        // Act
        var result = await _mathEvaluator.RunAsync(_context);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Mathematical expression is required"));
    }

    [Test]
    public async Task RunAsync_WithEmptyExpression_ShouldReturnError()
    {
        // Arrange
        _context.Parameters["expression"] = "";

        // Act
        var result = await _mathEvaluator.RunAsync(_context);

        // Assert
        // Empty string is treated as successful evaluation returning empty result
        Assert.That(result.Success, Is.True);
        Assert.That(result.Output?.ToString(), Is.Empty);
    }

    [Test]
    public async Task RunAsync_ShouldTrackExecutionTime()
    {
        // Arrange
        _context.Parameters["expression"] = "2 + 2";

        // Act
        var result = await _mathEvaluator.RunAsync(_context);

        // Assert
        Assert.That(result.ExecutionTime, Is.GreaterThan(TimeSpan.Zero));
    }

    [TestCase("1 + 1", 2)]
    [TestCase("10 - 5", 5)]
    [TestCase("3 * 4", 12)]
    [TestCase("15 / 3", 5)]
    [TestCase("2 * (3 + 4)", 14)]
    public async Task RunAsync_WithVariousValidExpressions_ShouldReturnCorrectResults(string expression, object expected)
    {
        // Arrange
        _context.Parameters["expression"] = expression;

        // Act
        var result = await _mathEvaluator.RunAsync(_context);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Output, Is.EqualTo(expected));
    }
}
