using System;
using System.Text.Json;
using System.IO;

class Program
{
    static void Main()
    {
        try
        {
            var json = File.ReadAllText("test_json_parsing.json");
            Console.WriteLine($"JSON Length: {json.Length}");
            
            // Count character at position 877
            Console.WriteLine($"Character at position 877: '{(json.Length > 877 ? json[877] : ' ')}'");
            Console.WriteLine($"Line 11 context: {GetLineContext(json, 11, 877)}");
            
            var parsed = JsonSerializer.Deserialize<JsonElement>(json);
            Console.WriteLine("JSON parsing successful!");
            Console.WriteLine($"Task completed: {parsed.GetProperty("taskCompleted").GetBoolean()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    static string GetLineContext(string text, int targetLine, int bytePosition)
    {
        var lines = text.Split('\n');
        if (targetLine <= lines.Length)
        {
            var line = lines[targetLine - 1];
            return $"Line {targetLine}: {line}";
        }
        
        if (bytePosition < text.Length)
        {
            var start = Math.Max(0, bytePosition - 50);
            var end = Math.Min(text.Length, bytePosition + 50);
            return $"Position {bytePosition}: ...{text.Substring(start, end - start)}...";
        }
        
        return "Position not found";
    }
}
