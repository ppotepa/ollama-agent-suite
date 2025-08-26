namespace Ollama.Domain.Agents;

public interface IRepositoryPort
{
    IEnumerable<string> List(string path);
    // Add other repo ops as ports when needed
}
