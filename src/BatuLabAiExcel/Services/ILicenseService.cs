using BatuLabAiExcel.Models.DTOs;
using BatuLabAiExcel.Models.Entities;

namespace BatuLabAiExcel.Services;

/// <summary>
/// License management service interface
/// </summary>
public interface ILicenseService
{
    /// <summary>
    /// Create a 1-day trial license for new user
    /// </summary>
    Task<License?> CreateTrialLicenseAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate user license (local + remote)
    /// </summary>
    Task<LicenseValidationResponse> ValidateLicenseAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's active license
    /// </summary>
    Task<License?> GetActiveLicenseAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update license based on successful payment
    /// </summary>
    Task<License?> UpdateLicenseFromPaymentAsync(Guid userId, LicenseType type, string stripeSubscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if license is expired and update status
    /// </summary>
    Task UpdateExpiredLicensesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get license expiry information
    /// </summary>
    Task<(bool IsExpired, DateTime? ExpiryDate, int RemainingDays)> GetLicenseExpiryInfoAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel license (for subscription cancellation)
    /// </summary>
    Task<bool> CancelLicenseAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check startup license validation
    /// </summary>
    Task<bool> ValidateStartupLicenseAsync(Guid userId, CancellationToken cancellationToken = default);
}