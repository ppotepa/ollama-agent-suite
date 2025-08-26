@echo off
REM OllamaAgentSuite Build Script for Windows

echo 🔨 Building OllamaAgentSuite...

REM Build the solution
dotnet build OllamaAgentSuite.sln

if %ERRORLEVEL% EQU 0 (
    echo ✅ Build successful!
    
    REM Run tests
    echo 🧪 Running tests...
    dotnet test OllamaAgentSuite.sln --no-build
    
    if %ERRORLEVEL% EQU 0 (
        echo ✅ All tests passed!
        echo 🚀 Ready to run!
        echo.
        echo Usage examples:
        echo   dotnet run --project src/Ollama.Interface.Cli/Ollama.Interface.Cli.csproj "Your query here"
        echo   dotnet run --project src/Ollama.Interface.Cli/Ollama.Interface.Cli.csproj "Your query" "mode"
        echo.
        echo Available modes: single, collaborative, intelligent
    ) else (
        echo ❌ Tests failed!
        exit /b 1
    )
) else (
    echo ❌ Build failed!
    exit /b 1
)
