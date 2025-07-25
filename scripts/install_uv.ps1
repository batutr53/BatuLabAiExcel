# Office Ai - Batu Lab. - UV Package Manager Installation Script
# This script installs UV package manager which provides uvx command

param(
    [switch]$Force = $false
)

$ErrorActionPreference = "Stop"

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

function Write-Success {
    param($Message)
    Write-ColoredOutput "✅ $Message" $Green
}

function Write-Error {
    param($Message)
    Write-ColoredOutput "❌ $Message" $Red
}

function Write-Info {
    param($Message)
    Write-ColoredOutput "ℹ️  $Message" $Blue
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

try {
    Write-ColoredOutput @"
╔══════════════════════════════════════════════════════════════╗
║                    Office Ai - Batu Lab.                    ║
║                 UV Package Manager Setup                    ║
╚══════════════════════════════════════════════════════════════╝
"@ $Blue

    # Check if uvx is already available
    if (Test-Command "uvx" -and -not $Force) {
        Write-Success "uvx is already installed and available!"
        uvx --version
        Write-Info "Use -Force to reinstall if needed."
        exit 0
    }

    Write-Info "Installing UV package manager..."

    # Method 1: Try pip install (most reliable)
    if (Test-Command "pip") {
        Write-Info "Installing UV via pip..."
        try {
            pip install uv
            Write-Success "UV installed via pip successfully!"
            
            # Verify installation
            if (Test-Command "uvx") {
                Write-Success "uvx command is now available!"
                uvx --version
            } else {
                Write-Error "uvx command not found after pip installation. Trying alternative method..."
                throw "uvx not available"
            }
        }
        catch {
            Write-Error "Pip installation failed, trying alternative method..."
        }
    }

    # Method 2: Try direct download if pip failed or not available
    if (-not (Test-Command "uvx")) {
        Write-Info "Trying direct installation method..."
        
        # Download and install UV using PowerShell
        $uvInstallScript = "https://astral.sh/uv/install.ps1"
        Write-Info "Downloading UV installer from $uvInstallScript"
        
        try {
            Invoke-RestMethod $uvInstallScript | Invoke-Expression
            Write-Success "UV installed via direct download!"
            
            # Add to PATH if needed
            $uvPath = "$env:USERPROFILE\.cargo\bin"
            if (Test-Path $uvPath) {
                $currentPath = [Environment]::GetEnvironmentVariable("PATH", "User")
                if ($currentPath -notlike "*$uvPath*") {
                    [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$uvPath", "User")
                    Write-Success "Added UV to user PATH: $uvPath"
                    Write-Info "Please restart your terminal or IDE to use uvx command."
                }
            }
        }
        catch {
            Write-Error "Direct installation also failed: $($_.Exception.Message)"
        }
    }

    # Method 3: Manual instructions if both methods failed
    if (-not (Test-Command "uvx")) {
        Write-ColoredOutput @"

❌ Automatic installation failed. Please try manual installation:

Option 1 - Using pip:
  pip install uv

Option 2 - Using winget:
  winget install astral-sh.uv

Option 3 - Download from GitHub:
  https://github.com/astral-sh/uv/releases

After installation, restart your terminal and try again.

"@ $Red
        exit 1
    }

    # Test the installation
    Write-Info "Testing UV installation..."
    try {
        $uvVersion = uvx --version
        Write-Success "UV/UVX is working correctly: $uvVersion"
        
        # Test with excel-mcp-server
        Write-Info "Testing excel-mcp-server availability..."
        $testResult = uvx excel-mcp-server --help 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Success "excel-mcp-server is available via uvx!"
        } else {
            Write-Info "excel-mcp-server will be downloaded on first use."
        }
    }
    catch {
        Write-Error "UV/UVX test failed: $($_.Exception.Message)"
        exit 1
    }

    Write-ColoredOutput @"

✅ UV/UVX Installation Complete!

Next steps:
1. Restart your terminal/IDE if needed
2. Run the application: dotnet run --project src\BatuLabAiExcel
3. The MCP server will be automatically set up on first use

If you still get errors, try:
  .\scripts\setup_mcp.ps1 -Force

"@ $Green

} catch {
    Write-Error "Installation failed: $($_.Exception.Message)"
    Write-ColoredOutput @"

Manual Installation Required:
1. Install Python if not already installed
2. Run: pip install uv
3. Or download from: https://github.com/astral-sh/uv/releases
4. Add to PATH and restart terminal

"@ $Yellow
    exit 1
}