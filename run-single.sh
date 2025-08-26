#!/bin/bash
# Bash script for Linux/macOS to run Ollama Agent Suite with SingleQuery Mode

QUERY="${1:-What is 2+2}"

echo "🚀 Running Ollama Agent Suite with SingleQuery Mode"
echo "📝 Query: '$QUERY'"
echo ""

# Change to script directory
cd "$(dirname "$0")"

# Run the application
dotnet run --project "src/Ollama.Interface.Cli/Ollama.Interface.Cli.csproj" -- "$QUERY" "SingleQuery"

echo ""
echo "✅ Execution completed"
echo ""
echo "💡 Usage examples:"
echo "  ./run-single.sh"
echo "  ./run-single.sh 'Calculate 5 * 3'"
echo "  ./run-single.sh 'What is the capital of France?'"

read -p "Press Enter to exit"
