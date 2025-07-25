# Office Ai - Batu Lab. - Complete System Setup
# This script installs everything needed to run the application

param(
    [switch]$Force
)

Write-Host @"
╔══════════════════════════════════════════════════════════════╗
║              Office Ai - Batu Lab. Setup                    ║
║           Complete System Requirements Installer            ║
╚══════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Blue

Write-Host "Starting complete system setup..." -ForegroundColor Green
Write-Host ""

# Function to check if running as admin
function Test-Admin {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Function to test if command exists
function Test-Command {
    param($CommandName)
    try {
        Get-Command $CommandName -ErrorAction Stop | Out-Null
        return $true
    } catch {
        return $false
    }
}

# Step 1: Check if running as admin
Write-Host "1. Checking administrator privileges..." -ForegroundColor Yellow
if (-not (Test-Admin)) {
    Write-Host "   ⚠️  Not running as administrator" -ForegroundColor Yellow
    Write-Host "   Some installations may require admin rights" -ForegroundColor Yellow
} else {
    Write-Host "   ✅ Running as administrator" -ForegroundColor Green
}

# Step 2: Check and install Python
Write-Host "2. Checking Python installation..." -ForegroundColor Yellow
if (Test-Command "python") {
    $pythonVersion = python --version 2>&1
    Write-Host "   ✅ Python found: $pythonVersion" -ForegroundColor Green
} else {
    Write-Host "   ❌ Python not found. Installing..." -ForegroundColor Red
    
    if (Test-Command "winget") {
        Write-Host "   📥 Installing Python via winget..." -ForegroundColor Cyan
        winget install Python.Python.3.12 --accept-source-agreements --accept-package-agreements
        
        # Refresh PATH
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
        
        if (Test-Command "python") {
            Write-Host "   ✅ Python installed successfully" -ForegroundColor Green
        } else {
            Write-Host "   ❌ Python installation failed via winget" -ForegroundColor Red
            Write-Host "   📥 Please download manually from: https://python.org" -ForegroundColor Cyan
            Write-Host "   ⚠️  Make sure to check 'Add to PATH' during installation" -ForegroundColor Yellow
            exit 1
        }
    } else {
        Write-Host "   ❌ winget not available" -ForegroundColor Red
        Write-Host "   📥 Please install Python manually from: https://python.org" -ForegroundColor Cyan
        Write-Host "   ⚠️  Make sure to check 'Add to PATH' during installation" -ForegroundColor Yellow
        exit 1
    }
}

# Step 3: Check and upgrade pip
Write-Host "3. Checking pip..." -ForegroundColor Yellow
if (Test-Command "pip") {
    Write-Host "   ✅ pip found" -ForegroundColor Green
    Write-Host "   🔄 Upgrading pip..." -ForegroundColor Cyan
    python -m pip install --upgrade pip --quiet
    Write-Host "   ✅ pip upgraded" -ForegroundColor Green
} else {
    Write-Host "   ❌ pip not found" -ForegroundColor Red
    Write-Host "   🔄 Installing pip..." -ForegroundColor Cyan
    python -m ensurepip --upgrade
    if (Test-Command "pip") {
        Write-Host "   ✅ pip installed" -ForegroundColor Green
    } else {
        Write-Host "   ❌ pip installation failed" -ForegroundColor Red
        exit 1
    }
}

# Step 4: Install excel-mcp-server
Write-Host "4. Installing excel-mcp-server..." -ForegroundColor Yellow
try {
    Write-Host "   📦 Installing via pip..." -ForegroundColor Cyan
    pip install excel-mcp-server --upgrade --quiet
    
    # Test installation
    python -c "import excel_mcp_server; print('✅ Module imported successfully')" 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ excel-mcp-server installed and working" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Installation completed but import test failed" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ Installation failed: $($_.Exception.Message)" -ForegroundColor Red
    
    # Try alternative method
    Write-Host "   🔄 Trying user installation..." -ForegroundColor Cyan
    pip install --user excel-mcp-server --upgrade --quiet
    
    python -c "import excel_mcp_server; print('✅ Module imported successfully')" 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ excel-mcp-server installed via user method" -ForegroundColor Green
    } else {
        Write-Host "   ❌ Both installation methods failed" -ForegroundColor Red
        exit 1
    }
}

# Step 5: Check .NET 9 SDK
Write-Host "5. Checking .NET 9 SDK..." -ForegroundColor Yellow
if (Test-Command "dotnet") {
    $dotnetVersion = dotnet --version 2>&1
    Write-Host "   ✅ .NET found: $dotnetVersion" -ForegroundColor Green
    
    # Check if .NET 9 specifically
    $dotnetInfo = dotnet --info 2>&1 | Select-String "9\."
    if ($dotnetInfo) {
        Write-Host "   ✅ .NET 9 SDK available" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  .NET 9 SDK not found, but other version available" -ForegroundColor Yellow
        Write-Host "   📥 Consider upgrading to .NET 9 from: https://dot.net" -ForegroundColor Cyan
    }
} else {
    Write-Host "   ❌ .NET SDK not found" -ForegroundColor Red
    if (Test-Command "winget") {
        Write-Host "   📥 Installing .NET 9 SDK via winget..." -ForegroundColor Cyan
        winget install Microsoft.DotNet.SDK.9 --accept-source-agreements --accept-package-agreements
        
        # Refresh PATH
        $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")
        
        if (Test-Command "dotnet") {
            Write-Host "   ✅ .NET SDK installed successfully" -ForegroundColor Green
        } else {
            Write-Host "   ❌ .NET SDK installation failed" -ForegroundColor Red
            Write-Host "   📥 Please download manually from: https://dot.net" -ForegroundColor Cyan
        }
    } else {
        Write-Host "   📥 Please install .NET 9 SDK from: https://dot.net" -ForegroundColor Cyan
    }
}

# Step 6: Create directories
Write-Host "6. Creating required directories..." -ForegroundColor Yellow
$dirs = @("excel_files", "logs")
foreach ($dir in $dirs) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "   📁 Created: $dir" -ForegroundColor Green
    } else {
        Write-Host "   ✅ Directory exists: $dir" -ForegroundColor Green
    }
}

# Step 7: Fix configuration
Write-Host "7. Updating application configuration..." -ForegroundColor Yellow
$configFile = "src\BatuLabAiExcel\appsettings.json"
if (Test-Path $configFile) {
    try {
        $config = Get-Content $configFile | ConvertFrom-Json
        $config.Mcp.ServerScript = "python -m excel_mcp_server stdio"
        $config.Mcp.AutoInstall = $true
        $config | ConvertTo-Json -Depth 10 | Set-Content $configFile -Encoding UTF8
        Write-Host "   ✅ Configuration updated" -ForegroundColor Green
    } catch {
        Write-Host "   ⚠️  Configuration update failed, will try text replacement" -ForegroundColor Yellow
        $content = Get-Content $configFile -Raw
        $content = $content -replace '"ServerScript": "uvx excel-mcp-server stdio"', '"ServerScript": "python -m excel_mcp_server stdio"'
        $content = $content -replace '"AutoInstall": false', '"AutoInstall": true'
        Set-Content $configFile $content -Encoding UTF8
        Write-Host "   ✅ Configuration updated via text replacement" -ForegroundColor Green
    }
} else {
    Write-Host "   ❌ Configuration file not found: $configFile" -ForegroundColor Red
}

# Step 8: Build the application
Write-Host "8. Building the application..." -ForegroundColor Yellow
if (Test-Command "dotnet") {
    try {
        $buildOutput = dotnet build --verbosity quiet 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✅ Application built successfully" -ForegroundColor Green
        } else {
            Write-Host "   ❌ Build failed:" -ForegroundColor Red
            Write-Host $buildOutput -ForegroundColor Red
        }
    } catch {
        Write-Host "   ❌ Build error: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "   ⚠️  Skipping build - .NET SDK not available" -ForegroundColor Yellow
}

# Step 9: Final test
Write-Host "9. Final system test..." -ForegroundColor Yellow
$allGood = $true

# Test Python
if (Test-Command "python") {
    Write-Host "   ✅ Python: OK" -ForegroundColor Green
} else {
    Write-Host "   ❌ Python: FAILED" -ForegroundColor Red
    $allGood = $false
}

# Test pip
if (Test-Command "pip") {
    Write-Host "   ✅ pip: OK" -ForegroundColor Green
} else {
    Write-Host "   ❌ pip: FAILED" -ForegroundColor Red
    $allGood = $false
}

# Test excel-mcp-server
python -c "import excel_mcp_server" 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "   ✅ excel-mcp-server: OK" -ForegroundColor Green
} else {
    Write-Host "   ❌ excel-mcp-server: FAILED" -ForegroundColor Red
    $allGood = $false
}

# Test .NET
if (Test-Command "dotnet") {
    Write-Host "   ✅ .NET SDK: OK" -ForegroundColor Green
} else {
    Write-Host "   ❌ .NET SDK: FAILED" -ForegroundColor Red
    $allGood = $false
}

Write-Host ""
if ($allGood) {
    Write-Host "🎉 SETUP COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your system is ready! Next steps:" -ForegroundColor Cyan
    Write-Host "1. Run the application:" -ForegroundColor White
    Write-Host "   dotnet run --project src\BatuLabAiExcel" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "2. Test Excel integration:" -ForegroundColor White
    Write-Host "   .\scripts\run_backend_check.ps1" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "3. Add your Claude API key to:" -ForegroundColor White
    Write-Host "   src\BatuLabAiExcel\appsettings.json" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "✨ Office Ai - Batu Lab. is ready to use!" -ForegroundColor Green
} else {
    Write-Host "⚠️  SETUP COMPLETED WITH WARNINGS" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Some components failed to install. Please:" -ForegroundColor Yellow
    Write-Host "1. Check the errors above" -ForegroundColor White
    Write-Host "2. Install missing components manually" -ForegroundColor White
    Write-Host "3. Run this script again if needed" -ForegroundColor White
}

Write-Host ""
Write-Host "Setup log completed at: $(Get-Date)" -ForegroundColor Gray