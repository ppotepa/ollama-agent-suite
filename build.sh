#!/bin/bash

# OllamaAgentSuite Build Script

echo "🔨 Building OllamaAgentSuite..."

# Build the solution
dotnet build OllamaAgentSuite.sln

if [ $? -eq 0 ]; then
    echo "✅ Build successful!"
    
    # Run tests
    echo "🧪 Running tests..."
    dotnet test OllamaAgentSuite.sln --no-build
    
    if [ $? -eq 0 ]; then
        echo "✅ All tests passed!"
        echo "🚀 Ready to run!"
        echo ""
        echo "Usage examples:"
        echo "  dotnet run --project src/Ollama.Interface.Cli/Ollama.Interface.Cli.csproj \"Your query here\""
        echo "  dotnet run --project src/Ollama.Interface.Cli/Ollama.Interface.Cli.csproj \"Your query\" \"mode\""
        echo ""
        echo "Available modes: single, collaborative, intelligent"
    else
        echo "❌ Tests failed!"
        exit 1
    fi
else
    echo "❌ Build failed!"
    exit 1
fi
