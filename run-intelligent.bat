@echo off
echo Running Ollama Agent Suite with Intelligent Mode
echo Query: "What is 2 + 2"
echo.

cd /d "%~dp0"
dotnet run --project "src\Ollama.Interface.Cli\Ollama.Interface.Cli.csproj" -- "What is 2 + 2" "Intelligent"

pause
