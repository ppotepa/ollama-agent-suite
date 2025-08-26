#!/usr/bin/env pwsh
# Interactive Ollama Agent Suite Runner
# This script prompts for a query and runs it in intelligent mode

param(
    [string]$Mode = "intelligent"
)

Write-Host "🚀 Ollama Agent Suite - Interactive Mode" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Function to get user input with validation
function Get-UserQuery {
    do {
        Write-Host "💭 Enter your query (or 'exit' to quit):" -ForegroundColor Yellow
        Write-Host "   Examples:" -ForegroundColor Gray
        Write-Host "   - What is 2 + 2?" -ForegroundColor Gray
        Write-Host "   - Explain quantum computing" -ForegroundColor Gray
        Write-Host "   - Write a Python function to sort a list" -ForegroundColor Gray
        Write-Host "   - Analyze this code for bugs" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Query: " -ForegroundColor Green -NoNewline
        $query = Read-Host
        
        if ($query.ToLower() -eq "exit" -or $query.ToLower() -eq "quit") {
            Write-Host "👋 Goodbye!" -ForegroundColor Cyan
            exit 0
        }
        
        if ([string]::IsNullOrWhiteSpace($query)) {
            Write-Host "❌ Please enter a valid query." -ForegroundColor Red
            Write-Host ""
        }
    } while ([string]::IsNullOrWhiteSpace($query))
    
    return $query.Trim()
}

# Function to run the CLI application
function Invoke-OllamaAgent {
    param(
        [string]$Query,
        [string]$Mode = "intelligent"
    )
    
    $exePath = "src\Ollama.Interface.Cli\bin\Debug\net9.0\Ollama.Interface.Cli.exe"
    
    if (-not (Test-Path $exePath)) {
        Write-Host "❌ CLI executable not found at: $exePath" -ForegroundColor Red
        Write-Host "🔧 Building the solution first..." -ForegroundColor Yellow
        
        try {
            dotnet build OllamaAgentSuite.sln
            if ($LASTEXITCODE -ne 0) {
                Write-Host "❌ Build failed!" -ForegroundColor Red
                return $false
            }
            Write-Host "✅ Build successful!" -ForegroundColor Green
        }
        catch {
            Write-Host "❌ Error building solution: $_" -ForegroundColor Red
            return $false
        }
    }
    
    Write-Host ""
    Write-Host "🤖 Processing query with Intelligent Mode..." -ForegroundColor Cyan
    Write-Host "Query: $Query" -ForegroundColor White
    Write-Host "Mode: $Mode" -ForegroundColor White
    Write-Host "----------------------------------------" -ForegroundColor DarkGray
    
    try {
        & $exePath --mode $Mode -q $Query
        return $LASTEXITCODE -eq 0
    }
    catch {
        Write-Host "❌ Error running CLI: $_" -ForegroundColor Red
        return $false
    }
}

# Main execution loop
Write-Host "🎯 Welcome to the Interactive Ollama Agent Suite!" -ForegroundColor Green
Write-Host "This will run your queries using the Intelligent Mode with AI planning." -ForegroundColor White
Write-Host ""

do {
    try {
        $query = Get-UserQuery
        Write-Host ""
        
        $startTime = Get-Date
        $success = Invoke-OllamaAgent -Query $query -Mode $Mode
        $endTime = Get-Date
        $duration = ($endTime - $startTime).TotalSeconds
        
        Write-Host ""
        Write-Host "----------------------------------------" -ForegroundColor DarkGray
        if ($success) {
            Write-Host "✅ Query completed successfully in $([math]::Round($duration, 2)) seconds" -ForegroundColor Green
        } else {
            Write-Host "❌ Query failed after $([math]::Round($duration, 2)) seconds" -ForegroundColor Red
        }
        Write-Host ""
        Write-Host "🔄 Ready for next query..." -ForegroundColor Cyan
        Write-Host ""
    }
    catch {
        Write-Host "❌ Unexpected error: $_" -ForegroundColor Red
        Write-Host ""
    }
    
    # Ask if user wants to continue
    Write-Host "Continue? (y/N): " -ForegroundColor Yellow -NoNewline
    $continue = Read-Host
    
    if ($continue.ToLower() -ne "y" -and $continue.ToLower() -ne "yes") {
        Write-Host "👋 Thanks for using Ollama Agent Suite!" -ForegroundColor Cyan
        break
    }
    Write-Host ""
    
} while ($true)
