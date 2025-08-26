@echo off
echo Running Ollama Agent Suite with SingleQuery Mode
echo Query: "What is 2+2"
echo.

cd /d "%~dp0"
dotnet run --project "src\Ollama.Interface.Cli\Ollama.Interface.Cli.csproj" -- "What is 2+2" "SingleQuery"

pause
