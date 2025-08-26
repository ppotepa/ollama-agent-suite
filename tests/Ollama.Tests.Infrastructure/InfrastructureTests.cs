using NUnit.Framework;
using Ollama.Infrastructure.Agents;

namespace Ollama.Tests.Infrastructure;

[TestFixture]
public class UniversalAgentAdapterTests
{
    [Test]
    public void UniversalAgentAdapter_ShouldAnswerQueries()
    {
        // Arrange
        var agent = new UniversalAgentAdapter("test-model");
        var prompt = "Test prompt";

        // Act
        var response = agent.Answer(prompt);

        // Assert
        Assert.That(response, Does.Contain("test-model"));
        Assert.That(response, Does.Contain(prompt));
    }

    [Test]
    public void UniversalAgentAdapter_ShouldThinkAboutPrompts()
    {
        // Arrange
        var agent = new UniversalAgentAdapter("test-model");
        var prompt = "Complex problem";

        // Act
        var thinking = agent.Think(prompt);

        // Assert
        Assert.That(thinking, Does.Contain("test-model"));
        Assert.That(thinking, Does.Contain(prompt));
        Assert.That(thinking, Does.Contain("Thinking"));
    }

    [Test]
    public void UniversalAgentAdapter_ShouldCreatePlans()
    {
        // Arrange
        var agent = new UniversalAgentAdapter("test-model");
        var prompt = "Build something";

        // Act
        var plan = agent.Plan(prompt);

        // Assert
        Assert.That(plan, Is.Not.Null);
        // Additional assertions would depend on the actual implementation
    }

    [Test]
    public void UniversalAgentAdapter_ShouldExecuteInstructions()
    {
        // Arrange
        var agent = new UniversalAgentAdapter("test-model");
        var instruction = "Do something";

        // Act
        var result = agent.Act(instruction);

        // Assert
        Assert.That(result, Is.Not.Null);
        // Additional assertions would depend on the actual implementation
    }
}
