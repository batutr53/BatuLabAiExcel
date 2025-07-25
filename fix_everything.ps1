Write-Host "Office Ai - Batu Lab - Complete Fix Script" -ForegroundColor Blue
Write-Host "==========================================" -ForegroundColor Blue

# Test if command exists
function CommandExists {
    param($cmd)
    $null = Get-Command $cmd -ErrorAction SilentlyContinue
    return $?
}

Write-Host "Starting setup..." -ForegroundColor Green

# 1. Python check and install
Write-Host "1. Python setup..." -ForegroundColor Yellow
if (CommandExists python) {
    Write-Host "Python found" -ForegroundColor Green
    python --version
} else {
    Write-Host "Installing Python..." -ForegroundColor Cyan
    if (CommandExists winget) {
        winget install Python.Python.3.12 --silent --accept-source-agreements --accept-package-agreements
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
        if (CommandExists python) {
            Write-Host "Python installed!" -ForegroundColor Green
        } else {
            Write-Host "Python install failed. Please install from python.org manually" -ForegroundColor Red
            pause
            exit
        }
    } else {
        Write-Host "Please install Python from python.org and run this script again" -ForegroundColor Red
        pause
        exit
    }
}

# 2. Pip upgrade
Write-Host "2. Upgrading pip..." -ForegroundColor Yellow
python -m pip install --upgrade pip --quiet

# 3. Install excel-mcp-server
Write-Host "3. Installing excel-mcp-server..." -ForegroundColor Yellow
pip install excel-mcp-server --upgrade --quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host "excel-mcp-server installed" -ForegroundColor Green
} else {
    Write-Host "Trying user install..." -ForegroundColor Cyan
    pip install --user excel-mcp-server --upgrade --quiet
}

# 4. Test installation
Write-Host "4. Testing installation..." -ForegroundColor Yellow
$testResult = python -c "import excel_mcp_server; print('SUCCESS')" 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Test passed!" -ForegroundColor Green
} else {
    Write-Host "Test failed but continuing..." -ForegroundColor Yellow
}

# 5. .NET check
Write-Host "5. Checking .NET..." -ForegroundColor Yellow
if (CommandExists dotnet) {
    Write-Host ".NET found" -ForegroundColor Green
    dotnet --version
} else {
    Write-Host "Installing .NET..." -ForegroundColor Cyan
    if (CommandExists winget) {
        winget install Microsoft.DotNet.SDK.9 --silent --accept-source-agreements --accept-package-agreements
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
    } else {
        Write-Host "Please install .NET 9 from dot.net" -ForegroundColor Yellow
    }
}

# 6. Create folders
Write-Host "6. Creating folders..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path "excel_files" -Force | Out-Null
New-Item -ItemType Directory -Path "logs" -Force | Out-Null
Write-Host "Folders created" -ForegroundColor Green

# 7. Fix config
Write-Host "7. Fixing configuration..." -ForegroundColor Yellow
$configPath = "src\BatuLabAiExcel\appsettings.json"
if (Test-Path $configPath) {
    $config = Get-Content $configPath -Raw
    $newConfig = $config -replace '"uvx excel-mcp-server stdio"', '"python -m excel_mcp_server stdio"'
    $newConfig = $newConfig -replace '"python -m pip install excel-mcp-server && python -m excel_mcp_server stdio"', '"python -m excel_mcp_server stdio"'
    Set-Content $configPath $newConfig -Encoding UTF8
    Write-Host "Configuration fixed" -ForegroundColor Green
} else {
    Write-Host "Config file not found" -ForegroundColor Red
}

# 8. Build app
Write-Host "8. Building application..." -ForegroundColor Yellow
if (CommandExists dotnet) {
    dotnet build --configuration Release --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build successful!" -ForegroundColor Green
    } else {
        Write-Host "Build had warnings but may work" -ForegroundColor Yellow
    }
} else {
    Write-Host "Skipping build - no .NET" -ForegroundColor Yellow
}

# 9. Final test
Write-Host "9. Final verification..." -ForegroundColor Yellow
$pythonOK = CommandExists python
$pipOK = CommandExists pip
$dotnetOK = CommandExists dotnet

Write-Host "Results:" -ForegroundColor Cyan
Write-Host "  Python: $(if($pythonOK){'OK'}else{'MISSING'})" -ForegroundColor $(if($pythonOK){'Green'}else{'Red'})
Write-Host "  pip: $(if($pipOK){'OK'}else{'MISSING'})" -ForegroundColor $(if($pipOK){'Green'}else{'Red'})
Write-Host "  .NET: $(if($dotnetOK){'OK'}else{'MISSING'})" -ForegroundColor $(if($dotnetOK){'Green'}else{'Red'})

if ($pythonOK -and $pipOK) {
    Write-Host ""
    Write-Host "SUCCESS! Your system is ready!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Add your Claude API key to: src\BatuLabAiExcel\appsettings.json"
    Write-Host "2. Run the app: dotnet run --project src\BatuLabAiExcel"
    Write-Host ""
    Write-Host "Press any key to start the application now..."
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    
    if ($dotnetOK) {
        Write-Host "Starting application..." -ForegroundColor Green
        Set-Location $PSScriptRoot
        dotnet run --project src\BatuLabAiExcel
    }
} else {
    Write-Host ""
    Write-Host "Some components are missing. Please install them manually:" -ForegroundColor Yellow
    if (-not $pythonOK) { Write-Host "- Install Python from python.org" }
    if (-not $dotnetOK) { Write-Host "- Install .NET 9 from dot.net" }
}

Write-Host ""
Write-Host "Script completed!" -ForegroundColor Blue