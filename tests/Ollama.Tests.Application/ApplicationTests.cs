using NUnit.Framework;
using Ollama.Application.Services;
using Ollama.Domain.Execution;
using System;

namespace Ollama.Tests.Application;

[TestFixture]
public class ExecutionTreeBuilderTests
{
    [Test]
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
        Assert.That(tree.NodeType, Is.EqualTo(ExecutionNodeType.UserQuery));
        Assert.That(tree.Content, Is.EqualTo("Test query"));
        Assert.That(tree.Children.Count, Is.EqualTo(1));
        
        var analysisNode = tree.Children[0];
        Assert.That(analysisNode.NodeType, Is.EqualTo(ExecutionNodeType.InterceptorAnalysis));
        Assert.That(analysisNode.Content, Is.EqualTo("Analysis result"));
    }

    [Test]
    public void ExecutionTreeBuilder_ShouldThrowWhenBuildWithoutBegin()
    {
        // Arrange
        var builder = new ExecutionTreeBuilder();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }
}

[TestFixture]
public class CollaborationContextServiceTests
{
    [Test]
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
        Assert.That(notes.Count, Is.EqualTo(2));
        Assert.That(notes, Contains.Item(note1));
        Assert.That(notes, Contains.Item(note2));
    }

    [Test]
    public void CollaborationContextService_ShouldClearNotes()
    {
        // Arrange
        var service = new CollaborationContextService();
        service.AddNote("Test note");

        // Act
        service.Clear();
        var notes = service.GetNotes();

        // Assert
        Assert.That(notes.Count, Is.EqualTo(0));
    }
}
