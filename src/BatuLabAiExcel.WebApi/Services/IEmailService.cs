using BatuLabAiExcel.WebApi.Models;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// Email service interface for Web API
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send license key email to user
    /// </summary>
    Task<Result> SendLicenseKeyEmailAsync(string toEmail, string userName, string licenseKey, string planType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send welcome email to new user
    /// </summary>
    Task<Result> SendWelcomeEmailAsync(string toEmail, string userName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send license expiry warning email
    /// </summary>
    Task<Result> SendLicenseExpiryWarningAsync(string toEmail, string userName, int daysRemaining, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send password reset email
    /// </summary>
    Task<Result> SendPasswordResetEmailAsync(string toEmail, string userName, string tempPassword, CancellationToken cancellationToken = default);
}