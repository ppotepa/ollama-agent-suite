using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Ollama.Domain.Tools;
using Ollama.Infrastructure.Tools;

namespace Ollama.Tests.Infrastructure.Tools;

[TestFixture]
public class ToolRepositoryTests
{
    private ToolRepository _toolRepository;
    private Mock<ILogger<ToolRepository>> _mockLogger;
    private Mock<ITool> _mockTool;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<ToolRepository>>();
        _toolRepository = new ToolRepository(_mockLogger.Object);
        
        _mockTool = new Mock<ITool>();
        _mockTool.Setup(t => t.Name).Returns("TestTool");
        _mockTool.Setup(t => t.Description).Returns("A test tool");
        _mockTool.Setup(t => t.Capabilities).Returns(new[] { "test:capability", "mock:tool" });
    }

    [Test]
    public void RegisterTool_WithValidTool_ShouldRegisterSuccessfully()
    {
        // Act
        _toolRepository.RegisterTool(_mockTool.Object);

        // Assert
        var retrievedTool = _toolRepository.GetToolByName("TestTool");
        Assert.That(retrievedTool, Is.Not.Null);
        Assert.That(retrievedTool.Name, Is.EqualTo("TestTool"));
    }

    [Test]
    public void RegisterTool_WithSameToolTwice_ShouldLogWarning()
    {
        // Arrange
        _toolRepository.RegisterTool(_mockTool.Object);

        // Act
        _toolRepository.RegisterTool(_mockTool.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("already registered")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public void GetToolByName_WithExistingTool_ShouldReturnTool()
    {
        // Arrange
        _toolRepository.RegisterTool(_mockTool.Object);

        // Act
        var result = _toolRepository.GetToolByName("TestTool");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("TestTool"));
    }

    [Test]
    public void GetToolByName_WithNonExistingTool_ShouldReturnNull()
    {
        // Act
        var result = _toolRepository.GetToolByName("NonExistentTool");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetToolByName_WithCaseInsensitiveName_ShouldReturnTool()
    {
        // Arrange
        _toolRepository.RegisterTool(_mockTool.Object);

        // Act
        var result = _toolRepository.GetToolByName("testtool");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("TestTool"));
    }

    [Test]
    public void FindToolsByCapability_WithExistingCapability_ShouldReturnMatchingTools()
    {
        // Arrange
        _toolRepository.RegisterTool(_mockTool.Object);

        // Act
        var result = _toolRepository.FindToolsByCapability("test:capability");

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result.First().Name, Is.EqualTo("TestTool"));
    }

    [Test]
    public void FindToolsByCapability_WithNonExistingCapability_ShouldReturnEmpty()
    {
        // Arrange
        _toolRepository.RegisterTool(_mockTool.Object);

        // Act
        var result = _toolRepository.FindToolsByCapability("nonexistent:capability");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FindToolsByCapability_WithCaseInsensitiveCapability_ShouldReturnMatchingTools()
    {
        // Arrange
        _toolRepository.RegisterTool(_mockTool.Object);

        // Act
        var result = _toolRepository.FindToolsByCapability("TEST:CAPABILITY");

        // Assert
        Assert.That(result, Is.Not.Empty);
        Assert.That(result.First().Name, Is.EqualTo("TestTool"));
    }

    [Test]
    public void GetAllTools_WithMultipleTools_ShouldReturnAllTools()
    {
        // Arrange
        var mockTool2 = new Mock<ITool>();
        mockTool2.Setup(t => t.Name).Returns("TestTool2");
        mockTool2.Setup(t => t.Capabilities).Returns(new[] { "test:capability2" });

        _toolRepository.RegisterTool(_mockTool.Object);
        _toolRepository.RegisterTool(mockTool2.Object);

        // Act
        var result = _toolRepository.GetAllTools();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.Select(t => t.Name), Contains.Item("TestTool"));
        Assert.That(result.Select(t => t.Name), Contains.Item("TestTool2"));
    }

    [Test]
    public void GetAllTools_WithNoTools_ShouldReturnEmpty()
    {
        // Act
        var result = _toolRepository.GetAllTools();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FindToolsByCapability_WithMultipleMatchingTools_ShouldReturnAllMatching()
    {
        // Arrange
        var mockTool2 = new Mock<ITool>();
        mockTool2.Setup(t => t.Name).Returns("TestTool2");
        mockTool2.Setup(t => t.Capabilities).Returns(new[] { "test:capability", "other:capability" });

        _toolRepository.RegisterTool(_mockTool.Object);
        _toolRepository.RegisterTool(mockTool2.Object);

        // Act
        var result = _toolRepository.FindToolsByCapability("test:capability");

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.Select(t => t.Name), Contains.Item("TestTool"));
        Assert.That(result.Select(t => t.Name), Contains.Item("TestTool2"));
    }
}
