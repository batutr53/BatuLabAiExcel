using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Desktop automation service for ChatGPT Desktop App
/// </summary>
public class ChatGptDesktopService : IDesktopAutomationService
{
    private readonly WindowsAutomationHelper _automationHelper;
    private readonly AppConfiguration.ChatGptDesktopSettings _settings;
    private readonly AppConfiguration.GeneralDesktopSettings _generalSettings;
    private readonly ILogger<ChatGptDesktopService> _logger;
    
    private IntPtr _windowHandle;
    private string _lastResponse = string.Empty;

    public string ProviderName => "ChatGPT Desktop";
    public bool IsEnabled => _settings.Enabled;

    public ChatGptDesktopService(
        WindowsAutomationHelper automationHelper,
        IOptions<AppConfiguration.DesktopAutomationSettings> settings,
        ILogger<ChatGptDesktopService> logger)
    {
        _automationHelper = automationHelper;
        _settings = settings.Value.ChatGPT;
        _generalSettings = settings.Value.General;
        _logger = logger;
    }

    public async Task<Result<string>> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await EnsureApplicationReadyAsync())
            {
                return Result<string>.Failure("ChatGPT Desktop app is not available or could not be launched");
            }

            _logger.LogInformation("Sending message to ChatGPT Desktop: {MessageLength} characters", message.Length);

            if (!await SendMessageToAppAsync(message))
            {
                return Result<string>.Failure("Failed to send message to ChatGPT Desktop");
            }

            var response = await WaitForResponseAsync(cancellationToken);
            if (string.IsNullOrEmpty(response))
            {
                return Result<string>.Failure("No response received from ChatGPT Desktop or response timeout");
            }

            _logger.LogInformation("Received response from ChatGPT Desktop: {ResponseLength} characters", response.Length);
            return Result<string>.Success(response);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("ChatGPT Desktop automation was cancelled");
            return Result<string>.Failure("Operation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ChatGPT Desktop automation");
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
            _logger.LogError(ex, "Error checking ChatGPT Desktop availability");
            return false;
        }
    }

    public async Task<Result<bool>> LaunchApplicationAsync()
    {
        try
        {
            if (await IsAvailableAsync())
            {
                _logger.LogInformation("ChatGPT Desktop is already running");
                return Result<bool>.Success(true);
            }

            if (!_generalSettings.EnableAutoLaunch)
            {
                return Result<bool>.Failure("Auto-launch is disabled in configuration");
            }

            _logger.LogInformation("Launching ChatGPT Desktop application");
            
            var process = await _automationHelper.LaunchApplicationAsync(_settings.LaunchPath, _settings.LaunchCommand);
            if (process == null)
            {
                return Result<bool>.Failure("Failed to launch ChatGPT Desktop application");
            }

            await Task.Delay(_generalSettings.WaitForAppStartup);

            var isAvailable = await IsAvailableAsync();
            if (!isAvailable)
            {
                return Result<bool>.Failure("ChatGPT Desktop launched but is not responding");
            }

            _logger.LogInformation("ChatGPT Desktop launched successfully");
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error launching ChatGPT Desktop");
            return Result<bool>.Failure($"Launch error: {ex.Message}");
        }
    }

    private async Task<bool> EnsureApplicationReadyAsync()
    {
        if (await IsAvailableAsync())
        {
            _windowHandle = await _automationHelper.FindWindowAsync(_settings.ProcessName, _settings.WindowTitle);
            return _windowHandle != IntPtr.Zero;
        }

        var launchResult = await LaunchApplicationAsync();
        if (!launchResult.IsSuccess)
        {
            _logger.LogError("Failed to launch ChatGPT Desktop: {Error}", launchResult.Error);
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

            if (!await _automationHelper.SendTextToWindowAsync(_windowHandle, message))
            {
                return false;
            }

            await Task.Delay(_generalSettings.MessageDelay);

            if (!await _automationHelper.SendEnterKeyAsync(_windowHandle))
            {
                return false;
            }

            _logger.LogDebug("Successfully sent message to ChatGPT Desktop");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to ChatGPT Desktop");
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
            const int requiredStableCount = 3;

            while (DateTime.UtcNow - startTime < maxWaitTime && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var currentContent = await _automationHelper.GetWindowTextAsync(_windowHandle);
                    
                    if (!string.IsNullOrEmpty(currentContent) && currentContent != previousContent)
                    {
                        previousContent = currentContent;
                        stableResponseCount = 0;
                        _lastResponse = ExtractLatestResponse(currentContent);
                        
                        _logger.LogDebug("Content changed, new response detected: {Length} chars", 
                            _lastResponse.Length);
                    }
                    else if (!string.IsNullOrEmpty(_lastResponse))
                    {
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
        try
        {
            var lines = windowContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            var response = new List<string>();
            bool foundResponse = false;
            
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;
                
                // Skip ChatGPT UI elements
                if (line.Contains("Send") || line.Contains("ChatGPT") || line.Contains("New chat") || line.Length < 10)
                    continue;
                
                response.Insert(0, line);
                foundResponse = true;
                
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