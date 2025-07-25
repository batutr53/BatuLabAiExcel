# Office Ai - Batu Lab. - Comprehensive Python and MCP Setup Script
# This script automatically detects, installs, and configures Python and excel-mcp-server

param(
    [string]$WorkingDirectory = ".\excel_files",
    [switch]$ForceReinstall = $false,
    [switch]$Verbose = $false,
    [switch]$InstallPython = $false
)

$ErrorActionPreference = "Stop"

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

function Write-Info {
    param($Message)
    Write-ColoredOutput "ℹ $Message" $Cyan
}

function Test-PythonInstallation {
    param($PythonPath)
    
    try {
        $process = Start-Process -FilePath $PythonPath -ArgumentList "--version" -NoNewWindow -Wait -PassThru -RedirectStandardOutput "temp_python_version.txt" -RedirectStandardError "temp_python_error.txt"
        
        if ($process.ExitCode -eq 0 -and (Test-Path "temp_python_version.txt")) {
            $version = Get-Content "temp_python_version.txt" -Raw
            Remove-Item "temp_python_version.txt" -ErrorAction SilentlyContinue
            Remove-Item "temp_python_error.txt" -ErrorAction SilentlyContinue
            return @{ Success = $true; Version = $version.Trim() }
        }
        
        Remove-Item "temp_python_version.txt" -ErrorAction SilentlyContinue
        Remove-Item "temp_python_error.txt" -ErrorAction SilentlyContinue
        return @{ Success = $false; Version = $null }
    }
    catch {
        return @{ Success = $false; Version = $null }
    }
}

function Find-PythonInstallation {
    Write-Step "Searching for Python installations..."
    
    $candidates = @(
        "python",
        "python3",
        "py",
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
        "$env:USERPROFILE\AppData\Local\Programs\Python\Python310\python.exe"
    )
    
    $found = @()
    
    foreach ($candidate in $candidates) {
        if ($Verbose) {
            Write-Host "  Checking: $candidate" -ForegroundColor Gray
        }
        
        # Check if it's a direct file path
        if (Test-Path $candidate -PathType Leaf) {
            $result = Test-PythonInstallation $candidate
            if ($result.Success) {
                $found += @{ Path = $candidate; Version = $result.Version }
                Write-Success "Found: $candidate ($($result.Version))"
            }
            continue
        }
        
        # Check if it's in PATH
        try {
            $result = Test-PythonInstallation $candidate
            if ($result.Success) {
                $found += @{ Path = $candidate; Version = $result.Version }
                Write-Success "Found: $candidate ($($result.Version))"
            }
        }
        catch {
            # Ignore PATH lookup failures
        }
    }
    
    return $found
}

function Install-PythonFromStore {
    Write-Step "Installing Python from Microsoft Store..."
    
    try {
        # Try to install Python from Microsoft Store
        Start-Process "ms-windows-store://pdp/?productid=9PJPW5LDXLZ5" -Wait
        Write-Info "Microsoft Store opened. Please install Python and then re-run this script."
        Write-Info "Alternatively, download Python from https://python.org/downloads/"
        return $false
    }
    catch {
        Write-Warning "Could not open Microsoft Store. Please install Python manually from https://python.org/downloads/"
        return $false
    }
}

function Install-PythonManually {
    Write-Step "Downloading and installing Python..."
    
    $pythonVersion = "3.12.0"
    $pythonUrl = "https://www.python.org/ftp/python/$pythonVersion/python-$pythonVersion-amd64.exe"
    $pythonInstaller = "python-installer.exe"
    
    try {
        Write-Info "Downloading Python $pythonVersion..."
        Invoke-WebRequest -Uri $pythonUrl -OutFile $pythonInstaller -UseBasicParsing
        
        Write-Info "Installing Python (this may take a few minutes)..."
        $installArgs = @(
            "/quiet",
            "InstallAllUsers=0",
            "PrependPath=1",
            "Include_test=0",
            "Include_pip=1"
        )
        
        $process = Start-Process -FilePath $pythonInstaller -ArgumentList $installArgs -Wait -PassThru
        
        Remove-Item $pythonInstaller -ErrorAction SilentlyContinue
        
        if ($process.ExitCode -eq 0) {
            Write-Success "Python installed successfully!"
            
            # Refresh PATH
            $env:PATH = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
            
            return $true
        }
        else {
            Write-Error "Python installation failed with exit code: $($process.ExitCode)"
            return $false
        }
    }
    catch {
        Write-Error "Error downloading/installing Python: $($_.Exception.Message)"
        Remove-Item $pythonInstaller -ErrorAction SilentlyContinue
        return $false
    }
}

function Install-ExcelMcpServer {
    param($PythonPath)
    
    Write-Step "Installing excel-mcp-server..."
    
    try {
        $installArgs = @("-m", "pip", "install", "excel-mcp-server")
        
        if ($ForceReinstall) {
            $installArgs += @("--force-reinstall")
        }
        
        Write-Info "Running: $PythonPath $($installArgs -join ' ')"
        
        $process = Start-Process -FilePath $PythonPath -ArgumentList $installArgs -NoNewWindow -Wait -PassThru
        
        if ($process.ExitCode -eq 0) {
            Write-Success "excel-mcp-server installed successfully!"
            return $true
        }
        else {
            Write-Error "Failed to install excel-mcp-server (exit code: $($process.ExitCode))"
            return $false
        }
    }
    catch {
        Write-Error "Error installing excel-mcp-server: $($_.Exception.Message)"
        return $false
    }
}

function Test-ExcelMcpServer {
    param($PythonPath)
    
    Write-Step "Testing excel-mcp-server installation..."
    
    try {
        $testArgs = @("-m", "excel_mcp_server", "--help")
        $process = Start-Process -FilePath $PythonPath -ArgumentList $testArgs -NoNewWindow -Wait -PassThru -RedirectStandardOutput "temp_mcp_test.txt"
        
        if ($process.ExitCode -eq 0) {
            Write-Success "excel-mcp-server is working correctly!"
            Remove-Item "temp_mcp_test.txt" -ErrorAction SilentlyContinue
            return $true
        }
        else {
            Write-Error "excel-mcp-server test failed"
            Remove-Item "temp_mcp_test.txt" -ErrorAction SilentlyContinue
            return $false
        }
    }
    catch {
        Write-Error "Error testing excel-mcp-server: $($_.Exception.Message)"
        return $false
    }
}

function Create-McpConfiguration {
    param($PythonPath, $WorkingDirectory)
    
    Write-Step "Creating MCP configuration..."
    
    $config = @{
        python_path = $PythonPath
        working_directory = $WorkingDirectory
        mcp_server = @{
            command = $PythonPath
            args = @("-m", "excel_mcp_server", "stdio")
        }
        created = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        version = "1.0.0"
    }
    
    $configPath = Join-Path $WorkingDirectory "mcp_config.json"
    $config | ConvertTo-Json -Depth 10 | Set-Content $configPath -Encoding UTF8
    
    Write-Success "Configuration saved to: $configPath"
}

# Main installation process
try {
    Write-ColoredOutput @"
╔══════════════════════════════════════════════════════════════╗
║                    Office Ai - Batu Lab.                    ║
║              Python & MCP Server Setup                      ║
╚══════════════════════════════════════════════════════════════╝
"@ $Blue

    Write-Info "Working Directory: $WorkingDirectory"
    Write-Info "Force Reinstall: $ForceReinstall"
    Write-Info "Install Python if missing: $InstallPython"
    Write-Host ""

    # Create working directory
    if (-not (Test-Path $WorkingDirectory)) {
        New-Item -ItemType Directory -Path $WorkingDirectory -Force | Out-Null
        Write-Success "Created working directory: $WorkingDirectory"
    }

    # Find Python installations
    $pythonInstalls = Find-PythonInstallation
    
    if ($pythonInstalls.Count -eq 0) {
        Write-Warning "No Python installation found!"
        
        if ($InstallPython) {
            Write-Info "Attempting to install Python..."
            $installed = Install-PythonManually
            
            if ($installed) {
                Start-Sleep -Seconds 3  # Give system time to refresh PATH
                $pythonInstalls = Find-PythonInstallation
            }
        }
        
        if ($pythonInstalls.Count -eq 0) {
            Write-Error @"
Python is required but not found. Please either:
1. Install Python from https://python.org/downloads/
2. Run this script with -InstallPython flag
3. Use Microsoft Store to install Python

After installing Python, please re-run this script.
"@
            exit 1
        }
    }

    # Select the best Python installation (prefer latest version)
    $selectedPython = $pythonInstalls | Sort-Object Version -Descending | Select-Object -First 1
    Write-Info "Selected Python: $($selectedPython.Path) ($($selectedPython.Version))"

    # Install excel-mcp-server
    $mcpInstalled = Install-ExcelMcpServer $selectedPython.Path
    
    if (-not $mcpInstalled) {
        Write-Error "Failed to install excel-mcp-server. Please check your Python installation and internet connection."
        exit 1
    }

    # Test the installation
    $mcpWorking = Test-ExcelMcpServer $selectedPython.Path
    
    if (-not $mcpWorking) {
        Write-Error "excel-mcp-server installation test failed."
        exit 1
    }

    # Create configuration
    Create-McpConfiguration $selectedPython.Path (Resolve-Path $WorkingDirectory).Path

    # Final success message
    Write-ColoredOutput @"

╔══════════════════════════════════════════════════════════════╗
║                 Setup Completed Successfully! ✓             ║  
╚══════════════════════════════════════════════════════════════╝

Setup Summary:
  ✓ Python: $($selectedPython.Path) ($($selectedPython.Version))
  ✓ excel-mcp-server: Installed and tested
  ✓ Working directory: $WorkingDirectory
  ✓ Configuration: Created

Next Steps:
1. Launch Office Ai - Batu Lab. application
2. The application will automatically use the configured Python installation
3. Start chatting with your Excel files!

Troubleshooting:
- If you encounter issues, run scripts\run_backend_check.ps1 to test the setup
- For verbose output, add -Verbose flag to any script
- Configuration is stored in $WorkingDirectory\mcp_config.json

"@ $Green

    Write-Info "Setup completed successfully! You can now use Office Ai - Batu Lab."

} catch {
    Write-Error "Setup failed: $($_.Exception.Message)"
    if ($Verbose) {
        Write-Host "Stack Trace:" -ForegroundColor Red
        Write-Host $_.ScriptStackTrace -ForegroundColor Red
    }
    
    Write-ColoredOutput @"

╔══════════════════════════════════════════════════════════════╗
║                 Setup Failed! ✗                             ║  
╚══════════════════════════════════════════════════════════════╝

Troubleshooting:
1. Run as Administrator if permission issues occur
2. Check internet connection for downloads
3. Verify Python installation manually
4. Run with -Verbose for detailed output
5. Try -ForceReinstall to reinstall packages

"@ $Red
    
    exit 1
}