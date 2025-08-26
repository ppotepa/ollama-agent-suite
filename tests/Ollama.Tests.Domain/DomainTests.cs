using NUnit.Framework;
using Ollama.Domain.Execution;
using Ollama.Domain.Strategies;

namespace Ollama.Tests.Domain;

[TestFixture]
public class ExecutionNodeTests
{
    [Test]
    public void ExecutionNode_ShouldCreateWithTypeAndContent()
    {
        // Arrange
        var nodeType = ExecutionNodeType.UserQuery;
        var content = "Test query";

        // Act
        var node = new ExecutionNode(nodeType, content);

        // Assert
        Assert.That(node.NodeType, Is.EqualTo(nodeType));
        Assert.That(node.Content, Is.EqualTo(content));
        Assert.That(node.Children.Count, Is.EqualTo(0));
        Assert.That(node.Parent, Is.Null);
    }

    [Test]
    public void ExecutionNode_ShouldAddChildWithParentReference()
    {
        // Arrange
        var parent = new ExecutionNode(ExecutionNodeType.UserQuery, "Parent");
        var child = new ExecutionNode(ExecutionNodeType.AgentResponse, "Child");

        // Act
        parent.AddChild(child);

        // Assert
        Assert.That(parent.Children.Count, Is.EqualTo(1));
        Assert.That(parent.Children[0], Is.EqualTo(child));
        Assert.That(child.Parent, Is.EqualTo(parent));
    }
}

[TestFixture]
public class ExecutionContextTests
{
    [Test]
    public void ExecutionContext_ShouldCreateWithQuery()
    {
        // Arrange
        var query = "Test query";

        // Act
        var context = new Ollama.Domain.Strategies.ExecutionContext(query);

        // Assert
        Assert.That(context.Query, Is.EqualTo(query));
        Assert.That(context.SessionId, Is.Null);
        Assert.That(context.Metadata.Count, Is.EqualTo(0));
    }

    [Test]
    public void ExecutionContext_ShouldCreateWithQueryAndSessionId()
    {
        // Arrange
        var query = "Test query";
        var sessionId = "test-session-123";

        // Act
        var context = new Ollama.Domain.Strategies.ExecutionContext(query, sessionId);

        // Assert
        Assert.That(context.Query, Is.EqualTo(query));
        Assert.That(context.SessionId, Is.EqualTo(sessionId));
    }
}
