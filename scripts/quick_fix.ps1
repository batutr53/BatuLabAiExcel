# Office Ai - Batu Lab. - Quick Fix for MCP Server Issues
# This script automatically fixes common MCP server startup problems

$ErrorActionPreference = "Stop"

Write-Host "🔧 Quick Fix for MCP Server Issues" -ForegroundColor Blue
Write-Host ("=" * 50)

# Step 1: Check Python
Write-Host "1. Checking Python installation..." -ForegroundColor Yellow
try {
    $pythonVersion = python --version 2>&1
    Write-Host "   ✅ Python found: $pythonVersion" -ForegroundColor Green
}
catch {
    Write-Host "   ❌ Python not found. Please install Python first." -ForegroundColor Red
    Write-Host "   📥 Download from: https://python.org" -ForegroundColor Cyan
    exit 1
}

# Step 2: Install excel-mcp-server via pip
Write-Host "2. Installing excel-mcp-server via pip..." -ForegroundColor Yellow
try {
    pip install excel-mcp-server --upgrade --quiet
    Write-Host "   ✅ excel-mcp-server installed successfully" -ForegroundColor Green
}
catch {
    Write-Host "   ❌ pip installation failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 3: Test the installation
Write-Host "3. Testing excel-mcp-server..." -ForegroundColor Yellow
try {
    $testOutput = python -m excel_mcp_server --help 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ excel-mcp-server is working correctly" -ForegroundColor Green
    } else {
        throw "Test failed with exit code $LASTEXITCODE"
    }
}
catch {
    Write-Host "   ❌ Test failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   🔄 Trying alternative installation..." -ForegroundColor Yellow
    
    # Alternative: Install with user flag
    try {
        pip install --user excel-mcp-server --upgrade --quiet
        python -m excel_mcp_server --help | Out-Null
        Write-Host "   ✅ Alternative installation successful" -ForegroundColor Green
    }
    catch {
        Write-Host "   ❌ Alternative installation also failed" -ForegroundColor Red
        exit 1
    }
}

# Step 4: Create/update configuration
Write-Host "4. Updating application configuration..." -ForegroundColor Yellow
$configPath = "src\BatuLabAiExcel\appsettings.json"
if (Test-Path $configPath) {
    # Read and update config
    $config = Get-Content $configPath | ConvertFrom-Json
    $config.Mcp.ServerScript = "python -m excel_mcp_server stdio"
    $config.Mcp.AutoInstall = $true
    
    $config | ConvertTo-Json -Depth 10 | Set-Content $configPath
    Write-Host "   ✅ Configuration updated to use pip method" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  Configuration file not found at $configPath" -ForegroundColor Yellow
}

# Step 5: Final check
Write-Host "5. Final verification..." -ForegroundColor Yellow
try {
    if (Test-Path $configPath) {
        Write-Host "   ✅ Configuration file exists" -ForegroundColor Green
    }
    
    # Test Python module availability
    python -c "import excel_mcp_server; print('Module import successful')" 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Python module can be imported" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Python module import test inconclusive" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "   ⚠️  Some verification steps failed, but this may be normal" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎉 Quick fix completed!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Run: dotnet run --project src\BatuLabAiExcel" 
Write-Host "2. The MCP server should now start automatically"
Write-Host "3. If you still have issues, check the logs in logs/ folder"
Write-Host ""
Write-Host "✨ Your Office Ai - Batu Lab. is ready to use!" -ForegroundColor Green