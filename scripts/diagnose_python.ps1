# Office Ai - Batu Lab. - Python Diagnosis Script
# This script helps diagnose Python and MCP server issues

param(
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Continue"

# Colors for output
$Green = "`e[32m"
$Red = "`e[31m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Cyan = "`e[36m"
$Reset = "`e[0m"

function Write-ColoredOutput {
    param($Message, $Color = $Reset)
    Write-Host "$Color$Message$Reset"
}

function Write-Section {
    param($Title)
    Write-Host ""
    Write-ColoredOutput "═══ $Title ═══" $Blue
}

function Write-Success {
    param($Message)
    Write-ColoredOutput "✓ $Message" $Green
}

function Write-Error {
    param($Message)
    Write-ColoredOutput "✗ $Message" $Red
}

function Write-Warning {
    param($Message)
    Write-ColoredOutput "⚠ $Message" $Yellow
}

function Write-Info {
    param($Message)
    Write-ColoredOutput "ℹ $Message" $Cyan
}

function Test-CommandExists {
    param($Command)
    try {
        $null = Get-Command $Command -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

function Test-PythonCommand {
    param($Command)
    try {
        $process = Start-Process -FilePath $Command -ArgumentList "--version" -NoNewWindow -Wait -PassThru -RedirectStandardOutput "temp_python_version.txt" -RedirectStandardError "temp_python_error.txt"
        
        if ($process.ExitCode -eq 0 -and (Test-Path "temp_python_version.txt")) {
            $version = Get-Content "temp_python_version.txt" -Raw
            Remove-Item "temp_python_version.txt" -ErrorAction SilentlyContinue
            Remove-Item "temp_python_error.txt" -ErrorAction SilentlyContinue
            return @{ Success = $true; Version = $version.Trim(); ExitCode = $process.ExitCode }
        }
        
        $error = ""
        if (Test-Path "temp_python_error.txt") {
            $error = Get-Content "temp_python_error.txt" -Raw
        }
        
        Remove-Item "temp_python_version.txt" -ErrorAction SilentlyContinue
        Remove-Item "temp_python_error.txt" -ErrorAction SilentlyContinue
        return @{ Success = $false; Version = $null; Error = $error; ExitCode = $process.ExitCode }
    }
    catch {
        return @{ Success = $false; Version = $null; Error = $_.Exception.Message; ExitCode = -1 }
    }
}

Write-ColoredOutput @"
╔══════════════════════════════════════════════════════════════╗
║                    Office Ai - Batu Lab.                    ║
║                  Python Diagnosis Tool                      ║
╚══════════════════════════════════════════════════════════════╝
"@ $Blue

Write-Section "System Information"
Write-Info "OS: $($env:OS)"
Write-Info "Architecture: $($env:PROCESSOR_ARCHITECTURE)"
Write-Info "PowerShell Version: $($PSVersionTable.PSVersion)"
Write-Info "User: $($env:USERNAME)"

Write-Section "Environment Variables"
$pathEntries = $env:PATH -split ';'
$pythonPaths = $pathEntries | Where-Object { $_ -match 'python' -or $_ -match 'Python' }

if ($pythonPaths) {
    Write-Success "Found Python-related PATH entries:"
    foreach ($path in $pythonPaths) {
        Write-Host "  - $path" -ForegroundColor Gray
    }
} else {
    Write-Warning "No Python-related PATH entries found"
}

Write-Section "Python Command Tests"
$pythonCommands = @("python", "python3", "py", "python.exe")

foreach ($cmd in $pythonCommands) {
    Write-Host "Testing '$cmd'..." -NoNewline
    
    if (Test-CommandExists $cmd) {
        $result = Test-PythonCommand $cmd
        if ($result.Success) {
            Write-Success " Found: $($result.Version)"
        } else {
            Write-Error " Failed: $($result.Error)"
            if ($Verbose) {
                Write-Host "    Exit Code: $($result.ExitCode)" -ForegroundColor Gray
            }
        }
    } else {
        Write-Error " Not found in PATH"
    }
}

Write-Section "Python Installation Locations"
$commonPaths = @(
    "C:\Python312\python.exe",
    "C:\Python311\python.exe", 
    "C:\Python310\python.exe",
    "C:\Program Files\Python312\python.exe",
    "C:\Program Files\Python311\python.exe",
    "C:\Program Files\Python310\python.exe",
    "C:\Program Files (x86)\Python312\python.exe",
    "C:\Program Files (x86)\Python311\python.exe",
    "C:\Program Files (x86)\Python310\python.exe",
    "$env:USERPROFILE\AppData\Local\Programs\Python\Python312\python.exe",
    "$env:USERPROFILE\AppData\Local\Programs\Python\Python311\python.exe",
    "$env:USERPROFILE\AppData\Local\Programs\Python\Python310\python.exe",
    "$env:USERPROFILE\AppData\Local\Microsoft\WindowsApps\python.exe"
)

$foundInstallations = @()

foreach ($path in $commonPaths) {
    if (Test-Path $path) {
        $result = Test-PythonCommand $path
        if ($result.Success) {
            Write-Success "Found: $path ($($result.Version))"
            $foundInstallations += @{ Path = $path; Version = $result.Version }
        } else {
            Write-Warning "Found but not working: $path"
        }
    }
}

if ($foundInstallations.Count -eq 0) {
    Write-Error "No working Python installations found in common locations"
}

Write-Section "Package Manager Tests"
$packageManagers = @("pip", "pip3", "uvx")

foreach ($pm in $packageManagers) {
    Write-Host "Testing '$pm'..." -NoNewline
    
    if (Test-CommandExists $pm) {
        try {
            $process = Start-Process -FilePath $pm -ArgumentList "--version" -NoNewWindow -Wait -PassThru -RedirectStandardOutput "temp_pm_version.txt" -RedirectStandardError "temp_pm_error.txt"
            
            if ($process.ExitCode -eq 0 -and (Test-Path "temp_pm_version.txt")) {
                $version = Get-Content "temp_pm_version.txt" -Raw
                Write-Success " Found: $($version.Trim())"
            } else {
                Write-Error " Failed to get version"
            }
            
            Remove-Item "temp_pm_version.txt" -ErrorAction SilentlyContinue
            Remove-Item "temp_pm_error.txt" -ErrorAction SilentlyContinue
        }
        catch {
            Write-Error " Error: $($_.Exception.Message)"
        }
    } else {
        Write-Warning " Not found"
    }
}

Write-Section "Excel MCP Server Check"
if ($foundInstallations.Count -gt 0) {
    $bestPython = $foundInstallations | Sort-Object Version -Descending | Select-Object -First 1
    Write-Info "Testing with: $($bestPython.Path)"
    
    try {
        $process = Start-Process -FilePath $bestPython.Path -ArgumentList "-m", "excel_mcp_server", "--help" -NoNewWindow -Wait -PassThru -RedirectStandardOutput "temp_mcp_test.txt" -RedirectStandardError "temp_mcp_error.txt"
        
        if ($process.ExitCode -eq 0) {
            Write-Success "excel-mcp-server is installed and working"
        } else {
            Write-Error "excel-mcp-server not installed or not working"
            if ($Verbose -and (Test-Path "temp_mcp_error.txt")) {
                $error = Get-Content "temp_mcp_error.txt" -Raw
                Write-Host "Error: $error" -ForegroundColor Red
            }
        }
        
        Remove-Item "temp_mcp_test.txt" -ErrorAction SilentlyContinue
        Remove-Item "temp_mcp_error.txt" -ErrorAction SilentlyContinue
    }
    catch {
        Write-Error "Error testing excel-mcp-server: $($_.Exception.Message)"
    }
} else {
    Write-Warning "Cannot test excel-mcp-server: No working Python found"
}

Write-Section "Configuration Check"
$workingDir = ".\excel_files"
$configFile = Join-Path $workingDir "mcp_config.json"

if (Test-Path $workingDir) {
    Write-Success "Working directory exists: $workingDir"
    
    if (Test-Path $configFile) {
        Write-Success "Configuration file found: $configFile"
        
        try {
            $config = Get-Content $configFile | ConvertFrom-Json
            Write-Info "Python path in config: $($config.python_path)"
            Write-Info "MCP command: $($config.mcp_server.command) $($config.mcp_server.args -join ' ')"
        }
        catch {
            Write-Warning "Configuration file exists but cannot be read: $($_.Exception.Message)"
        }
    } else {
        Write-Warning "No configuration file found"
    }
} else {
    Write-Warning "Working directory does not exist: $workingDir"
}

Write-Section "Recommendations"
if ($foundInstallations.Count -eq 0) {
    Write-ColoredOutput @"
❌ No working Python installation found!

Recommended actions:
1. Install Python from https://python.org/downloads/
2. Or run: setup.bat (includes Python installation)
3. Or run: scripts\setup_python_and_mcp.ps1 -InstallPython

Make sure to:
- Check "Add Python to PATH" during installation
- Restart your command prompt after installation
"@ $Red
} elseif (!(Test-CommandExists "pip")) {
    Write-ColoredOutput @"
⚠️  Python found but pip is not available!

Recommended actions:
1. Reinstall Python with pip enabled
2. Or manually install pip
3. Or use the comprehensive setup script
"@ $Yellow
} else {
    $bestPython = $foundInstallations | Sort-Object Version -Descending | Select-Object -First 1
    Write-ColoredOutput @"
✅ Python setup looks good!

Best Python installation: $($bestPython.Path) ($($bestPython.Version))

Next steps:
1. Run: scripts\setup_python_and_mcp.ps1
2. Or run: setup.bat for guided setup
3. Test with: scripts\run_backend_check.ps1
"@ $Green
}

Write-ColoredOutput @"

For more help:
- Run setup.bat for automatic setup
- Run scripts\setup_python_and_mcp.ps1 -Verbose for detailed setup
- Run scripts\run_backend_check.ps1 to test MCP server
"@ $Cyan