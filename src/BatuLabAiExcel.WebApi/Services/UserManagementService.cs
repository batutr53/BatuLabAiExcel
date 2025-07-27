using BatuLabAiExcel.WebApi.Data;
using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// User management service implementation for API operations
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly AppDbContext _context;
    private readonly ILicenseService _licenseService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        AppDbContext context,
        ILicenseService licenseService,
        IPaymentService paymentService,
        ILogger<UserManagementService> logger)
    {
        _context = context;
        _licenseService = licenseService;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<LicenseValidationResponse> ValidateLicenseKeyAsync(string licenseKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating license key: {LicenseKey}", licenseKey.Substring(0, Math.Min(licenseKey.Length, 10)) + "...");

            var license = await _context.Licenses
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey, cancellationToken);

            if (license == null)
            {
                return new LicenseValidationResponse
                {
                    IsValid = false,
                    Message = "License key not found",
                    Errors = new List<string> { "Invalid license key" }
                };
            }

            if (!license.IsActive)
            {
                return new LicenseValidationResponse
                {
                    IsValid = false,
                    Message = "License is inactive",
                    Errors = new List<string> { "License has been deactivated" }
                };
            }

            if (license.ExpiresAt.HasValue && license.ExpiresAt.Value < DateTime.UtcNow)
            {
                return new LicenseValidationResponse
                {
                    IsValid = false,
                    Message = "License has expired",
                    Errors = new List<string> { "License expired on " + license.ExpiresAt.Value.ToString("yyyy-MM-dd") }
                };
            }

            var daysRemaining = license.ExpiresAt.HasValue
                ? Math.Max(0, (int)(license.ExpiresAt.Value - DateTime.UtcNow).TotalDays)
                : int.MaxValue;

            var licenseInfo = new ApiLicenseInfo
            {
                Id = license.Id,
                Type = license.Type,
                LicenseKey = license.LicenseKey,
                IsActive = license.IsActive,
                ExpiresAt = license.ExpiresAt,
                CreatedAt = license.CreatedAt,
                DaysRemaining = daysRemaining
            };

            _logger.LogInformation("License validation successful for user: {UserId}", license.UserId);

            return new LicenseValidationResponse
            {
                IsValid = true,
                Message = "License is valid",
                License = licenseInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating license key");
            return new LicenseValidationResponse
            {
                IsValid = false,
                Message = "Validation error",
                Errors = new List<string> { "Internal server error" }
            };
        }
    }

    public async Task<ApiResponse<ApiLicenseInfo>> UpdateLicenseFromPaymentAsync(Guid userId, string licenseType, string subscriptionId = "", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating license from payment - User: {UserId}, Type: {LicenseType}", userId, licenseType);

            if (!Enum.TryParse<LicenseType>(licenseType, out var parsedLicenseType))
            {
                return ApiResponse<ApiLicenseInfo>.ErrorResult("Invalid license type", new List<string> { "Unknown license type: " + licenseType });
            }

            var license = await _licenseService.UpdateLicenseFromPaymentAsync(userId, parsedLicenseType, subscriptionId, cancellationToken);

            if (license == null)
            {
                return ApiResponse<ApiLicenseInfo>.ErrorResult("Failed to update license", new List<string> { "License update failed" });
            }

            var daysRemaining = license.ExpiresAt.HasValue
                ? Math.Max(0, (int)(license.ExpiresAt.Value - DateTime.UtcNow).TotalDays)
                : int.MaxValue;

            var licenseInfo = new ApiLicenseInfo
            {
                Id = license.Id,
                Type = license.Type,
                LicenseKey = license.LicenseKey,
                IsActive = license.IsActive,
                ExpiresAt = license.ExpiresAt,
                CreatedAt = license.CreatedAt,
                DaysRemaining = daysRemaining
            };

            _logger.LogInformation("License updated successfully for user: {UserId}", userId);

            return ApiResponse<ApiLicenseInfo>.SuccessResult(licenseInfo, "License updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating license from payment for user: {UserId}", userId);
            return ApiResponse<ApiLicenseInfo>.ErrorResult("Internal server error", new List<string> { "License update service error" });
        }
    }

    public async Task<ApiResponse<ApiLicenseInfo>> GetUserLicenseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var license = await _context.Licenses
                .Where(l => l.UserId == userId && l.IsActive)
                .OrderByDescending(l => l.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (license == null)
            {
                return ApiResponse<ApiLicenseInfo>.ErrorResult("No active license found", new List<string> { "User does not have an active license" });
            }

            var daysRemaining = license.ExpiresAt.HasValue
                ? Math.Max(0, (int)(license.ExpiresAt.Value - DateTime.UtcNow).TotalDays)
                : int.MaxValue;

            var licenseInfo = new ApiLicenseInfo
            {
                Id = license.Id,
                Type = license.Type,
                LicenseKey = license.LicenseKey,
                IsActive = license.IsActive,
                ExpiresAt = license.ExpiresAt,
                CreatedAt = license.CreatedAt,
                DaysRemaining = daysRemaining
            };

            return ApiResponse<ApiLicenseInfo>.SuccessResult(licenseInfo, "License retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user license: {UserId}", userId);
            return ApiResponse<ApiLicenseInfo>.ErrorResult("Internal server error", new List<string> { "License retrieval service error" });
        }
    }

    public async Task<ApiResponse<ApiLicenseInfo>> ExtendTrialAsync(Guid userId, int days, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Extending trial for user: {UserId} by {Days} days", userId, days);

            var license = await _context.Licenses
                .Where(l => l.UserId == userId && l.IsActive && l.Type == LicenseType.Trial)
                .FirstOrDefaultAsync(cancellationToken);

            if (license == null)
            {
                return ApiResponse<ApiLicenseInfo>.ErrorResult("No active trial license found", new List<string> { "User does not have an active trial license" });
            }

            if (license.ExpiresAt.HasValue)
            {
                license.ExpiresAt = license.ExpiresAt.Value.AddDays(days);
            }
            else
            {
                license.ExpiresAt = DateTime.UtcNow.AddDays(days);
            }

            await _context.SaveChangesAsync(cancellationToken);

            var daysRemaining = license.ExpiresAt.HasValue
                ? Math.Max(0, (int)(license.ExpiresAt.Value - DateTime.UtcNow).TotalDays)
                : int.MaxValue;

            var licenseInfo = new ApiLicenseInfo
            {
                Id = license.Id,
                Type = license.Type,
                LicenseKey = license.LicenseKey,
                IsActive = license.IsActive,
                ExpiresAt = license.ExpiresAt,
                CreatedAt = license.CreatedAt,
                DaysRemaining = daysRemaining
            };

            _logger.LogInformation("Trial extended successfully for user: {UserId}", userId);

            return ApiResponse<ApiLicenseInfo>.SuccessResult(licenseInfo, "Trial extended successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending trial for user: {UserId}", userId);
            return ApiResponse<ApiLicenseInfo>.ErrorResult("Internal server error", new List<string> { "Trial extension service error" });
        }
    }

    public async Task<ApiResponse> CancelSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling subscription for user: {UserId}", userId);

            var license = await _context.Licenses
                .Where(l => l.UserId == userId && l.IsActive && !string.IsNullOrEmpty(l.StripeSubscriptionId))
                .FirstOrDefaultAsync(cancellationToken);

            if (license == null)
            {
                return ApiResponse.ErrorResult("No active subscription found", new List<string> { "User does not have an active subscription" });
            }

            var cancelResult = await _paymentService.CancelSubscriptionAsync(license.UserId, cancellationToken);

            if (!cancelResult.IsSuccess)
            {
                return ApiResponse.ErrorResult("Failed to cancel subscription", new List<string> { "Stripe cancellation failed" });
            }

            await _licenseService.CancelLicenseAsync(userId, "Subscription cancelled by user", cancellationToken);

            _logger.LogInformation("Subscription cancelled successfully for user: {UserId}", userId);

            return ApiResponse.SuccessResult("Subscription cancelled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for user: {UserId}", userId);
            return ApiResponse.ErrorResult("Internal server error", new List<string> { "Subscription cancellation service error" });
        }
    }
}