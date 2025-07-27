using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.IO;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Service for interacting with Claude CLI
/// </summary>
public class ClaudeCliService : IAiService
{
    private readonly AppConfiguration.ClaudeCliSettings _settings;
    private readonly ILogger<ClaudeCliService> _logger;

    public string ProviderName => "Claude CLI";

    public ClaudeCliService(
        IOptions<AppConfiguration.ClaudeCliSettings> settings,
        ILogger<ClaudeCliService> logger)
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
                return Result<AiResponse>.Failure("Claude CLI is disabled in configuration");
            }

            // Ensure Claude CLI is installed
            if (!await EnsureClaudeCliInstalledAsync())
            {
                return Result<AiResponse>.Failure("Claude CLI is not installed and auto-install failed");
            }

            // Create MCP configuration for Excel integration
            await CreateMcpConfigAsync();

            // Build the message for Claude CLI
            var messageText = BuildMessageFromHistory(messages);
            
            // Execute Claude CLI command
            var result = await ExecuteClaudeCliAsync(messageText, cancellationToken);
            
            if (!result.IsSuccess)
            {
                return Result<AiResponse>.Failure(result.Error!);
            }

            // Parse Claude CLI response
            var aiResponse = ParseClaudeCliResponse(result.Value!);
            return Result<AiResponse>.Success(aiResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Claude CLI service");
            return Result<AiResponse>.Failure($"Claude CLI error: {ex.Message}");
        }
    }

    private async Task<bool> EnsureClaudeCliInstalledAsync()
    {
        try
        {
            // Expand environment variables in executable path
            var executablePath = Environment.ExpandEnvironmentVariables(_settings.ExecutablePath);
            
            // Use PowerShell for better command handling on Windows
            var checkCommand = "powershell.exe";
            var checkArgs = $"-Command \"& '{executablePath}' --version\"";
            
            // Check if Claude CLI is already installed
            var checkResult = await ExecuteCommandAsync(
                checkCommand, 
                checkArgs, 
                TimeSpan.FromSeconds(10));

            if (checkResult.IsSuccess)
            {
                _logger.LogInformation("Claude CLI is already installed: {Version}", checkResult.Value);
                return true;
            }

            if (!_settings.AutoInstall)
            {
                _logger.LogWarning("Claude CLI not found and auto-install is disabled");
                return false;
            }

            _logger.LogInformation("Installing Claude CLI: {Command}", _settings.InstallCommand);
            
            var installResult = await ExecuteCommandAsync(
                "cmd", 
                $"/c {_settings.InstallCommand}", 
                TimeSpan.FromMinutes(5));

            if (installResult.IsSuccess)
            {
                _logger.LogInformation("Claude CLI installed successfully");
                return true;
            }

            _logger.LogError("Failed to install Claude CLI: {Error}", installResult.Error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking/installing Claude CLI");
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

    private async Task<Result<string>> ExecuteClaudeCliAsync(string message, CancellationToken cancellationToken)
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
            
            // Escape the message for command line
            var escapedMessage = message.Replace("\"", "\\\"");
            
            // Use PowerShell for better command handling on Windows
            var finalCommand = "powershell.exe";
            var claudeArgs = $"--print --output-format json --mcp-config '{mcpConfigPath}' --dangerously-skip-permissions '{escapedMessage}'";
            var finalArgs = $"-Command \"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; & '{executablePath}' {claudeArgs}\"";
            
            _logger.LogInformation("Executing Claude CLI via PowerShell: {Command} {Args}", finalCommand, finalArgs);
            _logger.LogInformation("Claude executable: {Executable}", executablePath);
            _logger.LogInformation("MCP config path: {McpPath}", mcpConfigPath);

            var result = await ExecuteCommandAsync(
                finalCommand,
                finalArgs,
                TimeSpan.FromSeconds(_settings.TimeoutSeconds),
                workingDir,
                cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Claude CLI");
            return Result<string>.Failure($"Execution error: {ex.Message}");
        }
    }

    private AiResponse ParseClaudeCliResponse(string output)
    {
        try
        {
            // Claude CLI --output-format json returns a JSON with "result" field
            var jsonDocument = JsonDocument.Parse(output);
            if (jsonDocument.RootElement.TryGetProperty("result", out var resultElement))
            {
                var resultText = resultElement.GetString();
                if (!string.IsNullOrEmpty(resultText))
                {
                    // Fix Turkish character encoding issues
                    var cleanedText = FixTurkishCharacters(resultText);
                    
                    return new AiResponse
                    {
                        Id = Guid.NewGuid().ToString(),
                        Provider = ProviderName,
                        Content = new List<AiResponseContent>
                        {
                            new AiResponseContent
                            {
                                Type = "text",
                                Text = cleanedText
                            }
                        },
                        Usage = null,
                        FinishReason = "stop"
                    };
                }
            }

            // If no result field, try to extract text from JSON structure
            var cleanedOutput = CleanClaudeCliOutput(output);
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
            _logger.LogWarning(ex, "Error parsing Claude CLI JSON response, falling back to plain text");
            
            // Try to extract just the meaningful content if it's visible JSON
            var cleanedOutput = ExtractMeaningfulContent(output);
            
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
    }

    private string CleanClaudeCliOutput(string output)
    {
        // Remove Claude CLI specific output formatting and keep only the response
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var responseLines = new List<string>();
        bool inResponse = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Skip CLI status messages
            if (trimmed.StartsWith("✓") || trimmed.StartsWith("Loading") || 
                trimmed.StartsWith("Connecting") || trimmed.StartsWith("$") ||
                trimmed.Contains("claude >"))
            {
                continue;
            }

            // Start collecting response content
            if (!inResponse && (trimmed.Length > 10 || responseLines.Count > 0))
            {
                inResponse = true;
            }

            if (inResponse)
            {
                responseLines.Add(line);
            }
        }

        return string.Join("\n", responseLines).Trim();
    }

    private string FixTurkishCharacters(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Common Turkish character encoding fixes
        var fixes = new Dictionary<string, string>
        {
            // Unicode escape sequences to proper Turkish characters
            {"\\u0131", "ı"}, {"\\u0130", "İ"},
            {"\\u015f", "ş"}, {"\\u015e", "Ş"},
            {"\\u011f", "ğ"}, {"\\u011e", "Ğ"},
            {"\\u00fc", "ü"}, {"\\u00dc", "Ü"},
            {"\\u00f6", "ö"}, {"\\u00d6", "Ö"},
            {"\\u00e7", "ç"}, {"\\u00c7", "Ç"},
            
            // HTML entities
            {"&ccedil;", "ç"}, {"&Ccedil;", "Ç"},
            {"&ouml;", "ö"}, {"&Ouml;", "Ö"},
            {"&uuml;", "ü"}, {"&Uuml;", "Ü"},
            {"&inodot;", "ı"}, {"&Idot;", "İ"},
            {"&scaron;", "ş"}, {"&Scaron;", "Ş"},
            {"&gbreve;", "ğ"}, {"&Gbreve;", "Ğ"},
            
            // Common misencoded characters
            {"Ä±", "ı"}, {"Ä°", "İ"},
            {"ÅŸ", "ş"}, {"Åž", "Ş"}, 
            {"Ä£", "ğ"}, {"Ä¢", "Ğ"},
            {"Ã¼", "ü"}, {"Ãœ", "Ü"},
            {"Ã¶", "ö"}, {"Ã–", "Ö"},
            {"Ã§", "ç"}, {"Ã‡", "Ç"}
        };

        var result = input;
        foreach (var fix in fixes)
        {
            result = result.Replace(fix.Key, fix.Value);
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
                        // Remove escape characters and fix Turkish characters
                        resultContent = resultContent.Replace("\\n", "\n")
                                                   .Replace("\\\"", "\"")
                                                   .Replace("\\\\", "\\");
                        return FixTurkishCharacters(resultContent);
                    }
                }
            }

            // Fallback to basic cleaning
            return CleanClaudeCliOutput(jsonOutput);
        }
        catch
        {
            return CleanClaudeCliOutput(jsonOutput);
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

    private class ClaudeCliJsonResponse
    {
        public string? Response { get; set; }
        public string? Status { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}