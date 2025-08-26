param(
    [Parameter(Position=0)]
    [string]$Query,
    [string]$Mode = "intelligent"
)

if ([string]::IsNullOrWhiteSpace($Query)) {
    Write-Host "Ollama Agent Suite - Quick Query Runner" -ForegroundColor Cyan
    Write-Host "Usage: .\run-query.ps1 'Your question here'" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Gray
    Write-Host "  .\run-query.ps1 'What is 2 + 2?'" -ForegroundColor Gray
    Write-Host "  .\run-query.ps1 'Explain machine learning'" -ForegroundColor Gray
    Write-Host ""
    
    Write-Host "Enter your query: " -ForegroundColor Green -NoNewline
    $Query = Read-Host
    
    if ([string]::IsNullOrWhiteSpace($Query)) {
        Write-Host "No query provided!" -ForegroundColor Red
        exit 1
    }
}

$exePath = "src\Ollama.Interface.Cli\bin\Debug\net9.0\Ollama.Interface.Cli.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "Building solution..." -ForegroundColor Yellow
    dotnet build OllamaAgentSuite.sln
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "Processing: $Query" -ForegroundColor Cyan
Write-Host "Mode: $Mode" -ForegroundColor White
Write-Host "==================================================" -ForegroundColor DarkGray

$startTime = Get-Date
& $exePath --mode $Mode -q $Query
$exitCode = $LASTEXITCODE
$endTime = Get-Date
$duration = ($endTime - $startTime).TotalSeconds

Write-Host ""
Write-Host "==================================================" -ForegroundColor DarkGray
if ($exitCode -eq 0) {
    Write-Host "Completed in $([math]::Round($duration, 2)) seconds" -ForegroundColor Green
} else {
    Write-Host "Failed after $([math]::Round($duration, 2)) seconds" -ForegroundColor Red
}

exit $exitCode
