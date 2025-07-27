namespace BatuLabAiExcel.WebApi.Models;

/// <summary>
/// Application configuration settings for Web API
/// </summary>
public class AppConfiguration
{
    public DatabaseSettings Database { get; set; } = new();
    public AuthenticationSettings Authentication { get; set; } = new();
    public LicenseSettings License { get; set; } = new();
    public StripeSettings Stripe { get; set; } = new();
    public EmailSettings Email { get; set; } = new();

    public class DatabaseSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public bool EnableAutoMigration { get; set; } = true;
        public int CommandTimeout { get; set; } = 120;
    }

    public class AuthenticationSettings
    {
        public string JwtSecretKey { get; set; } = string.Empty;
        public int TokenExpiryHours { get; set; } = 24;
        public bool RequireEmailVerification { get; set; } = false;
        public int PasswordMinLength { get; set; } = 6;
        public int MaxLoginAttempts { get; set; } = 5;
        public int LockoutDurationMinutes { get; set; } = 15;
        public string Issuer { get; set; } = "BatuLabAiExcel.WebApi";
        public string Audience { get; set; } = "BatuLabAiExcel.Client";
    }

    public class LicenseSettings
    {
        public int TrialDurationDays { get; set; } = 1;
        public bool EnableRemoteValidation { get; set; } = true;
        public int ValidationIntervalHours { get; set; } = 24;
        public int GracePeriodDays { get; set; } = 3;
        public decimal MonthlyPriceUsd { get; set; } = 29.99m;
        public decimal YearlyPriceUsd { get; set; } = 299.99m;
        public decimal LifetimePriceUsd { get; set; } = 999.99m;
    }

    public class StripeSettings
    {
        public string PublishableKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string MonthlyPriceId { get; set; } = string.Empty;
        public string YearlyPriceId { get; set; } = string.Empty;
        public string LifetimePriceId { get; set; } = string.Empty;
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;

        /// <summary>
        /// Get masked secret key for logging
        /// </summary>
        public string GetMaskedSecretKey()
        {
            if (string.IsNullOrEmpty(SecretKey) || SecretKey.Length < 8)
                return "***";
            
            return $"{SecretKey[..4]}***{SecretKey[^4..]}";
        }
    }

    public class EmailSettings
    {
        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromEmail { get; set; } = "noreply@batulab.com";
        public string FromName { get; set; } = "Office AI - Batu Lab";
        public bool EnableSsl { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Get masked password for logging
        /// </summary>
        public string GetMaskedPassword()
        {
            if (string.IsNullOrEmpty(Password) || Password.Length < 4)
                return "***";
            
            return $"***{Password[^2..]}";
        }
    }
}