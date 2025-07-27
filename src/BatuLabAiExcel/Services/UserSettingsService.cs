using Microsoft.Extensions.Logging;
using System.Text.Json;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Service for managing user settings and API keys securely
/// </summary>
public class UserSettingsService : IUserSettingsService
{
    private readonly ISecureStorageService _secureStorage;
    private readonly ILogger<UserSettingsService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    private const string API_KEY_PREFIX = "ApiKey_";
    private const string SETTING_PREFIX = "Setting_";
    private const string DEFAULT_PROVIDER_KEY = "DefaultProvider";

    public UserSettingsService(
        ISecureStorageService secureStorage,
        ILogger<UserSettingsService> logger)
    {
        _secureStorage = secureStorage;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<string?> GetApiKeyAsync(string provider)
    {
        try
        {
            var key = API_KEY_PREFIX + provider;
            return await _secureStorage.GetAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API key for provider: {Provider}", provider);
            return null;
        }
    }

    public async Task<Result> SetApiKeyAsync(string provider, string apiKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(provider))
                return Result.Failure("Provider name cannot be empty");
            
            if (string.IsNullOrWhiteSpace(apiKey))
                return Result.Failure("API key cannot be empty");

            var key = API_KEY_PREFIX + provider;
            await _secureStorage.SetAsync(key, apiKey);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting API key for provider: {Provider}", provider);
            return Result.Failure($"Failed to save API key: {ex.Message}");
        }
    }

    public async Task<T?> GetSettingAsync<T>(string key)
    {
        try
        {
            var storageKey = SETTING_PREFIX + key;
            var result = await _secureStorage.GetAsync(storageKey);
            
            if (string.IsNullOrEmpty(result))
                return default(T);

            if (typeof(T) == typeof(string))
                return (T)(object)result;

            return JsonSerializer.Deserialize<T>(result, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting setting: {Key}", key);
            return default(T);
        }
    }

    public async Task<Result> SetSettingAsync<T>(string key, T value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
                return Result.Failure("Setting key cannot be empty");

            var storageKey = SETTING_PREFIX + key;
            string serializedValue;

            if (typeof(T) == typeof(string))
            {
                serializedValue = value?.ToString() ?? string.Empty;
            }
            else
            {
                serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
            }

            await _secureStorage.SetAsync(storageKey, serializedValue);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value for key: {Key}", key);
            return Result.Failure($"Failed to save setting: {ex.Message}");
        }
    }

    public async Task<Result> RemoveSettingAsync(string key)
    {
        try
        {
            var storageKey = SETTING_PREFIX + key;
            await _secureStorage.RemoveAsync(storageKey);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing setting: {Key}", key);
            return Result.Failure($"Failed to remove setting: {ex.Message}");
        }
    }

    public async Task<bool> HasApiKeyAsync(string provider)
    {
        try
        {
            var apiKey = await GetApiKeyAsync(provider);
            return !string.IsNullOrWhiteSpace(apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking API key for provider: {Provider}", provider);
            return false;
        }
    }

    public async Task<string> GetDefaultProviderAsync()
    {
        try
        {
            var provider = await GetSettingAsync<string>(DEFAULT_PROVIDER_KEY);
            return provider ?? "Claude";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default provider");
            return "Claude";
        }
    }

    public async Task<Result> SetDefaultProviderAsync(string provider)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(provider))
                return Result.Failure("Provider name cannot be empty");

            return await SetSettingAsync(DEFAULT_PROVIDER_KEY, provider);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default provider: {Provider}", provider);
            return Result.Failure($"Failed to set default provider: {ex.Message}");
        }
    }
}