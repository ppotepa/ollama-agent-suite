@echo off
REM Interactive Ollama Agent Suite Runner (Batch version)
REM This script prompts for a query and runs it in intelligent mode

title Ollama Agent Suite - Interactive Mode

echo.
echo 🚀 Ollama Agent Suite - Interactive Mode
echo =========================================
echo.

:main_loop
echo 💭 Enter your query (or 'exit' to quit):
echo    Examples:
echo    - What is 2 + 2?
echo    - Explain quantum computing  
echo    - Write a Python function to sort a list
echo    - Analyze this code for bugs
echo.
set /p "query=Query: "

REM Check for exit commands
if /i "%query%"=="exit" goto :exit
if /i "%query%"=="quit" goto :exit
if "%query%"=="" (
    echo ❌ Please enter a valid query.
    echo.
    goto :main_loop
)

echo.
echo 🤖 Processing query with Intelligent Mode...
echo Query: %query%
echo Mode: intelligent
echo ----------------------------------------

REM Check if executable exists
if not exist "src\Ollama.Interface.Cli\bin\Debug\net9.0\Ollama.Interface.Cli.exe" (
    echo ❌ CLI executable not found!
    echo 🔧 Building the solution first...
    dotnet build OllamaAgentSuite.sln
    if errorlevel 1 (
        echo ❌ Build failed!
        pause
        goto :main_loop
    )
    echo ✅ Build successful!
    echo.
)

REM Record start time (simplified)
echo Starting execution...

REM Run the CLI application
"src\Ollama.Interface.Cli\bin\Debug\net9.0\Ollama.Interface.Cli.exe" --mode intelligent -q "%query%"

echo.
echo ----------------------------------------
if errorlevel 1 (
    echo ❌ Query failed
) else (
    echo ✅ Query completed successfully
)
echo.
echo 🔄 Ready for next query...
echo.

REM Ask if user wants to continue
set /p "continue=Continue? (y/N): "
if /i "%continue%"=="y" goto :main_loop
if /i "%continue%"=="yes" goto :main_loop

:exit
echo 👋 Thanks for using Ollama Agent Suite!
pause
exit /b 0
