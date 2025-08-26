using Ollama.Domain.Execution;

namespace Ollama.Application.Services;

public sealed class ExecutionTreeBuilder
{
    private ExecutionNode? _root;
    private ExecutionNode? _cursor;

    public ExecutionTreeBuilder Begin(string query)
    {
        _root = new ExecutionNode(ExecutionNodeType.UserQuery, query);
        _cursor = _root;
        return this;
    }

    public ExecutionTreeBuilder AddAnalysis(string content)
        => Add(ExecutionNodeType.InterceptorAnalysis, content);

    public ExecutionTreeBuilder AddCommand(string content)
        => Add(ExecutionNodeType.CommandExecution, content);

    public ExecutionTreeBuilder AddResponse(string content)
        => Add(ExecutionNodeType.AgentResponse, content);

    public ExecutionTreeBuilder Finish(string result)
        => Add(ExecutionNodeType.FinalResult, result);

    private ExecutionTreeBuilder Add(ExecutionNodeType type, string content)
    {
        if (_cursor == null)
            throw new InvalidOperationException("Must call Begin() first");

        var node = new ExecutionNode(type, content);
        _cursor.AddChild(node);
        _cursor = node;
        return this;
    }

    public ExecutionNode Build()
    {
        if (_root == null)
            throw new InvalidOperationException("Must call Begin() first");
        
        return _root;
    }
}
