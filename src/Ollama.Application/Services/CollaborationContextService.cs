namespace Ollama.Application.Services;

public sealed class CollaborationContextService
{
    private readonly List<string> _notes = new();

    public void AddNote(string note)
    {
        _notes.Add(note);
    }

    public IReadOnlyList<string> GetNotes()
    {
        return _notes.AsReadOnly();
    }

    public void Clear()
    {
        _notes.Clear();
    }
}
