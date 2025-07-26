using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Interface for desktop application automation services
/// </summary>
public interface IDesktopAutomationService
{
    /// <summary>
    /// Send a message to the desktop application and get response
    /// </summary>
    Task<Result<string>> SendMessageAsync(string message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if the desktop application is available and running
    /// </summary>
    Task<bool> IsAvailableAsync();
    
    /// <summary>
    /// Launch the desktop application if not running
    /// </summary>
    Task<Result<bool>> LaunchApplicationAsync();
    
    /// <summary>
    /// Get the provider name for this automation service
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// Check if this automation service is enabled in configuration
    /// </summary>
    bool IsEnabled { get; }
}