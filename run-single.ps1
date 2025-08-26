# PowerShell script to run Ollama Agent Suite with SingleQuery Mode
param(
    [string]$Query = "What is 2+2"
)

Write-Host "🚀 Running Ollama Agent Suite with SingleQuery Mode" -ForegroundColor Green
Write-Host "📝 Query: '$Query'" -ForegroundColor Cyan
Write-Host ""

# Change to script directory
Set-Location $PSScriptRoot

# Run the application
dotnet run --project "src\Ollama.Interface.Cli\Ollama.Interface.Cli.csproj" -- "$Query" "SingleQuery"

Write-Host ""
Write-Host "✅ Execution completed" -ForegroundColor Green
Write-Host ""
Write-Host "💡 Usage examples:" -ForegroundColor Yellow
Write-Host "  .\run-single.ps1" -ForegroundColor Gray
Write-Host "  .\run-single.ps1 -Query 'Calculate 5 * 3'" -ForegroundColor Gray
Write-Host "  .\run-single.ps1 -Query 'What is the capital of France?'" -ForegroundColor Gray

Read-Host "Press Enter to exit"
