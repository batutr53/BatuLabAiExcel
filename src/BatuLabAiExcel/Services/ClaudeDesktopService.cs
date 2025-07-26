using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Desktop automation service for Claude Desktop App
/// </summary>
public class ClaudeDesktopService : IDesktopAutomationService
{
    private readonly WindowsAutomationHelper _automationHelper;
    private readonly AppConfiguration.ClaudeDesktopSettings _settings;
    private readonly AppConfiguration.GeneralDesktopSettings _generalSettings;
    private readonly ILogger<ClaudeDesktopService> _logger;
    
    private IntPtr _windowHandle;
    private string _lastResponse = string.Empty;

    public string ProviderName => "Claude Desktop";
    public bool IsEnabled => _settings.Enabled;

    public ClaudeDesktopService(
        WindowsAutomationHelper automationHelper,
        IOptions<AppConfiguration.DesktopAutomationSettings> settings,
        ILogger<ClaudeDesktopService> logger)
    {
        _automationHelper = automationHelper;
        _settings = settings.Value.Claude;
        _generalSettings = settings.Value.General;
        _logger = logger;
    }

    public async Task<Result<string>> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Ensure Claude Desktop is running and available
            if (!await EnsureApplicationReadyAsync())
            {
                return Result<string>.Failure("Claude Desktop app is not available or could not be launched");
            }

            _logger.LogInformation("Sending message to Claude Desktop: {MessageLength} characters", message.Length);

            // Clear any previous content and send the message
            if (!await SendMessageToAppAsync(message))
            {
                return Result<string>.Failure("Failed to send message to Claude Desktop");
            }

            // Wait for and retrieve response
            var response = await WaitForResponseAsync(cancellationToken);
            if (string.IsNullOrEmpty(response))
            {
                return Result<string>.Failure("No response received from Claude Desktop or response timeout");
            }

            _logger.LogInformation("Received response from Claude Desktop: {ResponseLength} characters", response.Length);
            return Result<string>.Success(response);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Claude Desktop automation was cancelled");
            return Result<string>.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Claude Desktop automation");
            return Result<string>.Failure($"Desktop automation error: {ex.Message}");
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            return _automationHelper.IsProcessRunning(_settings.ProcessName) && 
                   await _automationHelper.FindWindowAsync(_settings.ProcessName, _settings.WindowTitle) != IntPtr.Zero;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Claude Desktop availability");
            return false;
        }
    }

    public async Task<Result<bool>> LaunchApplicationAsync()
    {
        try
        {
            if (await IsAvailableAsync())
            {
                _logger.LogInformation("Claude Desktop is already running");
                return Result<bool>.Success(true);
            }

            if (!_generalSettings.EnableAutoLaunch)
            {
                return Result<bool>.Failure("Auto-launch is disabled in configuration");
            }

            _logger.LogInformation("Launching Claude Desktop application");
            
            var process = await _automationHelper.LaunchApplicationAsync(_settings.LaunchPath, _settings.LaunchCommand);
            if (process == null)
            {
                return Result<bool>.Failure("Failed to launch Claude Desktop application");
            }

            // Wait for application to start up
            await Task.Delay(_generalSettings.WaitForAppStartup);

            // Verify the application is now available
            var isAvailable = await IsAvailableAsync();
            if (!isAvailable)
            {
                return Result<bool>.Failure("Claude Desktop launched but is not responding");
            }

            _logger.LogInformation("Claude Desktop launched successfully");
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error launching Claude Desktop");
            return Result<bool>.Failure($"Launch error: {ex.Message}");
        }
    }

    private async Task<bool> EnsureApplicationReadyAsync()
    {
        // Check if already available
        if (await IsAvailableAsync())
        {
            _windowHandle = await _automationHelper.FindWindowAsync(_settings.ProcessName, _settings.WindowTitle);
            return _windowHandle != IntPtr.Zero;
        }

        // Try to launch if not available
        var launchResult = await LaunchApplicationAsync();
        if (!launchResult.IsSuccess)
        {
            _logger.LogError("Failed to launch Claude Desktop: {Error}", launchResult.Error);
            return false;
        }

        _windowHandle = await _automationHelper.FindWindowAsync(_settings.ProcessName, _settings.WindowTitle);
        return _windowHandle != IntPtr.Zero;
    }

    private async Task<bool> SendMessageToAppAsync(string message)
    {
        try
        {
            if (_windowHandle == IntPtr.Zero)
            {
                return false;
            }

            // Send the message text
            if (!await _automationHelper.SendTextToWindowAsync(_windowHandle, message))
            {
                return false;
            }

            // Wait a moment for the text to be processed
            await Task.Delay(_generalSettings.MessageDelay);

            // Send Enter key to submit the message
            if (!await _automationHelper.SendEnterKeyAsync(_windowHandle))
            {
                return false;
            }

            _logger.LogDebug("Successfully sent message to Claude Desktop");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Claude Desktop");
            return false;
        }
    }

    private async Task<string> WaitForResponseAsync(CancellationToken cancellationToken)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var maxWaitTime = TimeSpan.FromMilliseconds(_generalSettings.MaxResponseWaitTime);
            var pollInterval = TimeSpan.FromMilliseconds(_generalSettings.ResponsePollInterval);

            _lastResponse = string.Empty;
            string previousContent = string.Empty;
            int stableResponseCount = 0;
            const int requiredStableCount = 3; // Response must be stable for 3 polls

            while (DateTime.UtcNow - startTime < maxWaitTime && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Get current window content (this is a simplified approach)
                    // In a real implementation, you might need to use UI Automation to find specific elements
                    var currentContent = await _automationHelper.GetWindowTextAsync(_windowHandle);
                    
                    if (!string.IsNullOrEmpty(currentContent) && currentContent != previousContent)
                    {
                        // Content changed, reset stability counter
                        previousContent = currentContent;
                        stableResponseCount = 0;
                        _lastResponse = ExtractLatestResponse(currentContent);
                        
                        _logger.LogDebug("Content changed, new response detected: {Length} chars", 
                            _lastResponse.Length);
                    }
                    else if (!string.IsNullOrEmpty(_lastResponse))
                    {
                        // Content is stable, increment counter
                        stableResponseCount++;
                        
                        if (stableResponseCount >= requiredStableCount)
                        {
                            _logger.LogDebug("Response stabilized after {Count} polls", stableResponseCount);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error polling for response");
                }

                await Task.Delay(pollInterval, cancellationToken);
            }

            if (string.IsNullOrEmpty(_lastResponse))
            {
                _logger.LogWarning("No response received within timeout period");
            }

            return _lastResponse;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Response waiting was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error waiting for response");
            return string.Empty;
        }
    }

    private string ExtractLatestResponse(string windowContent)
    {
        // This is a simplified response extraction
        // In a real implementation, you would need to parse the specific UI structure
        // of Claude Desktop to extract just the assistant's response
        
        try
        {
            // Look for patterns that might indicate an assistant response
            // This would need to be customized based on the actual Claude Desktop UI
            
            var lines = windowContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            // Simple heuristic: return the last substantial chunk of text
            var response = new List<string>();
            bool foundResponse = false;
            
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                // Skip obvious UI elements (this would need refinement)
                if (line.Contains("Send") || line.Contains("Claude") || line.Length < 10)
                    continue;
                
                response.Insert(0, line);
                foundResponse = true;
                
                // Stop if we have a reasonable amount of content
                if (response.Count > 5 || string.Join(" ", response).Length > 200)
                    break;
            }
            
            return foundResponse ? string.Join(" ", response) : windowContent;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting response, returning raw content");
            return windowContent;
        }
    }
}