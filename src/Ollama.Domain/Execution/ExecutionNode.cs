namespace Ollama.Domain.Execution;

public sealed class ExecutionNode
{
    public ExecutionNodeType NodeType { get; }
    public string Content { get; }
    public Dictionary<string, object> Metadata { get; } = new();
    public List<ExecutionNode> Children { get; } = new();
    public ExecutionNode? Parent { get; private set; }

    public ExecutionNode(ExecutionNodeType type, string content)
    {
        NodeType = type;
        Content = content;
    }

    public void AddChild(ExecutionNode child)
    {
        child.Parent = this;
        Children.Add(child);
    }
}
