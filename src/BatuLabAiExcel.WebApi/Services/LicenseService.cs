using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BatuLabAiExcel.WebApi.Data;
using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Models.Entities;
using BatuLabAiExcel.WebApi.Models.DTOs;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// License service implementation for Web API
/// </summary>
public class LicenseService : ILicenseService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LicenseService> _logger;
    private readonly AppConfiguration.LicenseSettings _licenseSettings;

    public LicenseService(
        AppDbContext context,
        ILogger<LicenseService> logger,
        IOptions<AppConfiguration.LicenseSettings> licenseSettings)
    {
        _context = context;
        _logger = logger;
        _licenseSettings = licenseSettings.Value;
    }

    public async Task<Result<License>> CreateTrialLicenseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating trial license for user: {UserId}", userId);

            var existingLicense = await _context.Licenses
                .FirstOrDefaultAsync(l => l.UserId == userId && l.IsActive, cancellationToken);

            if (existingLicense != null)
            {
                _logger.LogWarning("User already has active license: {UserId}", userId);
                return Result<License>.Success(existingLicense);
            }

            var license = new License
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = LicenseType.Trial,
                LicenseKey = GenerateLicenseKey(LicenseType.Trial),
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddDays(_licenseSettings.TrialDurationDays),
                CreatedAt = DateTime.UtcNow,
            };

            _context.Licenses.Add(license);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Trial license created successfully for user: {UserId}", userId);
            return Result<License>.Success(license);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating trial license for user: {UserId}", userId);
            return Result<License>.Failure("Failed to create trial license");
        }
    }

    public async Task<License?> UpdateLicenseFromPaymentAsync(Guid userId, LicenseType licenseType, string subscriptionId = "", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating license from payment for user: {UserId}, Type: {LicenseType}", userId, licenseType);

            // Deactivate existing licenses
            var existingLicenses = await _context.Licenses
                .Where(l => l.UserId == userId && l.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingLicenses)
            {
                existing.IsActive = false;
            }

            // Create new license
            var license = new License
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = licenseType,
                LicenseKey = GenerateLicenseKey(licenseType),
                IsActive = true,
                ExpiresAt = CalculateExpiryDate(licenseType),
                StripeSubscriptionId = subscriptionId,
                CreatedAt = DateTime.UtcNow,
            };

            _context.Licenses.Add(license);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("License updated successfully for user: {UserId}", userId);
            return license;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating license from payment for user: {UserId}", userId);
            return null;
        }
    }

    public async Task<Result> CancelLicenseAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling license for user: {UserId}", userId);

            var activeLicenses = await _context.Licenses
                .Where(l => l.UserId == userId && l.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var license in activeLicenses)
            {
                license.IsActive = false;
                license.CancellationReason = reason;
                license.CancelledAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("License cancelled successfully for user: {UserId}", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling license for user: {UserId}", userId);
            return Result.Failure("Failed to cancel license");
        }
    }

    public async Task<License?> GetActiveLicenseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var license = await _context.Licenses
                .Where(l => l.UserId == userId && l.IsActive)
                .OrderByDescending(l => l.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            return license;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active license for user: {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> ValidateLicenseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var license = await GetActiveLicenseAsync(userId, cancellationToken);
            
            if (license == null || !license.IsActive)
            {
                return false;
            }

            if (license.ExpiresAt.HasValue && license.ExpiresAt.Value <= DateTime.UtcNow)
            {
                // Mark license as expired
                license.IsActive = false;
                await _context.SaveChangesAsync(cancellationToken);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating license for user: {UserId}", userId);
            return false;
        }
    }

    private static string GenerateLicenseKey(LicenseType licenseType)
    {
        var prefix = licenseType switch
        {
            LicenseType.Trial => "TRIAL",
            LicenseType.Monthly => "MONTHLY",
            LicenseType.Yearly => "YEARLY",
            LicenseType.Lifetime => "LIFETIME",
            _ => "UNKNOWN"
        };

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var randomPart = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

        return $"OFFICE-AI-{prefix}-{timestamp}-{randomPart}";
    }

    private DateTime? CalculateExpiryDate(LicenseType licenseType)
    {
        return licenseType switch
        {
            LicenseType.Trial => DateTime.UtcNow.AddDays(_licenseSettings.TrialDurationDays),
            LicenseType.Monthly => DateTime.UtcNow.AddMonths(1),
            LicenseType.Yearly => DateTime.UtcNow.AddYears(1),
            LicenseType.Lifetime => null, // Never expires
            _ => DateTime.UtcNow.AddDays(1) // Default to 1 day
        };
    }

    public async Task<Result<License>> CreateLicenseAsync(Guid userId, LicenseType licenseType, Guid? paymentId = null, string? customerId = null, string? subscriptionId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating license for user: {UserId}, type: {LicenseType}", userId, licenseType);

            // Deactivate any existing active licenses
            var existingLicenses = await _context.Licenses
                .Where(l => l.UserId == userId && l.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingLicenses)
            {
                existing.IsActive = false;
            }

            // Create new license
            var license = new License
            {
                UserId = userId,
                Type = licenseType,
                Status = LicenseStatus.Active,
                LicenseKey = GenerateLicenseKey(licenseType),
                StartDate = DateTime.UtcNow,
                ExpiresAt = CalculateExpiryDate(licenseType),
                IsActive = true,
                StripeCustomerId = customerId,
                StripeSubscriptionId = subscriptionId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Licenses.Add(license);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("License created successfully: {LicenseId}", license.Id);
            return Result<License>.Success(license);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating license for user: {UserId}", userId);
            return Result<License>.Failure("Failed to create license");
        }
    }
}