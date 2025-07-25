# Office Ai - Batu Lab. - MCP Setup Script
# This script sets up the excel-mcp-server dependency
# For comprehensive setup including Python installation, use setup_python_and_mcp.ps1

param(
    [string]$InstallPath = ".\excel_files",
    [switch]$Force = $false,
    [switch]$Verbose = $false,
    [switch]$UseComprehensiveSetup = $false
)

$ErrorActionPreference = "Stop"

# Check if comprehensive setup should be used
if ($UseComprehensiveSetup) {
    Write-Host "Redirecting to comprehensive Python and MCP setup..." -ForegroundColor Cyan
    $scriptPath = Join-Path $PSScriptRoot "setup_python_and_mcp.ps1"
    $args = @("-WorkingDirectory", $InstallPath)
    if ($Force) { $args += "-ForceReinstall" }
    if ($Verbose) { $args += "-Verbose" }
    
    & $scriptPath @args
    exit $LASTEXITCODE
}

# Colors for output
$Green = "`e[32m"
$Red = "`e[31m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Reset = "`e[0m"

function Write-ColoredOutput {
    param($Message, $Color = $Reset)
    Write-Host "$Color$Message$Reset"
}

function Write-Step {
    param($Message)
    Write-ColoredOutput "==> $Message" $Blue
}

function Write-Success {
    param($Message)
    Write-ColoredOutput "✓ $Message" $Green
}

function Write-Warning {
    param($Message)
    Write-ColoredOutput "⚠ $Message" $Yellow
}

function Write-Error {
    param($Message)
    Write-ColoredOutput "✗ $Message" $Red
}

function Test-Command {
    param($Command)
    try {
        Get-Command $Command -ErrorAction Stop | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Test-PythonPackage {
    param($PackageName)
    try {
        $result = python -c "import $PackageName; print('OK')" 2>$null
        return $result -eq "OK"
    }
    catch {
        return $false
    }
}

# Main setup process
try {
    Write-ColoredOutput @"
╔══════════════════════════════════════════════════════════════╗
║                    Office Ai - Batu Lab.                    ║
║                 Excel MCP Server Setup                      ║
╚══════════════════════════════════════════════════════════════╝
"@ $Blue

    # Check prerequisites
    Write-Step "Checking prerequisites..."
    
    # Check Python
    if (-not (Test-Command "python")) {
        Write-Error "Python is not installed or not in PATH"
        Write-Host "Please install Python from https://python.org and add it to PATH"
        exit 1
    }
    
    $pythonVersion = python --version
    Write-Success "Found Python: $pythonVersion"
    
    # Check pip
    if (-not (Test-Command "pip")) {
        Write-Error "pip is not available"
        Write-Host "Please ensure pip is installed with Python"
        exit 1
    }
    
    Write-Success "Found pip"
    
    # Check uvx (recommended) and pip (fallback)
    $hasUvx = Test-Command "uvx"
    $hasPip = Test-Command "pip"
    
    if ($hasUvx) {
        Write-Success "Found uvx (recommended)"
    } elseif ($hasPip) {
        Write-Success "Found pip (will use as fallback)"
        Write-Warning "uvx not found. Consider installing uv for better package management: pip install uv"
    } else {
        Write-Error "Neither uvx nor pip found. Please install Python with pip first."
        exit 1
    }
    
    # Create installation directory
    Write-Step "Setting up installation directory..."
    
    $fullInstallPath = Resolve-Path $InstallPath -ErrorAction SilentlyContinue
    if (-not $fullInstallPath) {
        $fullInstallPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($InstallPath)
    }
    
    if (Test-Path $fullInstallPath) {
        if ($Force) {
            Write-Warning "Removing existing installation at $fullInstallPath"
            Remove-Item $fullInstallPath -Recurse -Force
        } else {
            Write-Warning "Installation directory already exists: $fullInstallPath"
            $response = Read-Host "Do you want to continue? This may update the existing installation. (y/N)"
            if ($response -notmatch '^[Yy]') {
                Write-Host "Setup cancelled."
                exit 0
            }
        }
    }
    
    New-Item -ItemType Directory -Path $fullInstallPath -Force | Out-Null
    Write-Success "Created directory: $fullInstallPath"
    
    # Set working directory
    Push-Location $fullInstallPath
    
    try {
        # Install excel-mcp-server
        Write-Step "Installing excel-mcp-server..."
        
        if ($hasUvx) {
            Write-Host "Using uvx for installation..."
            try {
                if ($Verbose) {
                    uvx --python python excel-mcp-server --help
                } else {
                    uvx --python python excel-mcp-server --help | Out-Null
                }
                Write-Success "excel-mcp-server installed via uvx"
            }
            catch {
                Write-Warning "uvx failed, falling back to pip installation"
                $hasUvx = $false
            }
        }
        
        if (-not $hasUvx -and $hasPip) {
            Write-Host "Using pip for installation..."
            
            try {
                # Try global installation first
                Write-Host "Installing excel-mcp-server globally..."
                pip install excel-mcp-server
                
                # Test the installation
                python -m excel_mcp_server --help | Out-Null
                Write-Success "excel-mcp-server installed globally via pip"
            }
            catch {
                Write-Host "Global installation failed, creating virtual environment..."
                
                # Create virtual environment as fallback
                python -m venv venv
                
                # Activate virtual environment
                $activateScript = ".\venv\Scripts\Activate.ps1"
                if (Test-Path $activateScript) {
                    & $activateScript
                    
                    # Install package in venv
                    Write-Host "Installing excel-mcp-server in virtual environment..."
                    pip install excel-mcp-server
                    
                    Write-Success "excel-mcp-server installed in virtual environment"
                }
                else {
                    throw "Failed to create or activate virtual environment"
                }
            }
        }
        
        # Test installation
        Write-Step "Testing installation..."
        
        if ($hasUvx) {
            $testResult = uvx excel-mcp-server stdio --help 2>&1
        } else {
            $testResult = & ".\venv\Scripts\python.exe" -m excel_mcp_server stdio --help 2>&1
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Installation test passed"
        } else {
            Write-Warning "Installation test had issues, but package appears to be installed"
            if ($Verbose) {
                Write-Host "Test output: $testResult"
            }
        }
        
        # Create sample Excel files directory
        Write-Step "Setting up sample environment..."
        
        New-Item -ItemType Directory -Path "sample_files" -Force | Out-Null
        
        # Create a sample Excel file (requires Excel or alternative)
        $sampleExcelContent = @"
This directory is for Excel files that the MCP server will work with.

Example usage:
1. Place Excel files in this directory
2. Use the application to interact with them via AI
3. The MCP server will handle Excel operations safely
"@
        
        Set-Content -Path "sample_files\README.txt" -Value $sampleExcelContent
        Write-Success "Created sample files directory"
        
        # Create configuration file
        $configContent = @{
            "mcp_server" = @{
                "command" = if ($hasUvx) { "uvx" } else { ".\venv\Scripts\python.exe" }
                "args" = if ($hasUvx) { @("excel-mcp-server", "stdio") } else { @("-m", "excel_mcp_server", "stdio") }
                "working_directory" = $fullInstallPath
                "timeout_seconds" = 30
            }
            "created" = Get-Date -Format "yyyy-MM-ddTHH:mm:ss"
            "version" = "1.0.0"
        } | ConvertTo-Json -Depth 3
        
        Set-Content -Path "mcp_config.json" -Value $configContent
        Write-Success "Created MCP configuration file"
        
    } finally {
        Pop-Location
    }
    
    # Final success message
    Write-ColoredOutput @"

╔══════════════════════════════════════════════════════════════╗
║                    Setup Completed! ✓                       ║  
╚══════════════════════════════════════════════════════════════╝

Installation Details:
  • Location: $fullInstallPath
  • Method: $(if ($hasUvx) { "uvx (recommended)" } else { "pip + virtual environment" })
  • Config: $fullInstallPath\mcp_config.json

Next Steps:
  1. Update appsettings.json with your Claude API key
  2. Set Mcp.WorkingDirectory to: $fullInstallPath
  3. Run the application and test Excel integration

Test Command:
  .\scripts\run_backend_check.ps1 -WorkingDirectory '$fullInstallPath'

"@ $Green

} catch {
    Write-Error "Setup failed: $($_.Exception.Message)"
    Write-Host "Stack Trace:" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
    exit 1
}