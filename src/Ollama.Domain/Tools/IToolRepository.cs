namespace Ollama.Domain.Tools;

public interface IToolRepository
{
    void RegisterTool(ITool tool);
    ITool? GetToolByName(string name);
    IEnumerable<ITool> FindToolsByCapability(string capability);
    IEnumerable<ITool> GetAllTools();
}
