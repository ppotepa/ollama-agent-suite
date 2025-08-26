namespace Ollama.Domain.Execution;

public enum ExecutionNodeType
{
    UserQuery,
    InterceptorAnalysis,
    CommandExecution,
    AgentResponse,
    FinalResult
}
