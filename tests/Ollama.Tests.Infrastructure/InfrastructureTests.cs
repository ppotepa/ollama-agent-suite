using Ollama.Infrastructure.Agents;

namespace Ollama.Tests.Infrastructure;

public class UniversalAgentAdapterTests
{
    [Fact]
    public void UniversalAgentAdapter_ShouldAnswerQueries()
    {
        // Arrange
        var agent = new UniversalAgentAdapter("test-model");
        var prompt = "Test prompt";

        // Act
        var response = agent.Answer(prompt);

        // Assert
        Assert.Contains("test-model", response);
        Assert.Contains(prompt, response);
    }

    [Fact]
    public void UniversalAgentAdapter_ShouldThinkAboutPrompts()
    {
        // Arrange
        var agent = new UniversalAgentAdapter("test-model");
        var prompt = "Complex problem";

        // Act
        var thinking = agent.Think(prompt);

        // Assert
        Assert.Contains("test-model", thinking);
        Assert.Contains(prompt, thinking);
        Assert.Contains("Thinking", thinking);
    }

    [Fact]
    public void UniversalAgentAdapter_ShouldCreatePlans()
    {
        // Arrange
        var agent = new UniversalAgentAdapter("test-model");
        var prompt = "Build something";

        // Act
        var plan = agent.Plan(prompt);

        // Assert
        Assert.NotNull(plan);
        // Additional assertions would depend on the actual implementation
    }

    [Fact]
    public void UniversalAgentAdapter_ShouldExecuteInstructions()
    {
        // Arrange
        var agent = new UniversalAgentAdapter("test-model");
        var instruction = "Do something";

        // Act
        var result = agent.Act(instruction);

        // Assert
        Assert.NotNull(result);
        // Additional assertions would depend on the actual implementation
    }
}
