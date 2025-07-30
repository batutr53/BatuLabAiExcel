using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.IO;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Service for interacting with Gemini CLI
/// </summary>
public class GeminiCliService : IAiService
{
    private readonly AppConfiguration.GeminiCliSettings _settings;
    private readonly ILogger<GeminiCliService> _logger;

    public string ProviderName => "Gemini CLI";

    public GeminiCliService(
        IOptions<AppConfiguration.GeminiCliSettings> settings,
        ILogger<GeminiCliService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Result<AiResponse>> SendMessageAsync(
        List<AiMessage> messages,
        List<AiTool>? tools = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_settings.Enabled)
            {
                return Result<AiResponse>.Failure("Gemini CLI is disabled in configuration");
            }

            // Ensure Gemini CLI is installed
            if (!await EnsureGeminiCliInstalledAsync())
            {
                return Result<AiResponse>.Failure("Gemini CLI is not installed and auto-install failed");
            }

            // Create MCP configuration for Excel integration
            await CreateMcpConfigAsync();

            // Build the message for Gemini CLI
            var messageText = BuildMessageFromHistory(messages);
            
            // Execute Gemini CLI command
            var result = await ExecuteGeminiCliAsync(messageText, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return Result<AiResponse>.Failure(result.Error!);
            }

            // Parse Gemini CLI response
            var aiResponse = ParseGeminiCliResponse(result.Value!);
            return Result<AiResponse>.Success(aiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Gemini CLI service");
            return Result<AiResponse>.Failure($"Gemini CLI error: {ex.Message}");
        }
    }

    private async Task<bool> EnsureGeminiCliInstalledAsync()
    {
        try
        {
            // Expand environment variables in executable path
            var executablePath = Environment.ExpandEnvironmentVariables(_settings.ExecutablePath);
            
            // Check if Gemini CLI is already installed
            var checkResult = await ExecuteCommandAsync(
                executablePath, 
                "--version", 
                TimeSpan.FromSeconds(10));

            if (checkResult.IsSuccess)
            {
                _logger.LogInformation("Gemini CLI is already installed: {Version}", checkResult.Value);
                return true;
            }

            if (!_settings.AutoInstall)
            {
                _logger.LogWarning("Gemini CLI not found and auto-install is disabled");
                return false;
            }

            _logger.LogInformation("Installing Gemini CLI: {Command}", _settings.InstallCommand);
            
            var installResult = await ExecuteCommandAsync(
                "cmd", 
                $"/c {_settings.InstallCommand}", 
                TimeSpan.FromMinutes(5));

            if (installResult.IsSuccess)
            {
                _logger.LogInformation("Gemini CLI installed successfully");
                return true;
            }

            _logger.LogError("Failed to install Gemini CLI: {Error}", installResult.Error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking/installing Gemini CLI");
            return false;
        }
    }

    private async Task CreateMcpConfigAsync()
    {
        try
        {
            var mcpConfig = new
            {
                mcpServers = new
                {
                    excel_mcp_server = new
                    {
                        command = "python",
                        args = new[] { "-m", "excel_mcp", "stdio" },
                        env = new { },
                        disabled = false
                    }
                }
            };

            // Get absolute path for MCP config - resolve relative to application directory
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var configPath = Path.IsPathRooted(_settings.McpConfigPath) 
                ? _settings.McpConfigPath 
                : Path.Combine(baseDir, _settings.McpConfigPath);
            
            // Normalize the path to avoid issues
            configPath = Path.GetFullPath(configPath);
            
            var configDir = Path.GetDirectoryName(configPath);
            
            if (configDir != null && !Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            var jsonContent = JsonSerializer.Serialize(mcpConfig, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            await File.WriteAllTextAsync(configPath, jsonContent);
            _logger.LogInformation("Created MCP config at: {Path}", configPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create MCP config file");
        }
    }

    private string BuildMessageFromHistory(List<AiMessage> messages)
    {
        var builder = new StringBuilder();
        
        // Add Excel context
        builder.AppendLine("CONTEXT: I am working with Excel files using MCP excel-mcp-server.");
        builder.AppendLine("Available Excel functions: read_range, write_range, format_range, create_chart, etc.");
        builder.AppendLine("");

        // Add recent conversation history
        foreach (var message in messages.TakeLast(3))
        {
            var content = string.Join(" ", message.Content
                .Where(c => c.Type == "text")
                .Select(c => c.Text ?? string.Empty));

            if (!string.IsNullOrEmpty(content))
            {
                var roleLabel = message.Role switch
                {
                    "user" => "USER",
                    "assistant" => "ASSISTANT", 
                    _ => message.Role.ToUpper()
                };

                builder.AppendLine($"{roleLabel}: {content}");
            }

            // Add tool results if available
            var toolResults = message.Content.Where(c => c.Type == "tool_result").ToList();
            foreach (var result in toolResults)
            {
                if (!string.IsNullOrEmpty(result.ToolResult))
                {
                    builder.AppendLine($"EXCEL_DATA: {result.ToolResult}");
                }
            }
        }

        return builder.ToString();
    }

    private async Task<Result<string>> ExecuteGeminiCliAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            var workingDir = Path.GetFullPath(_settings.WorkingDirectory);
            if (!Directory.Exists(workingDir))
            {
                Directory.CreateDirectory(workingDir);
            }

            // Expand environment variables in executable path
            var executablePath = Environment.ExpandEnvironmentVariables(_settings.ExecutablePath);
            
            // Get absolute path for MCP config - resolve relative to application directory
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var mcpConfigPath = Path.IsPathRooted(_settings.McpConfigPath) 
                ? _settings.McpConfigPath 
                : Path.Combine(baseDir, _settings.McpConfigPath);
            
            // Normalize the path to avoid issues
            mcpConfigPath = Path.GetFullPath(mcpConfigPath);
            
            // Use Gemini CLI's correct arguments - escape message for command line
            var escapedMessage = message.Replace("\"", "\\\"");
            var finalArgs = $"-p \"{escapedMessage}\" --allowed-mcp-server-names excel_mcp_server";
            
            _logger.LogInformation("Executing Gemini CLI: {Command} {Args}", executablePath, finalArgs);
            _logger.LogInformation("Gemini executable: {Executable}", executablePath);
            _logger.LogInformation("Working directory: {WorkingDir}", workingDir);

            var result = await ExecuteCommandAsync(
                executablePath,
                finalArgs,
                TimeSpan.FromSeconds(_settings.TimeoutSeconds),
                workingDir,
                cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Gemini CLI");
            return Result<string>.Failure($"Execution error: {ex.Message}");
        }
    }

    private AiResponse ParseGeminiCliResponse(string output)
    {
        try
        {
            _logger.LogInformation("Raw Gemini CLI output: {Output}", output);
            
            // Gemini CLI returns plain text, not JSON - clean and return directly
            var cleanedOutput = CleanGeminiCliOutput(output);
            
            _logger.LogInformation("Cleaned Gemini CLI output: {CleanedOutput}", cleanedOutput);
            
            // Only provide default message if we truly got nothing useful
            if (string.IsNullOrWhiteSpace(cleanedOutput) ||
                cleanedOutput.Contains("I'm ready to help you analyze your Excel files"))
            {
                cleanedOutput = "I'm ready to analyze your Excel file. Please ask me what you'd like to know about the data.";
            }
            
            return new AiResponse
            {
                Id = Guid.NewGuid().ToString(),
                Provider = ProviderName,
                Content = new List<AiResponseContent>
                {
                    new AiResponseContent
                    {
                        Type = "text",
                        Text = cleanedOutput
                    }
                },
                Usage = null,
                FinishReason = "stop"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing Gemini CLI response");
            
            return new AiResponse
            {
                Id = Guid.NewGuid().ToString(),
                Provider = ProviderName,
                Content = new List<AiResponseContent>
                {
                    new AiResponseContent
                    {
                        Type = "text",
                        Text = output.Trim()
                    }
                },
                Usage = null,
                FinishReason = "stop"
            };
        }
    }

    private string CleanGeminiCliOutput(string output)
    {
        // Remove Gemini CLI specific output formatting and keep only the response
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var responseLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Skip only CLI setup messages, keep everything else
            if (trimmed.Contains("Loaded cached credentials") ||
                trimmed.StartsWith("✓") || 
                trimmed.StartsWith("Loading") || 
                trimmed.StartsWith("Connecting to MCP server") || 
                trimmed.StartsWith("$") ||
                trimmed.Contains("gemini >") ||
                string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            // Remove "Assistant:" prefix if it exists but keep the content
            if (trimmed.StartsWith("Assistant:"))
            {
                trimmed = trimmed.Substring("Assistant:".Length).Trim();
            }
            
            if (!string.IsNullOrEmpty(trimmed))
            {
                responseLines.Add(trimmed);
            }
        }

        var result = string.Join("\n", responseLines).Trim();
        
        // If we got nothing meaningful, return the raw output minus obvious CLI noise
        if (string.IsNullOrEmpty(result))
        {
            var rawLines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var cleanLines = rawLines.Where(l => 
                !string.IsNullOrWhiteSpace(l.Trim()) &&
                !l.Contains("Loaded cached credentials") &&
                !l.StartsWith("✓") &&
                !l.StartsWith("Loading")).ToList();
            
            return string.Join("\n", cleanLines).Trim();
        }

        return result;
    }

    private string ExtractMeaningfulContent(string jsonOutput)
    {
        try
        {
            // If it looks like JSON, try to extract the result field
            if (jsonOutput.Trim().StartsWith("{"))
            {
                var startIndex = jsonOutput.IndexOf("\"result\":\"");
                if (startIndex >= 0)
                {
                    startIndex += 10; // Length of "result":"
                    var endIndex = jsonOutput.LastIndexOf("\",\"session_id\"");
                    if (endIndex > startIndex)
                    {
                        var resultContent = jsonOutput.Substring(startIndex, endIndex - startIndex);
                        // Remove escape characters
                        resultContent = resultContent.Replace("\\n", "\n")
                                                   .Replace("\\\"", "\"")
                                                   .Replace("\\\\", "\\");
                        return resultContent;
                    }
                }
            }

            // Fallback to basic cleaning
            return CleanGeminiCliOutput(jsonOutput);
        }
        catch
        {
            return CleanGeminiCliOutput(jsonOutput);
        }
    }

    private async Task<Result<string>> ExecuteCommandWithStdinAsync(
        string fileName,
        string arguments,
        string stdinInput,
        TimeSpan timeout,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.StandardInputEncoding = Encoding.UTF8;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

            // Add Node.js environment variable to suppress warnings if needed
            process.StartInfo.EnvironmentVariables["NODE_NO_WARNINGS"] = "1";
            process.StartInfo.EnvironmentVariables["NODE_OPTIONS"] = "--no-deprecation";

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            var output = new StringBuilder();
            var error = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    output.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    // Filter out Node.js deprecation warnings
                    var data = e.Data;
                    if (!data.Contains("DEP0190") && !data.Contains("DeprecationWarning") && 
                        !data.Contains("--trace-deprecation"))
                    {
                        error.AppendLine(data);
                    }
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Write input to stdin and close it
            await process.StandardInput.WriteLineAsync(stdinInput);
            process.StandardInput.Close();

            await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds), cancellationToken);
            var completed = process.HasExited;
            
            if (!completed)
            {
                try { process.Kill(); } catch { }
                return Result<string>.Failure("Process timed out");
            }

            var outputStr = output.ToString().Trim();
            var errorStr = error.ToString().Trim();

            // If we only have Node.js deprecation warnings, consider it successful
            if (process.ExitCode != 0 && string.IsNullOrEmpty(errorStr))
            {
                // Check if output contains actual content despite exit code 1
                if (!string.IsNullOrEmpty(outputStr) && outputStr.Contains("result"))
                {
                    return Result<string>.Success(outputStr);
                }
            }

            if (process.ExitCode == 0 || (!string.IsNullOrEmpty(outputStr) && string.IsNullOrEmpty(errorStr)))
            {
                return Result<string>.Success(outputStr);
            }
            else
            {
                var errorMessage = !string.IsNullOrEmpty(errorStr) ? errorStr : "Gemini CLI failed";
                return Result<string>.Failure($"Process failed (exit code {process.ExitCode}): {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Process execution error: {ex.Message}");
        }
    }

    private async Task<Result<string>> ExecuteCommandAsync(
        string fileName, 
        string arguments, 
        TimeSpan timeout, 
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            var output = new StringBuilder();
            var error = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    output.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    error.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds), cancellationToken);
            var completed = process.HasExited;
            
            if (!completed)
            {
                try { process.Kill(); } catch { }
                return Result<string>.Failure("Process timed out");
            }

            var outputStr = output.ToString().Trim();
            var errorStr = error.ToString().Trim();

            if (process.ExitCode == 0)
            {
                return Result<string>.Success(outputStr);
            }
            else
            {
                var errorMessage = !string.IsNullOrEmpty(errorStr) ? errorStr : outputStr;
                return Result<string>.Failure($"Process failed (exit code {process.ExitCode}): {errorMessage}");
            }
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Process execution error: {ex.Message}");
        }
    }
}