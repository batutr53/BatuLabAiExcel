# Office Ai - Batu Lab. - Excel File Management Script
# This script helps users easily add Excel files to the working directory

param(
    [string]$FilePath,
    [string]$WorkingDirectory = ".\excel_files",
    [switch]$Open = $false
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

try {
    Write-ColoredOutput @"
╔══════════════════════════════════════════════════════════════╗
║                    Office Ai - Batu Lab.                    ║
║                 Excel File Manager                          ║
╚══════════════════════════════════════════════════════════════╝
"@ $Blue

    # Create working directory if it doesn't exist
    $fullWorkingDirectory = Resolve-Path $WorkingDirectory -ErrorAction SilentlyContinue
    if (-not $fullWorkingDirectory) {
        $fullWorkingDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($WorkingDirectory)
        New-Item -ItemType Directory -Path $fullWorkingDirectory -Force | Out-Null
        Write-Success "Created working directory: $fullWorkingDirectory"
    }

    if ($FilePath) {
        # Copy specific file
        if (-not (Test-Path $FilePath)) {
            Write-Error "File not found: $FilePath"
            exit 1
        }

        $fileName = Split-Path $FilePath -Leaf
        $targetPath = Join-Path $fullWorkingDirectory $fileName

        Copy-Item $FilePath $targetPath -Force
        Write-Success "Excel file copied to working directory:"
        Write-Host "  📁 Source: $FilePath"
        Write-Host "  📂 Target: $targetPath"
        
        if ($Open) {
            Write-Info "Opening application..."
            Start-Process "dotnet" -ArgumentList "run --project src\BatuLabAiExcel" -NoNewWindow
        }
    }
    else {
        # Interactive file selection
        Write-Info "Select Excel files to add to the working directory..."
        
        Add-Type -AssemblyName System.Windows.Forms
        $openFileDialog = New-Object System.Windows.Forms.OpenFileDialog
        $openFileDialog.Title = "Select Excel Files"
        $openFileDialog.Filter = "Excel Files (*.xlsx;*.xls)|*.xlsx;*.xls|All Files (*.*)|*.*"
        $openFileDialog.Multiselect = $true
        $openFileDialog.InitialDirectory = [Environment]::GetFolderPath("MyDocuments")

        if ($openFileDialog.ShowDialog() -eq [System.Windows.Forms.DialogResult]::OK) {
            foreach ($file in $openFileDialog.FileNames) {
                $fileName = Split-Path $file -Leaf
                $targetPath = Join-Path $fullWorkingDirectory $fileName
                
                Copy-Item $file $targetPath -Force
                Write-Success "Copied: $fileName"
            }
            
            Write-Info "All files copied successfully!"
            
            $response = Read-Host "Would you like to open the application now? (y/N)"
            if ($response -match '^[Yy]') {
                Write-Info "Starting Office Ai - Batu Lab..."
                Start-Process "dotnet" -ArgumentList "run --project src\BatuLabAiExcel" -NoNewWindow
            }
        }
        else {
            Write-Info "No files selected."
        }
    }

    # Show current files in working directory
    Write-Info "Current Excel files in working directory:"
    $excelFiles = Get-ChildItem $fullWorkingDirectory -Filter "*.xlsx" -ErrorAction SilentlyContinue
    $excelFiles += Get-ChildItem $fullWorkingDirectory -Filter "*.xls" -ErrorAction SilentlyContinue
    
    if ($excelFiles.Count -gt 0) {
        foreach ($file in $excelFiles) {
            Write-Host "  📊 $($file.Name) ($([math]::Round($file.Length / 1KB, 1)) KB)"
        }
    }
    else {
        Write-Host "  (No Excel files found)"
    }

    Write-ColoredOutput @"

💡 Usage Tips:
  • Run without parameters for interactive file selection
  • Use -FilePath to copy a specific file
  • Use -Open to automatically start the application
  • Drag & drop Excel files directly into the application window

Examples:
  .\scripts\add_excel_file.ps1
  .\scripts\add_excel_file.ps1 -FilePath "C:\MyData\sales.xlsx" -Open
  .\scripts\add_excel_file.ps1 -WorkingDirectory "C:\CustomPath"

"@ $Blue

} catch {
    Write-Error "Script failed: $($_.Exception.Message)"
    exit 1
}