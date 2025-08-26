#!/bin/bash
# Bash script for Linux/macOS to run Ollama Agent Suite with Intelligent Mode

echo "🚀 Running Ollama Agent Suite with Intelligent Mode"
echo "📝 Query: 'What is 2 + 2'"
echo ""

# Change to script directory
cd "$(dirname "$0")"

# Run the application
dotnet run --project "src/Ollama.Interface.Cli/Ollama.Interface.Cli.csproj" -- "What is 2 + 2" "Intelligent"

echo ""
echo "✅ Execution completed"
read -p "Press Enter to exit"
