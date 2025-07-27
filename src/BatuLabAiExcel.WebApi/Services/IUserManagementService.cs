using BatuLabAiExcel.WebApi.Models;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// User management service interface for API operations
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Validate license key and return license information
    /// </summary>
    Task<LicenseValidationResponse> ValidateLicenseKeyAsync(string licenseKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update user license from payment
    /// </summary>
    Task<ApiResponse<ApiLicenseInfo>> UpdateLicenseFromPaymentAsync(Guid userId, string licenseType, string subscriptionId = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's current license information
    /// </summary>
    Task<ApiResponse<ApiLicenseInfo>> GetUserLicenseAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extend trial license
    /// </summary>
    Task<ApiResponse<ApiLicenseInfo>> ExtendTrialAsync(Guid userId, int days, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel user's subscription
    /// </summary>
    Task<ApiResponse> CancelSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);
}