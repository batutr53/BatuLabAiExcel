using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Secure storage service implementation using Windows Registry and DPAPI
/// </summary>
public class SecureStorageService : ISecureStorageService
{
    private readonly ILogger<SecureStorageService> _logger;
    private const string RegistryPath = @"SOFTWARE\BatuLab\OfficeAI";
    private const string CredentialsKey = "UserCredentials";
    private const string ApiKeysKey = "ApiKeys";

    public SecureStorageService(ILogger<SecureStorageService> logger)
    {
        _logger = logger;
    }

    public async Task StoreCredentialsAsync(string email, string token)
    {
        try
        {
            var data = $"{email}|{token}";
            var encryptedData = ProtectedData.Protect(Encoding.UTF8.GetBytes(data), null, DataProtectionScope.CurrentUser);
            var base64Data = Convert.ToBase64String(encryptedData);

            await Task.Run(() =>
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
                key.SetValue(CredentialsKey, base64Data);
            });

            _logger.LogDebug("Credentials stored securely for user: {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing credentials for user: {Email}", email);
            throw;
        }
    }

    public async Task<(string? Email, string? Token)> GetStoredCredentialsAsync()
    {
        try
        {
            var base64Data = await Task.Run(() =>
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
                return key?.GetValue(CredentialsKey) as string;
            });

            if (string.IsNullOrEmpty(base64Data))
                return (null, null);

            var encryptedData = Convert.FromBase64String(base64Data);
            var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            var data = Encoding.UTF8.GetString(decryptedData);

            var parts = data.Split('|');
            if (parts.Length == 2)
            {
                _logger.LogDebug("Retrieved stored credentials");
                return (parts[0], parts[1]);
            }

            return (null, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stored credentials");
            return (null, null);
        }
    }

    public async Task ClearCredentialsAsync()
    {
        try
        {
            await Task.Run(() =>
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, true);
                key?.DeleteValue(CredentialsKey, false);
            });

            _logger.LogDebug("Stored credentials cleared");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing stored credentials");
        }
    }

    public async Task StoreApiKeyAsync(string keyName, string keyValue)
    {
        try
        {
            var encryptedData = ProtectedData.Protect(Encoding.UTF8.GetBytes(keyValue), null, DataProtectionScope.CurrentUser);
            var base64Data = Convert.ToBase64String(encryptedData);

            await Task.Run(() =>
            {
                using var key = Registry.CurrentUser.CreateSubKey($"{RegistryPath}\\{ApiKeysKey}");
                key.SetValue(keyName, base64Data);
            });

            _logger.LogDebug("API key stored securely: {KeyName}", keyName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing API key: {KeyName}", keyName);
            throw;
        }
    }

    public async Task<string?> GetApiKeyAsync(string keyName)
    {
        try
        {
            var base64Data = await Task.Run(() =>
            {
                using var key = Registry.CurrentUser.OpenSubKey($"{RegistryPath}\\{ApiKeysKey}");
                return key?.GetValue(keyName) as string;
            });

            if (string.IsNullOrEmpty(base64Data))
                return null;

            var encryptedData = Convert.FromBase64String(base64Data);
            var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API key: {KeyName}", keyName);
            return null;
        }
    }

    public string GetMachineId()
    {
        try
        {
            // Generate a consistent machine ID based on hardware characteristics
            var machineInfo = new StringBuilder();

            // Get processor ID
            using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    machineInfo.Append(obj["ProcessorId"]?.ToString());
                    break; // Use first processor
                }
            }

            // Get motherboard serial number
            using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    machineInfo.Append(obj["SerialNumber"]?.ToString());
                    break;
                }
            }

            // Get machine name as fallback
            machineInfo.Append(Environment.MachineName);

            // Create hash of the combined information
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineInfo.ToString()));
            return Convert.ToBase64String(hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating machine ID, using fallback");
            // Fallback to machine name hash
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName));
            return Convert.ToBase64String(hash);
        }
    }
}