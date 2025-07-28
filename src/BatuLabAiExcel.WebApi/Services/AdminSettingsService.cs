using BatuLabAiExcel.WebApi.Models;
using Microsoft.Extensions.Options;

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
    private readonly ILogger<AdminSettingsService> _logger;

    public AdminSettingsService(
        IOptionsMonitor<AppConfiguration.AuthenticationSettings> authSettingsMonitor,
        IOptionsMonitor<AppConfiguration.LicenseSettings> licenseSettingsMonitor,
        IOptionsMonitor<AppConfiguration.StripeSettings> stripeSettingsMonitor,
        IOptionsMonitor<AppConfiguration.EmailSettings> emailSettingsMonitor,
        IOptionsMonitor<RateLimitSettings> rateLimitSettingsMonitor,
        ILogger<AdminSettingsService> logger)
    {
        _authSettingsMonitor = authSettingsMonitor;
        _licenseSettingsMonitor = licenseSettingsMonitor;
        _stripeSettingsMonitor = stripeSettingsMonitor;
        _emailSettingsMonitor = emailSettingsMonitor;
        _rateLimitSettingsMonitor = rateLimitSettingsMonitor;
        _logger = logger;
    }

    public Task<ApiResponse<AdminSettings>> GetSettingsAsync()
    {
        try
        {
            var authSettings = _authSettingsMonitor.CurrentValue;
            var licenseSettings = _licenseSettingsMonitor.CurrentValue;
            var stripeSettings = _stripeSettingsMonitor.CurrentValue;
            var emailSettings = _emailSettingsMonitor.CurrentValue;
            var rateLimitSettings = _rateLimitSettingsMonitor.CurrentValue;

            var settings = new AdminSettings
            {
                General = new GeneralSettings
                {
                    // These would typically come from a separate config or DB
                    AppName = "Office AI - Batu Lab",
                    Version = "1.0.0",
                    TimeZone = "Europe/Istanbul",
                    Language = "tr",
                    MaintenanceMode = false // Placeholder
                },
                Users = new UserSettings
                {
                    AllowRegistration = true, // Placeholder
                    EmailVerificationRequired = authSettings.RequireEmailVerification,
                    AutoTrialLicense = true, // Placeholder
                    MinimumPasswordLength = authSettings.PasswordMinLength,
                    MaxLoginAttempts = authSettings.MaxLoginAttempts
                },
                Security = new SecuritySettings
                {
                    TokenExpiryHours = authSettings.TokenExpiryHours,
                    RefreshTokenDurationDays = 30, // Placeholder
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
                    EnabledNotificationTypes = new List<string> { "NewUser", "PaymentSuccess" } // Placeholder
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
                    ClaudeApiKey = "sk-ant-...", // Placeholder
                    ClaudeModel = "claude-3-sonnet", // Placeholder
                    GeminiApiKey = "AIza...", // Placeholder
                    GroqApiKey = "gsk_..." // Placeholder
                }
            };

            return Task.FromResult(ApiResponse<AdminSettings>.SuccessResult(settings));
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
            // In a real application, you would update the configuration source (e.g., appsettings.json, database)
            // directly here. Since I cannot modify appsettings.json dynamically, this will be a mock update.
            _logger.LogInformation("Attempting to update admin settings.");

            // Example of how you might update a setting if it were mutable:
            // _authSettingsMonitor.CurrentValue.RequireEmailVerification = settings.Users.EmailVerificationRequired;

            return Task.FromResult(ApiResponse<AdminSettings>.SuccessResult(settings, "Settings updated successfully (mock)"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating admin settings");
            return Task.FromResult(ApiResponse<AdminSettings>.ErrorResult("Failed to update settings"));
        }
    }
}