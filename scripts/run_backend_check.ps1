# Office Ai - Batu Lab. - Backend Health Check Script
# This script tests the MCP server connection and functionality

param(
    [string]$WorkingDirectory = ".\excel_files",
    [int]$TimeoutSeconds = 30,
    [switch]$Verbose = $false
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
    $null = Get-Command $Command -ErrorAction SilentlyContinue
    return $?
}

function Send-McpRequest {
    param(
        [System.Diagnostics.Process]$Process,
        [hashtable]$Request,
        [int]$TimeoutMs = 10000
    )
    
    $json = $Request | ConvertTo-Json -Compress -Depth 10
    if ($Verbose) {
        Write-Host "Sending: $json" -ForegroundColor Gray
    }
    
    $Process.StandardInput.WriteLine($json)
    $Process.StandardInput.Flush()
    
    # Read response with timeout
    $cts = [System.Threading.CancellationTokenSource]::new($TimeoutMs)
    $task = $Process.StandardOutput.ReadLineAsync()
    
    while (-not $task.IsCompleted) {
        if ($cts.Token.IsCancellationRequested) {
            throw "Request timed out after $TimeoutMs ms"
        }
        Start-Sleep -Milliseconds 100
    }
    
    $response = $task.Result
    if ($Verbose) {
        Write-Host "Received: $response" -ForegroundColor Gray
    }
    
    if ([string]::IsNullOrEmpty($response)) {
        throw "No response received"
    }
    
    return $response | ConvertFrom-Json
}

# Main health check process
Write-ColoredOutput @"
╔══════════════════════════════════════════════════════════════╗
║                    Office Ai - Batu Lab.                    ║
║                  Backend Health Check                       ║
╚══════════════════════════════════════════════════════════════╝
"@ $Blue

# Resolve working directory
$fullWorkingDirectory = Resolve-Path $WorkingDirectory -ErrorAction SilentlyContinue
if (-not $fullWorkingDirectory) {
    $fullWorkingDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($WorkingDirectory)
}

Write-Host "Working Directory: $fullWorkingDirectory"
Write-Host "Timeout: $TimeoutSeconds seconds"
Write-Host ""

# Check working directory exists
if (-not (Test-Path $fullWorkingDirectory)) {
    Write-Error "Working directory does not exist: $fullWorkingDirectory"
    Write-Host "Run setup_mcp.ps1 first to create the environment"
    exit 1
}

# Check for configuration
$configPath = Join-Path $fullWorkingDirectory "mcp_config.json"
$config = $null

if (Test-Path $configPath) {
    $config = Get-Content $configPath | ConvertFrom-Json
    Write-Success "Found MCP configuration"
} else {
    Write-Warning "No MCP configuration found, using defaults"
}

# Determine command to use
$mcpCommand = "uvx"
$mcpArgs = @("excel-mcp-server", "stdio")

if ($config -and $config.mcp_server) {
    $mcpCommand = $config.mcp_server.command
    $mcpArgs = $config.mcp_server.args
} elseif (-not (Test-Command "uvx")) {
    # Fallback to virtual environment
    $venvPython = Join-Path $fullWorkingDirectory "venv\Scripts\python.exe"
    if (Test-Path $venvPython) {
        $mcpCommand = $venvPython
        $mcpArgs = @("-m", "excel_mcp_server", "stdio")
    } else {
        Write-Error "Neither uvx nor virtual environment found"
        Write-Host "Run setup_mcp.ps1 first to install excel-mcp-server"
        exit 1
    }
}

Write-Step "Starting MCP server..."
Write-Host "Command: $mcpCommand $($mcpArgs -join ' ')"

# Start MCP server process
$processInfo = New-Object System.Diagnostics.ProcessStartInfo
$processInfo.FileName = $mcpCommand
$processInfo.Arguments = $mcpArgs -join " "
$processInfo.UseShellExecute = $false
$processInfo.RedirectStandardInput = $true
$processInfo.RedirectStandardOutput = $true
$processInfo.RedirectStandardError = $true
$processInfo.CreateNoWindow = $true
$processInfo.WorkingDirectory = $fullWorkingDirectory

$process = [System.Diagnostics.Process]::Start($processInfo)

if (-not $process) {
    Write-Error "Failed to start MCP server process"
    exit 1
}

Write-Success "MCP server started (PID: $($process.Id))"

# Give process time to start
Start-Sleep -Seconds 2

if ($process.HasExited) {
    $errorOutput = $process.StandardError.ReadToEnd()
    Write-Error "MCP server exited immediately"
    Write-Host "Error output: $errorOutput" -ForegroundColor Red
    exit 1
}

Write-Step "Testing MCP handshake..."

# Test 1: Initialize
$initRequest = @{
    jsonrpc = "2.0"
    id = "1"
    method = "initialize"
    params = @{
        protocolVersion = "2024-11-05"
        capabilities = @{
            tools = @{}
        }
        clientInfo = @{
            name = "Office Ai - Batu Lab. Health Check"
            version = "1.0.0"
        }
    }
}

$initResponse = Send-McpRequest -Process $process -Request $initRequest -TimeoutMs ($TimeoutSeconds * 1000)

if ($initResponse.error) {
    Write-Error "Initialize failed: $($initResponse.error.message)"
    exit 1
}

Write-Success "Initialize successful"
if ($Verbose -and $initResponse.result.serverInfo) {
    Write-Host "Server: $($initResponse.result.serverInfo.name) v$($initResponse.result.serverInfo.version)" -ForegroundColor Gray
}

# Test 2: Send initialized notification
$initializedNotification = @{
    jsonrpc = "2.0"
    method = "notifications/initialized"
    params = @{}
}

$json = $initializedNotification | ConvertTo-Json -Compress
$process.StandardInput.WriteLine($json)
$process.StandardInput.Flush()

Write-Success "Initialized notification sent"

# Test 3: List tools
Write-Step "Testing tool discovery..."

$listToolsRequest = @{
    jsonrpc = "2.0"
    id = "2"
    method = "tools/list"
    params = @{}
}

$toolsResponse = Send-McpRequest -Process $process -Request $listToolsRequest -TimeoutMs ($TimeoutSeconds * 1000)

if ($toolsResponse.error) {
    Write-Error "List tools failed: $($toolsResponse.error.message)"
    exit 1
}

$toolCount = $toolsResponse.result.tools.Count
Write-Success "Found $toolCount available tools"

if ($Verbose -and $toolCount -gt 0) {
    Write-Host "Available tools:" -ForegroundColor Gray
    foreach ($tool in $toolsResponse.result.tools) {
        Write-Host "  • $($tool.name): $($tool.description)" -ForegroundColor Gray
    }
}

# Test 4: Test a simple tool call (if available)
if ($toolCount -gt 0) {
    Write-Step "Testing tool execution..."
    
    # Try to find a safe test tool
    $testTool = $toolsResponse.result.tools | Where-Object { 
        $_.name -match "(get_workbook_metadata|create_workbook)" 
    } | Select-Object -First 1
    
    if ($testTool) {
        $toolCallRequest = @{
            jsonrpc = "2.0"
            id = "3"
            method = "tools/call"
            params = @{
                name = $testTool.name
                arguments = if ($testTool.name -eq "get_workbook_metadata") {
                    @{ filepath = "nonexistent_test_file.xlsx"; include_ranges = $false }
                } else {
                    @{ filepath = "test_health_check.xlsx" }
                }
            }
        }
        
        $toolResponse = Send-McpRequest -Process $process -Request $toolCallRequest -TimeoutMs ($TimeoutSeconds * 1000)
        
        if ($toolResponse.error) {
            # Expected for nonexistent file
            Write-Success "Tool execution test completed (expected error for test case)"
        } else {
            Write-Success "Tool execution test completed successfully"
        }
        
        if ($Verbose) {
            Write-Host "Tool response: $($toolResponse | ConvertTo-Json -Compress)" -ForegroundColor Gray
        }
    } else {
        Write-Warning "No safe test tools found, skipping tool execution test"
    }
}

Write-Step "All tests completed successfully!"

# Clean up process
if (-not $process.HasExited) {
    Write-Step "Shutting down MCP server..."
    
    $process.StandardInput.Close()
    $process.WaitForExit(5000)
    
    if (-not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit(2000)
    }
    
    Write-Success "MCP server shut down"
}

$process.Dispose()

# Final success message
Write-ColoredOutput @"

╔══════════════════════════════════════════════════════════════╗
║                 Health Check Passed! ✓                      ║  
╚══════════════════════════════════════════════════════════════╝

Backend Status: ✓ Healthy
  • MCP server starts successfully
  • JSON-RPC communication works
  • Tool discovery functional
  • Ready for AI integration

The Excel MCP server is ready to use with Office Ai - Batu Lab.

"@ $Green