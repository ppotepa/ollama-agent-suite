# Flexible PowerShell script to run Ollama Agent Suite
param(
    [string]$Query = "What is 2 + 2",
    [string]$Mode = "Intelligent"
)

Write-Host "🤖 Ollama Agent Suite Runner" -ForegroundColor Magenta
Write-Host "================================" -ForegroundColor Magenta
Write-Host "📝 Query: '$Query'" -ForegroundColor Cyan
Write-Host "🎯 Mode: $Mode" -ForegroundColor Yellow
Write-Host ""

# Change to script directory
Set-Location $PSScriptRoot

# Run the application
Write-Host "🚀 Starting execution..." -ForegroundColor Green
dotnet run --project "src\Ollama.Interface.Cli\Ollama.Interface.Cli.csproj" -- "$Query" "$Mode"

Write-Host ""
Write-Host "✅ Execution completed" -ForegroundColor Green
Write-Host ""
Write-Host "💡 Usage examples:" -ForegroundColor Yellow
Write-Host "  .\run.ps1" -ForegroundColor Gray
Write-Host "  .\run.ps1 -Query 'Explain quantum computing'" -ForegroundColor Gray
Write-Host "  .\run.ps1 -Query 'Write a Python function' -Mode 'Collaborative'" -ForegroundColor Gray

Read-Host "Press Enter to exit"
