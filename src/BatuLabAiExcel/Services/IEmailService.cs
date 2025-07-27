using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send license key email to user after successful payment
    /// </summary>
    Task<Result> SendLicenseKeyEmailAsync(string toEmail, string userName, string licenseKey, string planType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send welcome email to new user
    /// </summary>
    Task<Result> SendWelcomeEmailAsync(string toEmail, string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send license expiration warning email
    /// </summary>
    Task<Result> SendLicenseExpirationWarningAsync(string toEmail, string userName, int daysRemaining, CancellationToken cancellationToken = default);
}