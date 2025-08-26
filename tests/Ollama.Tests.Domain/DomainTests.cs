using Ollama.Domain.Execution;
using Ollama.Domain.Strategies;

namespace Ollama.Tests.Domain;

public class ExecutionNodeTests
{
    [Fact]
    public void ExecutionNode_ShouldCreateWithTypeAndContent()
    {
        // Arrange
        var nodeType = ExecutionNodeType.UserQuery;
        var content = "Test query";

        // Act
        var node = new ExecutionNode(nodeType, content);

        // Assert
        Assert.Equal(nodeType, node.NodeType);
        Assert.Equal(content, node.Content);
        Assert.Empty(node.Children);
        Assert.Null(node.Parent);
    }

    [Fact]
    public void ExecutionNode_ShouldAddChildWithParentReference()
    {
        // Arrange
        var parent = new ExecutionNode(ExecutionNodeType.UserQuery, "Parent");
        var child = new ExecutionNode(ExecutionNodeType.AgentResponse, "Child");

        // Act
        parent.AddChild(child);

        // Assert
        Assert.Single(parent.Children);
        Assert.Equal(child, parent.Children[0]);
        Assert.Equal(parent, child.Parent);
    }
}

public class ExecutionContextTests
{
    [Fact]
    public void ExecutionContext_ShouldCreateWithQuery()
    {
        // Arrange
        var query = "Test query";

        // Act
        var context = new Ollama.Domain.Strategies.ExecutionContext(query);

        // Assert
        Assert.Equal(query, context.Query);
        Assert.Null(context.SessionId);
        Assert.Empty(context.Metadata);
    }

    [Fact]
    public void ExecutionContext_ShouldCreateWithQueryAndSessionId()
    {
        // Arrange
        var query = "Test query";
        var sessionId = "test-session-123";

        // Act
        var context = new Ollama.Domain.Strategies.ExecutionContext(query, sessionId);

        // Assert
        Assert.Equal(query, context.Query);
        Assert.Equal(sessionId, context.SessionId);
    }
}
