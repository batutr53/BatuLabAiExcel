Write-Host "Office Ai - Batu Lab. - Quick Fix" -ForegroundColor Blue
Write-Host "=================================="

Write-Host "Step 1: Testing Python..." -ForegroundColor Yellow
python --version
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Python not found. Please install Python first." -ForegroundColor Red
    exit 1
}
Write-Host "Python OK" -ForegroundColor Green

Write-Host "Step 2: Installing excel-mcp-server..." -ForegroundColor Yellow
pip install excel-mcp-server --upgrade
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: pip install failed" -ForegroundColor Red
    exit 1
}
Write-Host "Installation OK" -ForegroundColor Green

Write-Host "Step 3: Testing installation..." -ForegroundColor Yellow
python -m excel_mcp_server --help | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "excel-mcp-server works!" -ForegroundColor Green
} else {
    Write-Host "WARNING: Test failed, but may still work" -ForegroundColor Yellow
}

Write-Host "Step 4: Updating config..." -ForegroundColor Yellow
$configFile = "src\BatuLabAiExcel\appsettings.json"
if (Test-Path $configFile) {
    $content = Get-Content $configFile -Raw
    $content = $content -replace '"ServerScript": "uvx excel-mcp-server stdio"', '"ServerScript": "python -m excel_mcp_server stdio"'
    Set-Content $configFile $content
    Write-Host "Config updated" -ForegroundColor Green
} else {
    Write-Host "Config file not found" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "DONE! Now run:" -ForegroundColor Green
Write-Host "dotnet run --project src\BatuLabAiExcel" -ForegroundColor Cyan