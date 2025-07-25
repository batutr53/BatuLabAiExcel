# Office Ai - Batu Lab. Complete Setup Script
# Installs all required components

Write-Host "=====================================" -ForegroundColor Blue
Write-Host "  Office Ai - Batu Lab. Setup" -ForegroundColor Blue  
Write-Host "=====================================" -ForegroundColor Blue
Write-Host ""

# Helper function to test commands
function Test-CommandExists {
    param($Command)
    try {
        Get-Command $Command -ErrorAction Stop | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

# Step 1: Check Python
Write-Host "1. Checking Python..." -ForegroundColor Yellow
if (Test-CommandExists "python") {
    $pythonVer = python --version
    Write-Host "   Found: $pythonVer" -ForegroundColor Green
}
else {
    Write-Host "   Python not found. Installing..." -ForegroundColor Red
    
    if (Test-CommandExists "winget") {
        Write-Host "   Installing Python via winget..." -ForegroundColor Cyan
        winget install Python.Python.3.12
        
        # Refresh environment
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
        
        if (Test-CommandExists "python") {
            Write-Host "   Python installed successfully!" -ForegroundColor Green
        }
        else {
            Write-Host "   Failed to install Python. Please install manually from python.org" -ForegroundColor Red
            exit 1
        }
    }
    else {
        Write-Host "   Please install Python manually from python.org" -ForegroundColor Red
        exit 1
    }
}

# Step 2: Upgrade pip
Write-Host "2. Checking pip..." -ForegroundColor Yellow
if (Test-CommandExists "pip") {
    Write-Host "   Upgrading pip..." -ForegroundColor Cyan
    python -m pip install --upgrade pip
    Write-Host "   pip upgraded" -ForegroundColor Green
}
else {
    Write-Host "   pip not found. Installing..." -ForegroundColor Red
    python -m ensurepip --upgrade
}

# Step 3: Install excel-mcp-server
Write-Host "3. Installing excel-mcp-server..." -ForegroundColor Yellow
pip install excel-mcp-server --upgrade
if ($LASTEXITCODE -eq 0) {
    Write-Host "   excel-mcp-server installed" -ForegroundColor Green
}
else {
    Write-Host "   Trying user installation..." -ForegroundColor Yellow
    pip install --user excel-mcp-server --upgrade
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   excel-mcp-server installed (user)" -ForegroundColor Green
    }
    else {
        Write-Host "   Installation failed" -ForegroundColor Red
        exit 1
    }
}

# Step 4: Test excel-mcp-server
Write-Host "4. Testing excel-mcp-server..." -ForegroundColor Yellow
python -c "import excel_mcp_server; print('OK')"
if ($LASTEXITCODE -eq 0) {
    Write-Host "   excel-mcp-server works!" -ForegroundColor Green
}
else {
    Write-Host "   Test failed, but may still work" -ForegroundColor Yellow
}

# Step 5: Check .NET
Write-Host "5. Checking .NET SDK..." -ForegroundColor Yellow
if (Test-CommandExists "dotnet") {
    $dotnetVer = dotnet --version
    Write-Host "   Found: $dotnetVer" -ForegroundColor Green
}
else {
    Write-Host "   .NET not found. Installing..." -ForegroundColor Red
    
    if (Test-CommandExists "winget") {
        Write-Host "   Installing .NET 9 SDK..." -ForegroundColor Cyan
        winget install Microsoft.DotNet.SDK.9
        
        # Refresh environment
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
        
        if (Test-CommandExists "dotnet") {
            Write-Host "   .NET SDK installed!" -ForegroundColor Green
        }
        else {
            Write-Host "   Please install .NET 9 SDK manually from dot.net" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "   Please install .NET 9 SDK from dot.net" -ForegroundColor Yellow
    }
}

# Step 6: Create directories
Write-Host "6. Creating directories..." -ForegroundColor Yellow
if (!(Test-Path "excel_files")) {
    New-Item -ItemType Directory -Path "excel_files" | Out-Null
    Write-Host "   Created excel_files folder" -ForegroundColor Green
}
if (!(Test-Path "logs")) {
    New-Item -ItemType Directory -Path "logs" | Out-Null
    Write-Host "   Created logs folder" -ForegroundColor Green
}

# Step 7: Fix config
Write-Host "7. Updating configuration..." -ForegroundColor Yellow
$configFile = "src\BatuLabAiExcel\appsettings.json"
if (Test-Path $configFile) {
    $content = Get-Content $configFile -Raw
    $content = $content -replace '"ServerScript": "uvx excel-mcp-server stdio"', '"ServerScript": "python -m excel_mcp_server stdio"'
    $content = $content -replace '"AutoInstall": false', '"AutoInstall": true'
    Set-Content $configFile $content
    Write-Host "   Configuration updated" -ForegroundColor Green
}
else {
    Write-Host "   Config file not found" -ForegroundColor Yellow
}

# Step 8: Build application
Write-Host "8. Building application..." -ForegroundColor Yellow
if (Test-CommandExists "dotnet") {
    dotnet build --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   Build successful!" -ForegroundColor Green
    }
    else {
        Write-Host "   Build failed - check for errors" -ForegroundColor Red
    }
}

# Final summary
Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host "         SETUP COMPLETE!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Add your Claude API key to src\BatuLabAiExcel\appsettings.json"
Write-Host "2. Run: dotnet run --project src\BatuLabAiExcel"
Write-Host ""
Write-Host "Your Office Ai - Batu Lab. is ready!" -ForegroundColor Green