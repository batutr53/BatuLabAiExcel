using Microsoft.Extensions.Logging;
using BatuLabAiExcel.Models.DTOs;
using BatuLabAiExcel.Models.Entities;

namespace BatuLabAiExcel.Services;

/// <summary>
/// License service that uses Web API for secure license operations
/// </summary>
public class WebApiLicenseService : ILicenseService
{
    private readonly IWebApiClient _webApiClient;
    private readonly ILogger<WebApiLicenseService> _logger;

    public WebApiLicenseService(IWebApiClient webApiClient, ILogger<WebApiLicenseService> logger)
    {
        _webApiClient = webApiClient;
        _logger = logger;
    }

    public async Task<License?> CreateTrialLicenseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating trial license through Web API for user: {UserId}", userId);

            // Trial licenses are created automatically during registration in the Web API
            // This method is for compatibility but doesn't need to do anything
            await Task.CompletedTask;
            
            _logger.LogInformation("Trial license creation handled by Web API for user: {UserId}", userId);
            return null; // Web API handles this automatically
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating trial license for user: {UserId}", userId);
            return null;
        }
    }

    public async Task<LicenseValidationResponse> ValidateLicenseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating license through Web API for user: {UserId}", userId);

            var licenseResult = await _webApiClient.GetUserLicenseAsync(cancellationToken);
            
            if (!licenseResult.IsSuccess)
            {
                _logger.LogWarning("Failed to get license info for user: {UserId}", userId);
                return new LicenseValidationResponse
                {
                    IsValid = false,
                    Message = licenseResult.Error ?? "Failed to validate license",
                    License = null
                };
            }

            var licenseInfo = licenseResult.Data!;
            var isValid = licenseInfo.IsActive && 
                         (!licenseInfo.ExpiresAt.HasValue || licenseInfo.ExpiresAt.Value > DateTime.UtcNow);

            _logger.LogInformation("License validation completed for user: {UserId}, Valid: {IsValid}", userId, isValid);
            return new LicenseValidationResponse
            {
                IsValid = isValid,
                Message = isValid ? "License is valid" : "License is invalid or expired",
                License = licenseInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating license for user: {UserId}", userId);
            return new LicenseValidationResponse
            {
                IsValid = false,
                Message = "License validation service error",
                License = null
            };
        }
    }

    public async Task<License?> GetActiveLicenseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting active license through Web API for user: {UserId}", userId);

            var licenseResult = await _webApiClient.GetUserLicenseAsync(cancellationToken);
            
            if (!licenseResult.IsSuccess)
            {
                _logger.LogWarning("Failed to get license info for user: {UserId}", userId);
                return null;
            }

            var licenseInfo = licenseResult.Data!;
            
            // Convert LicenseInfo to License entity for compatibility
            var license = new License
            {
                Id = licenseInfo.Id,
                UserId = userId,
                Type = licenseInfo.Type,
                LicenseKey = licenseInfo.LicenseKey,
                IsActive = licenseInfo.IsActive,
                ExpiresAt = licenseInfo.ExpiresAt ?? DateTime.MaxValue,
                CreatedAt = licenseInfo.CreatedAt
            };

            _logger.LogInformation("Active license retrieved for user: {UserId}", userId);
            return license;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active license for user: {UserId}", userId);
            return null;
        }
    }

    public async Task<License?> UpdateLicenseFromPaymentAsync(Guid userId, LicenseType type, string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating license from payment through Web API for user: {UserId}", userId);

            // License updates from payments are handled by the Web API webhooks
            // This method is for compatibility but the actual work is done server-side
            await Task.CompletedTask;
            
            _logger.LogInformation("License update from payment handled by Web API for user: {UserId}", userId);
            return null; // Web API handles this through webhooks
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating license from payment for user: {UserId}", userId);
            return null;
        }
    }

    public async Task UpdateExpiredLicensesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Expired license cleanup handled by Web API");

            // Expired license cleanup is handled by the Web API
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in expired license cleanup");
        }
    }

    public async Task<(bool IsExpired, DateTime? ExpiryDate, int RemainingDays)> GetLicenseExpiryInfoAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting license expiry info through Web API for user: {UserId}", userId);

            var licenseResult = await _webApiClient.GetUserLicenseAsync(cancellationToken);
            
            if (!licenseResult.IsSuccess)
            {
                _logger.LogWarning("Failed to get license info for user: {UserId}", userId);
                return (true, null, 0); // Assume expired if we can't get info
            }

            var licenseInfo = licenseResult.Data!;
            var expiryDate = licenseInfo.ExpiresAt;
            var isExpired = expiryDate.HasValue && expiryDate.Value <= DateTime.UtcNow;
            var remainingDays = licenseInfo.DaysRemaining;

            _logger.LogInformation("License expiry info retrieved for user: {UserId}, Expired: {IsExpired}, Remaining: {RemainingDays}", 
                userId, isExpired, remainingDays);
            
            return (isExpired, expiryDate, remainingDays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting license expiry info for user: {UserId}", userId);
            return (true, null, 0); // Assume expired on error
        }
    }

    public async Task<bool> CancelLicenseAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling license through Web API for user: {UserId}", userId);

            var result = await _webApiClient.CancelSubscriptionAsync(cancellationToken);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to cancel license for user: {UserId}", userId);
                return false;
            }

            _logger.LogInformation("License cancelled successfully for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling license for user: {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ValidateStartupLicenseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating startup license through Web API for user: {UserId}", userId);

            var validationResult = await ValidateLicenseAsync(userId, cancellationToken);
            
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Startup license validation failed for user: {UserId}", userId);
                return false;
            }

            _logger.LogInformation("Startup license validation successful for user: {UserId}", userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating startup license for user: {UserId}", userId);
            return false;
        }
    }
}