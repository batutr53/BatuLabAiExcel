using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Service for managing user settings and API keys securely
/// </summary>
public interface IUserSettingsService
{
    /// <summary>
    /// Get AI provider API key securely
    /// </summary>
    Task<string?> GetApiKeyAsync(string provider);
    
    /// <summary>
    /// Set AI provider API key securely
    /// </summary>
    Task<Result> SetApiKeyAsync(string provider, string apiKey);
    
    /// <summary>
    /// Get user preference
    /// </summary>
    Task<T?> GetSettingAsync<T>(string key);
    
    /// <summary>
    /// Set user preference
    /// </summary>
    Task<Result> SetSettingAsync<T>(string key, T value);
    
    /// <summary>
    /// Remove setting
    /// </summary>
    Task<Result> RemoveSettingAsync(string key);
    
    /// <summary>
    /// Check if API key is configured for provider
    /// </summary>
    Task<bool> HasApiKeyAsync(string provider);
    
    /// <summary>
    /// Get default AI provider
    /// </summary>
    Task<string> GetDefaultProviderAsync();
    
    /// <summary>
    /// Set default AI provider
    /// </summary>
    Task<Result> SetDefaultProviderAsync(string provider);
}