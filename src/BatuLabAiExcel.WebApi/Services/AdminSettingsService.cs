using BatuLabAiExcel.WebApi.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// Service for managing admin settings
/// </summary>
public class AdminSettingsService : IAdminSettingsService
{
    private readonly IOptionsMonitor<AppConfiguration.AuthenticationSettings> _authSettingsMonitor;
    private readonly IOptionsMonitor<AppConfiguration.LicenseSettings> _licenseSettingsMonitor;
    private readonly IOptionsMonitor<AppConfiguration.StripeSettings> _stripeSettingsMonitor;
    private readonly IOptionsMonitor<AppConfiguration.EmailSettings> _emailSettingsMonitor;
    private readonly IOptionsMonitor<RateLimitSettings> _rateLimitSettingsMonitor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminSettingsService> _logger;
    private readonly string _settingsFilePath;

    // Static settings cache
    private static AdminSettings? _cachedSettings;
    private static readonly object _settingsLock = new object();

    public AdminSettingsService(
        IOptionsMonitor<AppConfiguration.AuthenticationSettings> authSettingsMonitor,
        IOptionsMonitor<AppConfiguration.LicenseSettings> licenseSettingsMonitor,
        IOptionsMonitor<AppConfiguration.StripeSettings> stripeSettingsMonitor,
        IOptionsMonitor<AppConfiguration.EmailSettings> emailSettingsMonitor,
        IOptionsMonitor<RateLimitSettings> rateLimitSettingsMonitor,
        IConfiguration configuration,
        ILogger<AdminSettingsService> logger)
    {
        _authSettingsMonitor = authSettingsMonitor;
        _licenseSettingsMonitor = licenseSettingsMonitor;
        _stripeSettingsMonitor = stripeSettingsMonitor;
        _emailSettingsMonitor = emailSettingsMonitor;
        _rateLimitSettingsMonitor = rateLimitSettingsMonitor;
        _configuration = configuration;
        _logger = logger;
        
        // Settings file path for persistent storage
        _settingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "admin_settings.json");
    }

    public Task<ApiResponse<AdminSettings>> GetSettingsAsync()
    {
        try
        {
            lock (_settingsLock)
            {
                // Return cached settings if available
                if (_cachedSettings != null)
                {
                    _logger.LogInformation("Returning cached settings: {@Settings}", _cachedSettings);
                    return Task.FromResult(ApiResponse<AdminSettings>.SuccessResult(_cachedSettings));
                }

                // Try to load from file first
                if (File.Exists(_settingsFilePath))
                {
                    try
                    {
                        var jsonContent = File.ReadAllText(_settingsFilePath);
                        var fileSettings = JsonSerializer.Deserialize<AdminSettings>(jsonContent);
                        if (fileSettings != null)
                        {
                            _cachedSettings = fileSettings;
                            return Task.FromResult(ApiResponse<AdminSettings>.SuccessResult(_cachedSettings));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load settings from file, using default values");
                    }
                }

                // Load default settings from configuration
                var authSettings = _authSettingsMonitor.CurrentValue;
                var licenseSettings = _licenseSettingsMonitor.CurrentValue;
                var stripeSettings = _stripeSettingsMonitor.CurrentValue;
                var emailSettings = _emailSettingsMonitor.CurrentValue;
                var rateLimitSettings = _rateLimitSettingsMonitor.CurrentValue;

                var defaultSettings = new AdminSettings
                {
                    General = new GeneralSettings
                    {
                        AppName = "Office AI - Batu Lab",
                        Version = "1.0.0",
                        TimeZone = "Europe/Istanbul",
                        Language = "tr",
                        MaintenanceMode = false
                    },
                    Users = new UserSettings
                    {
                        AllowRegistration = true,
                        EmailVerificationRequired = authSettings.RequireEmailVerification,
                        AutoTrialLicense = true,
                        MinimumPasswordLength = authSettings.PasswordMinLength,
                        MaxLoginAttempts = authSettings.MaxLoginAttempts
                    },
                    Security = new SecuritySettings
                    {
                        TokenExpiryHours = authSettings.TokenExpiryHours,
                        RefreshTokenDurationDays = 30,
                        EnableRateLimiting = rateLimitSettings.EnableRateLimiting,
                        GeneralRateLimit = rateLimitSettings.GeneralLimit,
                        AuthRateLimit = rateLimitSettings.AuthLimit,
                        PaymentRateLimit = rateLimitSettings.PaymentLimit
                    },
                    Notifications = new NotificationSettings
                    {
                        SmtpHost = emailSettings.SmtpHost,
                        SmtpPort = emailSettings.SmtpPort,
                        SmtpUsername = emailSettings.Username,
                        SmtpPassword = emailSettings.Password,
                        FromEmail = emailSettings.FromEmail,
                        FromName = emailSettings.FromName,
                        EnableSsl = emailSettings.EnableSsl,
                        EnabledNotificationTypes = new List<string> { "NewUser", "PaymentSuccess", "PaymentFailed", "LicenseExpired" }
                    },
                    Payment = new PaymentSettings
                    {
                        StripePublishableKey = stripeSettings.PublishableKey,
                        StripeSecretKey = stripeSettings.SecretKey,
                        StripeWebhookSecret = stripeSettings.WebhookSecret,
                        MonthlyPlanPrice = licenseSettings.MonthlyPriceUsd,
                        YearlyPlanPrice = licenseSettings.YearlyPriceUsd,
                        LifetimePlanPrice = licenseSettings.LifetimePriceUsd,
                        TrialDurationDays = licenseSettings.TrialDurationDays
                    },
                    Api = new ApiSettings
                    {
                        ClaudeApiKey = _configuration["Claude:ApiKey"] ?? "sk-ant-...",
                        ClaudeModel = _configuration["Claude:Model"] ?? "claude-3-sonnet",
                        GeminiApiKey = _configuration["Gemini:ApiKey"] ?? "AIza...",
                        GroqApiKey = _configuration["Groq:ApiKey"] ?? "gsk_..."
                    }
                };

                _cachedSettings = defaultSettings;
                
                // Save default settings to file
                SaveSettingsToFile(defaultSettings);

                return Task.FromResult(ApiResponse<AdminSettings>.SuccessResult(_cachedSettings));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin settings");
            return Task.FromResult(ApiResponse<AdminSettings>.ErrorResult("Failed to retrieve settings"));
        }
    }

    public Task<ApiResponse<AdminSettings>> UpdateSettingsAsync(AdminSettings settings)
    {
        try
        {
            lock (_settingsLock)
            {
                // Validate settings
                if (settings == null)
                {
                    return Task.FromResult(ApiResponse<AdminSettings>.ErrorResult("Settings cannot be null"));
                }

                // Perform validation
                var validationErrors = ValidateSettings(settings);
                if (validationErrors.Any())
                {
                    return Task.FromResult(ApiResponse<AdminSettings>.ErrorResult("Validation failed", validationErrors));
                }

                // Update cached settings
                _cachedSettings = settings;

                // Save to file
                SaveSettingsToFile(settings);

                _logger.LogInformation("Admin settings updated successfully. New cached settings: {@Settings}", _cachedSettings);
                return Task.FromResult(ApiResponse<AdminSettings>.SuccessResult(settings, "Settings updated successfully"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating admin settings");
            return Task.FromResult(ApiResponse<AdminSettings>.ErrorResult("Failed to update settings"));
        }
    }

    private void SaveSettingsToFile(AdminSettings settings)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonContent = JsonSerializer.Serialize(settings, jsonOptions);
            File.WriteAllText(_settingsFilePath, jsonContent);
            
            _logger.LogDebug("Settings saved to file: {FilePath}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to file");
        }
    }

    private List<string> ValidateSettings(AdminSettings settings)
    {
        var errors = new List<string>();

        // General settings validation
        if (string.IsNullOrWhiteSpace(settings.General.AppName))
        {
            errors.Add("App name cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(settings.General.Version))
        {
            errors.Add("Version cannot be empty");
        }

        // User settings validation
        if (settings.Users.MinimumPasswordLength < 4 || settings.Users.MinimumPasswordLength > 20)
        {
            errors.Add("Minimum password length must be between 4 and 20");
        }

        if (settings.Users.MaxLoginAttempts < 3 || settings.Users.MaxLoginAttempts > 10)
        {
            errors.Add("Max login attempts must be between 3 and 10");
        }

        // Security settings validation
        if (settings.Security.TokenExpiryHours < 1 || settings.Security.TokenExpiryHours > 168)
        {
            errors.Add("Token expiry hours must be between 1 and 168 (1 week)");
        }

        if (settings.Security.RefreshTokenDurationDays < 1 || settings.Security.RefreshTokenDurationDays > 90)
        {
            errors.Add("Refresh token duration must be between 1 and 90 days");
        }

        // Rate limiting validation
        if (settings.Security.EnableRateLimiting)
        {
            if (settings.Security.GeneralRateLimit < 10 || settings.Security.GeneralRateLimit > 1000)
            {
                errors.Add("General rate limit must be between 10 and 1000");
            }

            if (settings.Security.AuthRateLimit < 5 || settings.Security.AuthRateLimit > 100)
            {
                errors.Add("Auth rate limit must be between 5 and 100");
            }

            if (settings.Security.PaymentRateLimit < 1 || settings.Security.PaymentRateLimit > 50)
            {
                errors.Add("Payment rate limit must be between 1 and 50");
            }
        }

        // Notification settings validation
        if (!string.IsNullOrWhiteSpace(settings.Notifications.SmtpHost))
        {
            if (settings.Notifications.SmtpPort < 1 || settings.Notifications.SmtpPort > 65535)
            {
                errors.Add("SMTP port must be between 1 and 65535");
            }

            if (string.IsNullOrWhiteSpace(settings.Notifications.FromEmail))
            {
                errors.Add("From email is required when SMTP host is configured");
            }
        }

        // Payment settings validation
        if (settings.Payment.MonthlyPlanPrice < 0 || settings.Payment.MonthlyPlanPrice > 1000)
        {
            errors.Add("Monthly plan price must be between 0 and 1000 USD");
        }

        if (settings.Payment.YearlyPlanPrice < 0 || settings.Payment.YearlyPlanPrice > 10000)
        {
            errors.Add("Yearly plan price must be between 0 and 10000 USD");
        }

        if (settings.Payment.LifetimePlanPrice < 0 || settings.Payment.LifetimePlanPrice > 50000)
        {
            errors.Add("Lifetime plan price must be between 0 and 50000 USD");
        }

        if (settings.Payment.TrialDurationDays < 1 || settings.Payment.TrialDurationDays > 30)
        {
            errors.Add("Trial duration must be between 1 and 30 days");
        }

        return errors;
    }

    /// <summary>
    /// Clear cached settings (for testing or forcing reload)
    /// </summary>
    public void ClearCache()
    {
        lock (_settingsLock)
        {
            _cachedSettings = null;
        }
    }

    /// <summary>
    /// Get current cached settings (for internal use)
    /// </summary>
    public AdminSettings? GetCachedSettings()
    {
        lock (_settingsLock)
        {
            return _cachedSettings;
        }
    }
}