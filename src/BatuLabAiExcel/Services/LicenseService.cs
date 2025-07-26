using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BatuLabAiExcel.Data;
using BatuLabAiExcel.Models.DTOs;
using BatuLabAiExcel.Models.Entities;

namespace BatuLabAiExcel.Services;

/// <summary>
/// License management service implementation
/// </summary>
public class LicenseService : ILicenseService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LicenseService> _logger;

    public LicenseService(AppDbContext context, ILogger<LicenseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<License?> CreateTrialLicenseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating trial license for user: {UserId}", userId);

            // Check if user already has any license
            var existingLicense = await _context.Licenses
                .FirstOrDefaultAsync(l => l.UserId == userId, cancellationToken);

            if (existingLicense != null)
            {
                _logger.LogWarning("User already has a license: {UserId}", userId);
                return existingLicense;
            }

            var trialLicense = new License
            {
                UserId = userId,
                Type = LicenseType.Trial,
                Status = LicenseStatus.Active,
                StartDate = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(1), // 1-day trial
                CreatedAt = DateTime.UtcNow,
                PaidAmount = 0m,
                Currency = "USD",
                IsActive = true,
                Notes = "Automatically created 1-day trial license"
            };

            _context.Licenses.Add(trialLicense);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Trial license created successfully for user: {UserId}, License ID: {LicenseId}", userId, trialLicense.Id);
            return trialLicense;
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
            _logger.LogDebug("Validating license for user: {UserId}", userId);

            var license = await GetActiveLicenseAsync(userId, cancellationToken);

            if (license == null)
            {
                return new LicenseValidationResponse
                {
                    IsValid = false,
                    Message = "No active license found",
                    Errors = ["No license found"]
                };
            }

            if (license.IsExpired)
            {
                // Update license status to expired
                license.Status = LicenseStatus.Expired;
                await _context.SaveChangesAsync(cancellationToken);

                return new LicenseValidationResponse
                {
                    IsValid = false,
                    Message = "License has expired",
                    License = LicenseInfo.FromEntity(license),
                    Errors = ["License expired"]
                };
            }

            // Update last validated timestamp
            license.LastValidatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return new LicenseValidationResponse
            {
                IsValid = true,
                Message = $"License valid - {license.RemainingDays} days remaining",
                License = LicenseInfo.FromEntity(license)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating license for user: {UserId}", userId);
            return new LicenseValidationResponse
            {
                IsValid = false,
                Message = "License validation failed",
                Errors = ["Validation error"]
            };
        }
    }

    public async Task<License?> GetActiveLicenseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Licenses
            .Where(l => l.UserId == userId && l.IsActive && l.Status == LicenseStatus.Active)
            .OrderByDescending(l => l.ExpiresAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<License?> UpdateLicenseFromPaymentAsync(Guid userId, LicenseType type, string stripeSubscriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating license from payment for user: {UserId}, Type: {Type}", userId, type);

            // Deactivate existing licenses
            var existingLicenses = await _context.Licenses
                .Where(l => l.UserId == userId && l.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var existingLicense in existingLicenses)
            {
                existingLicense.IsActive = false;
                existingLicense.Status = LicenseStatus.Cancelled;
            }

            // Create new license based on payment
            var startDate = DateTime.UtcNow;
            var expiryDate = type switch
            {
                LicenseType.Monthly => startDate.AddMonths(1),
                LicenseType.Yearly => startDate.AddYears(1),
                LicenseType.Lifetime => startDate.AddYears(100), // Lifetime = 100 years
                _ => startDate.AddDays(1) // Fallback to trial
            };

            var newLicense = new License
            {
                UserId = userId,
                Type = type,
                Status = LicenseStatus.Active,
                StartDate = startDate,
                ExpiresAt = expiryDate,
                CreatedAt = DateTime.UtcNow,
                StripeSubscriptionId = stripeSubscriptionId,
                PaidAmount = GetPriceForLicenseType(type),
                Currency = "USD",
                IsActive = true,
                Notes = $"License upgraded from payment - Stripe Subscription: {stripeSubscriptionId}"
            };

            _context.Licenses.Add(newLicense);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("License updated successfully for user: {UserId}, License ID: {LicenseId}", userId, newLicense.Id);
            return newLicense;
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
            var expiredLicenses = await _context.Licenses
                .Where(l => l.IsActive && l.Status == LicenseStatus.Active && l.ExpiresAt <= DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            if (expiredLicenses.Any())
            {
                _logger.LogInformation("Updating {Count} expired licenses", expiredLicenses.Count);

                foreach (var license in expiredLicenses)
                {
                    license.Status = LicenseStatus.Expired;
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expired licenses");
        }
    }

    public async Task<(bool IsExpired, DateTime? ExpiryDate, int RemainingDays)> GetLicenseExpiryInfoAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var license = await GetActiveLicenseAsync(userId, cancellationToken);

        if (license == null)
            return (true, null, 0);

        return (license.IsExpired, license.ExpiresAt, license.RemainingDays);
    }

    public async Task<bool> CancelLicenseAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var license = await GetActiveLicenseAsync(userId, cancellationToken);

            if (license == null)
                return false;

            license.Status = LicenseStatus.Cancelled;
            license.IsActive = false;
            license.Notes = $"{license.Notes}\nCancelled: {reason}";

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("License cancelled for user: {UserId}, Reason: {Reason}", userId, reason);
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
            // Update expired licenses first
            await UpdateExpiredLicensesAsync(cancellationToken);

            // Validate current license
            var validationResult = await ValidateLicenseAsync(userId, cancellationToken);
            return validationResult.IsValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during startup license validation for user: {UserId}", userId);
            return false;
        }
    }

    private static decimal GetPriceForLicenseType(LicenseType type) => type switch
    {
        LicenseType.Monthly => 29.99m,
        LicenseType.Yearly => 299.99m,
        LicenseType.Lifetime => 999.99m,
        _ => 0m
    };
}