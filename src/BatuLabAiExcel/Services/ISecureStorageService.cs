namespace BatuLabAiExcel.Services;

/// <summary>
/// Secure storage service for credentials and sensitive data
/// </summary>
public interface ISecureStorageService
{
    /// <summary>
    /// Store user credentials securely
    /// </summary>
    Task StoreCredentialsAsync(string email, string token);

    /// <summary>
    /// Retrieve stored credentials
    /// </summary>
    Task<(string? Email, string? Token)> GetStoredCredentialsAsync();

    /// <summary>
    /// Clear stored credentials
    /// </summary>
    Task ClearCredentialsAsync();

    /// <summary>
    /// Store API keys and secrets
    /// </summary>
    Task StoreApiKeyAsync(string keyName, string keyValue);

    /// <summary>
    /// Retrieve API key
    /// </summary>
    Task<string?> GetApiKeyAsync(string keyName);

    /// <summary>
    /// Get machine identifier for license validation
    /// </summary>
    string GetMachineId();

    /// <summary>
    /// Get a value by key
    /// </summary>
    Task<string?> GetAsync(string key);

    /// <summary>
    /// Set a value by key
    /// </summary>
    Task SetAsync(string key, string value);

    /// <summary>
    /// Remove a value by key
    /// </summary>
    Task RemoveAsync(string key);
}