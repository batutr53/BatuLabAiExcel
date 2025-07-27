using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Models.Entities;
using BatuLabAiExcel.WebApi.Models.DTOs;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// License service interface for Web API
/// </summary>
public interface ILicenseService
{
    /// <summary>
    /// Create trial license for new user
    /// </summary>
    Task<Result<License>> CreateTrialLicenseAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create license from payment
    /// </summary>
    Task<Result<License>> CreateLicenseAsync(Guid userId, LicenseType licenseType, Guid? paymentId = null, string? customerId = null, string? subscriptionId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update license from payment
    /// </summary>
    Task<License?> UpdateLicenseFromPaymentAsync(Guid userId, LicenseType licenseType, string subscriptionId = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel license
    /// </summary>
    Task<Result> CancelLicenseAsync(Guid userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's active license
    /// </summary>
    Task<License?> GetActiveLicenseAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate license
    /// </summary>
    Task<bool> ValidateLicenseAsync(Guid userId, CancellationToken cancellationToken = default);
}