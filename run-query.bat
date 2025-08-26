@echo off
REM Simple query runner for Ollama Agent Suite
setlocal EnableDelayedExpansion

if "%~1"=="" (
    echo Ollama Agent Suite - Quick Query Runner
    echo Usage: run-query.bat "Your question here"
    echo.
    echo Examples:
    echo   run-query.bat "What is 2 + 2?"
    echo   run-query.bat "Explain machine learning"
    echo.
    set /p "query=Enter your query: "
    if "!query!"=="" (
        echo No query provided!
        pause
        exit /b 1
    )
) else (
    set "query=%~1"
)

echo.
echo Processing: !query!
echo Mode: intelligent
echo ==================================================

REM Check if executable exists
if not exist "src\Ollama.Interface.Cli\bin\Debug\net9.0\Ollama.Interface.Cli.exe" (
    echo Building solution...
    dotnet build OllamaAgentSuite.sln
    if errorlevel 1 (
        echo Build failed!
        pause
        exit /b 1
    )
)

REM Run the CLI application
"src\Ollama.Interface.Cli\bin\Debug\net9.0\Ollama.Interface.Cli.exe" --mode intelligent -q "!query!"

echo.
echo ==================================================
if errorlevel 1 (
    echo Failed
) else (
    echo Completed successfully
)

if "%~1"=="" pause
