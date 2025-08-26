# PowerShell script to run Ollama Agent Suite with Intelligent Mode
Write-Host "🚀 Running Ollama Agent Suite with Intelligent Mode" -ForegroundColor Green
Write-Host "📝 Query: 'What is 2 + 2'" -ForegroundColor Cyan
Write-Host ""

# Change to script directory
Set-Location $PSScriptRoot

# Run the application
dotnet run --project "src\Ollama.Interface.Cli\Ollama.Interface.Cli.csproj" -- "What is 2 + 2" "Intelligent"

Write-Host ""
Write-Host "✅ Execution completed" -ForegroundColor Green
Read-Host "Press Enter to exit"
