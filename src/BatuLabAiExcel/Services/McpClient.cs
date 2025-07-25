using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.IO;
using BatuLabAiExcel.Models;
using BatuLabAiExcel.Infrastructure;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Client for communicating with MCP server via JSON-RPC over stdio
/// </summary>
public class McpClient : IMcpClient
{
    private readonly AppConfiguration.McpSettings _settings;
    private readonly ProcessHelper _processHelper;
    private readonly ILogger<McpClient> _logger;

    private Process? _mcpProcess;
    private bool _isInitialized;
    private List<McpTool>? _availableTools;
    private int _requestId = 1;
    private readonly object _processLock = new();
    private int _restartAttempts = 0;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public McpClient(
        IOptions<AppConfiguration.McpSettings> settings,
        ProcessHelper processHelper,
        ILogger<McpClient> logger)
    {
        _settings = settings.Value;
        _processHelper = processHelper;
        _logger = logger;
    }

    public async Task<Result> EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        lock (_processLock)
        {
            if (_isInitialized && _mcpProcess?.HasExited == false)
            {
                return Result.Success();
            }
        }

        return await InitializeAsync(cancellationToken);
    }

    public async Task<Result<List<McpTool>>> GetAvailableToolsAsync(CancellationToken cancellationToken = default)
    {
        var initResult = await EnsureInitializedAsync(cancellationToken);
        if (!initResult.IsSuccess)
        {
            return Result<List<McpTool>>.Failure(initResult.Error ?? "Initialization failed");
        }

        if (_availableTools != null)
        {
            return Result<List<McpTool>>.Success(_availableTools);
        }

        try
        {
            var request = new McpRequest
            {
                Id = GetNextRequestId(),
                Method = "tools/list",
                Params = new { }
            };

            var response = await SendRequestAsync<McpListToolsResult>(request, cancellationToken);
            if (!response.IsSuccess)
            {
                return Result<List<McpTool>>.Failure(response.Error ?? "Failed to get tools");
            }

            _availableTools = response.Value?.Tools ?? new List<McpTool>();
            _logger.LogInformation("Retrieved {Count} tools from MCP server", _availableTools.Count);

            return Result<List<McpTool>>.Success(_availableTools);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available tools");
            return Result<List<McpTool>>.Failure($"Error getting tools: {ex.Message}");
        }
    }

    public async Task<Result<string>> CallToolAsync(string toolName, object? arguments, CancellationToken cancellationToken = default)
    {
        var initResult = await EnsureInitializedAsync(cancellationToken);
        if (!initResult.IsSuccess)
        {
            return Result<string>.Failure(initResult.Error ?? "Initialization failed");
        }

        try
        {
            var request = new McpRequest
            {
                Id = GetNextRequestId(),
                Method = "tools/call",
                Params = new McpCallToolParams
                {
                    Name = toolName,
                    Arguments = arguments
                }
            };

            _logger.LogDebug("Calling MCP tool: {ToolName} with arguments: {Arguments}", 
                toolName, JsonSerializer.Serialize(arguments, JsonOptions));

            var response = await SendRequestAsync<McpCallToolResult>(request, cancellationToken);
            if (!response.IsSuccess)
            {
                _logger.LogError("MCP tool call failed: {Error}", response.Error);
                return Result<string>.Failure(response.Error ?? "Tool call failed");
            }

            var result = response.Value;
            if (result?.IsError == true)
            {
                var errorContent = result.Content?.FirstOrDefault()?.Text ?? "Unknown error";
                _logger.LogError("MCP tool returned error: {Error}", errorContent);
                return Result<string>.Failure($"Tool error: {errorContent}");
            }

            var resultText = result?.Content?.FirstOrDefault()?.Text ?? "No result";
            _logger.LogDebug("MCP tool result: {Result}", resultText);

            return Result<string>.Success(resultText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling MCP tool {ToolName}", toolName);
            return Result<string>.Failure($"Error calling tool: {ex.Message}");
        }
    }

    public async Task<Result> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        lock (_processLock)
        {
            if (_mcpProcess?.HasExited != false)
            {
                return Result.Failure("MCP process is not running");
            }
        }

        try
        {
            // Try to get tools as a health check
            var toolsResult = await GetAvailableToolsAsync(cancellationToken);
            return toolsResult.IsSuccess 
                ? Result.Success() 
                : Result.Failure($"Health check failed: {toolsResult.Error}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"Health check error: {ex.Message}");
        }
    }

    private async Task<Result> InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            lock (_processLock)
            {
                _isInitialized = false;
                _availableTools = null;
                
                // Clean up existing process
                if (_mcpProcess != null)
                {
                    _processHelper.SafeKillProcess(_mcpProcess);
                    _mcpProcess = null;
                }
            }

            // Start MCP server process
            var processResult = await StartMcpProcessAsync(cancellationToken);
            if (!processResult.IsSuccess)
            {
                return processResult;
            }

            // Perform MCP handshake
            var handshakeResult = await PerformHandshakeAsync(cancellationToken);
            if (!handshakeResult.IsSuccess)
            {
                return handshakeResult;
            }

            lock (_processLock)
            {
                _isInitialized = true;
                _restartAttempts = 0;
            }

            _logger.LogInformation("MCP client initialized successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MCP client");
            return Result.Failure($"Initialization error: {ex.Message}");
        }
    }

    private async Task<Result> StartMcpProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check for configuration file first
            string? configuredCommand = null;
            if (_settings.UseConfigFile)
            {
                configuredCommand = await TryLoadFromConfigFile();
            }

            // Build list of scripts to try
            var scripts = new List<string>();
            
            if (!string.IsNullOrEmpty(configuredCommand))
            {
                scripts.Add(configuredCommand);
                _logger.LogInformation("Found configured MCP command: {Command}", configuredCommand);
            }
            
            // Add default scripts
            if (!string.IsNullOrEmpty(_settings.ServerScript))
                scripts.Add(_settings.ServerScript);
            
            if (!string.IsNullOrEmpty(_settings.ServerScriptFallback))
                scripts.Add(_settings.ServerScriptFallback);
                
            if (!string.IsNullOrEmpty(_settings.ServerScriptInstallFirst))
                scripts.Add(_settings.ServerScriptInstallFirst);

            Exception? lastException = null;

            foreach (var script in scripts)
            {
                try
                {
                    _logger.LogInformation("Attempting to start MCP server with: {Script}", script);
                    
                    var result = await TryStartWithScript(script, cancellationToken);
                    if (result.IsSuccess)
                    {
                        return result;
                    }
                    lastException = new Exception(result.Error);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to start MCP server with script: {Script}", script);
                    lastException = ex;
                }
            }

            // If all methods failed, try automatic installation
            if (_settings.AutoInstall)
            {
                _logger.LogInformation("Attempting automatic installation of excel-mcp-server");
                var installResult = await TryAutoInstall(cancellationToken);
                if (installResult.IsSuccess)
                {
                    var pythonExe = await FindPythonExecutableAsync();
                    return await TryStartWithScript($"{pythonExe} -m excel_mcp_server stdio", cancellationToken);
                }
            }

            return Result.Failure($"Failed to start MCP server with all methods. Last error: {lastException?.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting MCP server process");
            return Result.Failure($"Failed to start MCP server: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Try to load MCP server command from configuration file
    /// </summary>
    private async Task<string?> TryLoadFromConfigFile()
    {
        try
        {
            var configPath = Path.Combine(_settings.WorkingDirectory, "mcp_config.json");
            if (!File.Exists(configPath))
            {
                _logger.LogDebug("No MCP configuration file found at: {Path}", configPath);
                return null;
            }

            var configJson = await File.ReadAllTextAsync(configPath);
            using var doc = JsonDocument.Parse(configJson);
            
            if (doc.RootElement.TryGetProperty("mcp_server", out var mcpServer))
            {
                if (mcpServer.TryGetProperty("command", out var command) &&
                    mcpServer.TryGetProperty("args", out var args))
                {
                    var commandStr = command.GetString();
                    var argsList = args.EnumerateArray().Select(x => x.GetString()).Where(x => x != null);
                    
                    return $"{commandStr} {string.Join(" ", argsList)}";
                }
            }
            
            _logger.LogDebug("MCP configuration file found but no valid command found");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading MCP configuration file");
            return null;
        }
    }

    private async Task<Result> TryStartWithScript(string script, CancellationToken cancellationToken)
    {
        var serverCommand = script.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (serverCommand.Length == 0)
        {
            return Result.Failure("Server script is empty");
        }

        ProcessStartInfo startInfo;

        // Try to find the best Python executable
        string pythonExe = await FindPythonExecutableAsync();
        
        // Replace 'python' with the found executable in the script
        if (script.StartsWith("python "))
        {
            script = script.Replace("python ", $"{pythonExe} ");
            serverCommand = script.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        // Handle compound commands (with &&)
        if (script.Contains("&&"))
        {
            startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{script}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = _settings.WorkingDirectory
            };
        }
        else
        {
            startInfo = new ProcessStartInfo
            {
                FileName = serverCommand[0],
                Arguments = string.Join(" ", serverCommand.Skip(1)),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = _settings.WorkingDirectory
            };
        }

        // Ensure working directory exists
        if (!Directory.Exists(_settings.WorkingDirectory))
        {
            Directory.CreateDirectory(_settings.WorkingDirectory);
            _logger.LogInformation("Created working directory: {Directory}", _settings.WorkingDirectory);
        }

        _logger.LogInformation("Starting MCP server: {Command} {Arguments}", 
            startInfo.FileName, startInfo.Arguments);

        var process = new Process { StartInfo = startInfo };
        
        // Handle process exit
        process.EnableRaisingEvents = true;
        process.Exited += OnMcpProcessExited;

        if (!process.Start())
        {
            return Result.Failure("Failed to start MCP server process");
        }

        lock (_processLock)
        {
            _mcpProcess = process;
        }

        // Give the process a moment to start
        await Task.Delay(2000, cancellationToken);

        if (process.HasExited)
        {
            var errorOutput = await process.StandardError.ReadToEndAsync(cancellationToken);
            _logger.LogWarning("Process exited with output: {Output}", errorOutput);
            return Result.Failure($"MCP server process exited immediately: {errorOutput}");
        }

        _logger.LogInformation("MCP server process started with PID: {ProcessId}", process.Id);
        return Result.Success();
    }

    private async Task<Result> TryAutoInstall(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Installing excel-mcp-server via pip");
            
            string pythonExe = await FindPythonExecutableAsync();
            
            var installProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = "-m pip install excel-mcp-server",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            installProcess.Start();
            await installProcess.WaitForExitAsync(cancellationToken);

            if (installProcess.ExitCode == 0)
            {
                _logger.LogInformation("excel-mcp-server installed successfully");
                return Result.Success();
            }
            else
            {
                var error = await installProcess.StandardError.ReadToEndAsync(cancellationToken);
                _logger.LogError("Installation failed: {Error}", error);
                return Result.Failure($"Installation failed: {error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-installation");
            return Result.Failure($"Auto-installation error: {ex.Message}");
        }
    }

    /// <summary>
    /// Find the best available Python executable
    /// </summary>
    private async Task<string> FindPythonExecutableAsync()
    {
        var candidates = new[]
        {
            _settings.PythonPath, // User configured path
            "python",            // Default
            "python3",           // Linux/Mac style
            "py",                // Windows Python Launcher
            @"C:\Python312\python.exe",
            @"C:\Python311\python.exe", 
            @"C:\Python310\python.exe",
            @"C:\Program Files\Python312\python.exe",
            @"C:\Program Files\Python311\python.exe",
            @"C:\Program Files\Python310\python.exe",
            @"C:\Program Files (x86)\Python312\python.exe",
            @"C:\Program Files (x86)\Python311\python.exe",
            @"C:\Program Files (x86)\Python310\python.exe",
            @"C:\Users\" + Environment.UserName + @"\AppData\Local\Programs\Python\Python312\python.exe",
            @"C:\Users\" + Environment.UserName + @"\AppData\Local\Programs\Python\Python311\python.exe",
            @"C:\Users\" + Environment.UserName + @"\AppData\Local\Programs\Python\Python310\python.exe"
        };

        foreach (var candidate in candidates.Where(c => !string.IsNullOrEmpty(c)))
        {
            try
            {
                // First check if it's a direct file path
                if (File.Exists(candidate))
                {
                    _logger.LogDebug("Found Python executable at: {Path}", candidate);
                    return candidate;
                }

                // Then try to execute it to see if it's in PATH
                var testProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = candidate,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                testProcess.Start();
                await testProcess.WaitForExitAsync();

                if (testProcess.ExitCode == 0)
                {
                    var version = await testProcess.StandardOutput.ReadToEndAsync();
                    _logger.LogInformation("Found Python: {Version} at {Path}", version.Trim(), candidate);
                    return candidate;
                }
            }
            catch (Exception ex)
            {
                _logger.LogTrace(ex, "Python candidate {Candidate} not available", candidate);
            }
        }

        _logger.LogWarning("No Python executable found, falling back to 'python'");
        return "python"; // Fallback
    }

    private async Task<Result> PerformHandshakeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var initializeRequest = new McpRequest
            {
                Id = GetNextRequestId(),
                Method = "initialize",
                Params = new McpInitializeParams
                {
                    ProtocolVersion = "2024-11-05",
                    Capabilities = new McpCapabilities(),
                    ClientInfo = new McpClientInfo
                    {
                        Name = "Office Ai - Batu Lab.",
                        Version = "1.0.0"
                    }
                }
            };

            var initResponse = await SendRequestAsync<McpInitializeResult>(initializeRequest, cancellationToken);
            if (!initResponse.IsSuccess)
            {
                return Result.Failure($"Initialize failed: {initResponse.Error}");
            }

            _logger.LogInformation("MCP handshake completed with server: {ServerName} v{ServerVersion}",
                initResponse.Value?.ServerInfo?.Name ?? "Unknown",
                initResponse.Value?.ServerInfo?.Version ?? "Unknown");

            // Send initialized notification
            var initializedNotification = new McpRequest
            {
                Method = "notifications/initialized",
                Params = new { }
            };

            await SendNotificationAsync(initializedNotification, cancellationToken);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MCP handshake");
            return Result.Failure($"Handshake error: {ex.Message}");
        }
    }

    private async Task<Result<T>> SendRequestAsync<T>(McpRequest request, CancellationToken cancellationToken)
        where T : class
    {
        Process? process;
        lock (_processLock)
        {
            process = _mcpProcess;
        }

        if (process?.HasExited != false)
        {
            return Result<T>.Failure("MCP process is not running");
        }

        try
        {
            var json = JsonSerializer.Serialize(request, JsonOptions);
            _logger.LogTrace("Sending MCP request: {Request}", json);

            await process.StandardInput.WriteLineAsync(json.AsMemory(), cancellationToken);
            await process.StandardInput.FlushAsync();

            // Read response
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_settings.TimeoutSeconds));

            var responseLine = await process.StandardOutput.ReadLineAsync(cts.Token);
            if (string.IsNullOrEmpty(responseLine))
            {
                return Result<T>.Failure("No response from MCP server");
            }

            _logger.LogTrace("Received MCP response: {Response}", responseLine);

            var response = JsonSerializer.Deserialize<McpResponse>(responseLine, JsonOptions);
            if (response?.Error != null)
            {
                return Result<T>.Failure($"MCP error ({response.Error.Code}): {response.Error.Message}");
            }

            if (response?.Result == null)
            {
                return Result<T>.Failure("MCP response has no result");
            }

            var resultJson = JsonSerializer.Serialize(response.Result, JsonOptions);
            var typedResult = JsonSerializer.Deserialize<T>(resultJson, JsonOptions);

            if (typedResult == null)
            {
                return Result<T>.Failure("Failed to deserialize MCP response");
            }

            return Result<T>.Success(typedResult);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<T>.Failure("Request was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending MCP request");
            return Result<T>.Failure($"Request error: {ex.Message}");
        }
    }

    private async Task SendNotificationAsync(McpRequest notification, CancellationToken cancellationToken)
    {
        Process? process;
        lock (_processLock)
        {
            process = _mcpProcess;
        }

        if (process?.HasExited != false)
        {
            return;
        }

        try
        {
            // Notifications don't have an ID
            notification.Id = null!;
            
            var json = JsonSerializer.Serialize(notification, JsonOptions);
            _logger.LogTrace("Sending MCP notification: {Notification}", json);

            await process.StandardInput.WriteLineAsync(json.AsMemory(), cancellationToken);
            await process.StandardInput.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending MCP notification");
        }
    }

    private void OnMcpProcessExited(object? sender, EventArgs e)
    {
        lock (_processLock)
        {
            _isInitialized = false;
            _availableTools = null;
        }

        _logger.LogWarning("MCP server process exited");

        if (_settings.RestartOnFailure && _restartAttempts < _settings.MaxRestartAttempts)
        {
            _restartAttempts++;
            _logger.LogInformation("Attempting to restart MCP server (attempt {Attempt}/{Max})", 
                _restartAttempts, _settings.MaxRestartAttempts);

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(2000); // Wait 2 seconds before restart
                    await InitializeAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error restarting MCP server");
                }
            });
        }
    }

    private string GetNextRequestId()
    {
        return Interlocked.Increment(ref _requestId).ToString();
    }

    public void Dispose()
    {
        lock (_processLock)
        {
            if (_mcpProcess != null)
            {
                _processHelper.SafeKillProcess(_mcpProcess);
                _mcpProcess = null;
            }
            
            _isInitialized = false;
            _availableTools = null;
        }
    }
}