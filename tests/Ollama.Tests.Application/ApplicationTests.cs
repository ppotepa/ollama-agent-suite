using Ollama.Application.Services;
using Ollama.Domain.Execution;

namespace Ollama.Tests.Application;

public class ExecutionTreeBuilderTests
{
    [Fact]
    public void ExecutionTreeBuilder_ShouldBuildSimpleTree()
    {
        // Arrange
        var builder = new ExecutionTreeBuilder();

        // Act
        var tree = builder
            .Begin("Test query")
            .AddAnalysis("Analysis result")
            .Finish("Final result")
            .Build();

        // Assert
        Assert.Equal(ExecutionNodeType.UserQuery, tree.NodeType);
        Assert.Equal("Test query", tree.Content);
        Assert.Single(tree.Children);
        
        var analysisNode = tree.Children[0];
        Assert.Equal(ExecutionNodeType.InterceptorAnalysis, analysisNode.NodeType);
        Assert.Equal("Analysis result", analysisNode.Content);
    }

    [Fact]
    public void ExecutionTreeBuilder_ShouldThrowWhenBuildWithoutBegin()
    {
        // Arrange
        var builder = new ExecutionTreeBuilder();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }
}

public class CollaborationContextServiceTests
{
    [Fact]
    public void CollaborationContextService_ShouldAddAndRetrieveNotes()
    {
        // Arrange
        var service = new CollaborationContextService();
        var note1 = "First note";
        var note2 = "Second note";

        // Act
        service.AddNote(note1);
        service.AddNote(note2);
        var notes = service.GetNotes();

        // Assert
        Assert.Equal(2, notes.Count);
        Assert.Contains(note1, notes);
        Assert.Contains(note2, notes);
    }

    [Fact]
    public void CollaborationContextService_ShouldClearNotes()
    {
        // Arrange
        var service = new CollaborationContextService();
        service.AddNote("Test note");

        // Act
        service.Clear();
        var notes = service.GetNotes();

        // Assert
        Assert.Empty(notes);
    }
}
