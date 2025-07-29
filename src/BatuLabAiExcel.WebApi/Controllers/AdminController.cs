using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using BatuLabAiExcel.WebApi.Data;
using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Models.Entities;
using BatuLabAiExcel.WebApi.Services;

namespace BatuLabAiExcel.WebApi.Controllers;

/// <summary>
/// Admin panel controller for dashboard and management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("General")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AdminController> _logger;
    private readonly IAuthenticationService _authService;
    private readonly ILicenseService _licenseService;
    private readonly IPaymentService _paymentService;
    private readonly IAdminSettingsService _adminSettingsService;
    private readonly INotificationService _notificationService;

    public AdminController(AppDbContext context, ILogger<AdminController> logger, IAuthenticationService authService, ILicenseService licenseService, IPaymentService paymentService, IAdminSettingsService adminSettingsService, INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _authService = authService;
        _licenseService = licenseService;
        _paymentService = paymentService;
        _adminSettingsService = adminSettingsService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("dashboard/stats")]
    public async Task<ActionResult<ApiResponse<object>>> GetDashboardStats()
    {
        try
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
            var totalLicenses = await _context.Licenses.CountAsync();
            var activeLicenses = await _context.Licenses.CountAsync(l => l.IsActive);
            var totalRevenue = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Succeeded)
                .SumAsync(p => p.Amount);

            var stats = new
            {
                totalUsers,
                activeUsers,
                totalLicenses,
                activeLicenses,
                totalRevenue = totalRevenue,
                revenueGrowth = 12.5, // Mock data
                userGrowth = 8.3 // Mock data
            };

            return Ok(ApiResponse<object>.SuccessResult(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to get dashboard statistics"));
        }
    }

    /// <summary>
    /// Get system status
    /// </summary>
    [HttpGet("system/status")]
    public ActionResult<ApiResponse<object>> GetSystemStatus()
    {
        try
        {
            var status = new
            {
                database = new { status = "healthy", responseTime = "12ms" },
                api = new { status = "healthy", responseTime = "5ms" },
                storage = new { status = "healthy", usage = "45%" },
                memory = new { status = "healthy", usage = "62%" },
                lastUpdated = DateTime.UtcNow
            };

            return Ok(ApiResponse<object>.SuccessResult(status));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system status");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to get system status"));
        }
    }

    /// <summary>
    /// Get users with pagination and filtering
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<object>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = _context.Users.Include(u => u.Licenses).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Email.Contains(search) || 
                                        u.FirstName.Contains(search) || 
                                        u.LastName.Contains(search));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    FullName = u.FirstName + " " + u.LastName,
                    u.IsActive,
                    u.CreatedAt,
                    u.LastLoginAt,
                    LicenseCount = u.Licenses.Count,
                    ActiveLicense = u.Licenses.FirstOrDefault(l => l.IsActive)
                })
                .ToListAsync();

            var result = new
            {
                data = users,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(ApiResponse<object>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to get users"));
        }
    }

    /// <summary>
    /// Get licenses with pagination and filtering
    /// </summary>
    [HttpGet("licenses")]
    public async Task<ActionResult<ApiResponse<object>>> GetLicenses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] LicenseType? type = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            var query = _context.Licenses.Include(l => l.User).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(l => l.LicenseKey.Contains(search) || 
                                        l.User.Email.Contains(search));
            }

            if (type.HasValue)
            {
                query = query.Where(l => l.Type == type.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(l => l.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            var licenses = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.LicenseKey,
                    l.Type,
                    l.Status,
                    l.IsActive,
                    l.StartDate,
                    l.ExpiresAt,
                    l.CreatedAt,
                    User = new
                    {
                        l.User.Id,
                        l.User.Email,
                        l.User.FirstName,
                        l.User.LastName,
                        FullName = l.User.FirstName + " " + l.User.LastName
                    }
                })
                .ToListAsync();

            var result = new
            {
                data = licenses,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(ApiResponse<object>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting licenses");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to get licenses"));
        }
    }

    /// <summary>
    /// Get payments with pagination and filtering
    /// </summary>
    [HttpGet("payments")]
    public async Task<ActionResult<ApiResponse<object>>> GetPayments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] PaymentStatus? status = null)
    {
        try
        {
            var query = _context.Payments.Include(p => p.User).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.StripePaymentIntentId.Contains(search) || 
                                        p.User.Email.Contains(search));
            }

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            var totalCount = await query.CountAsync();
            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.StripePaymentIntentId,
                    p.Amount,
                    p.Currency,
                    p.Status,
                    p.LicenseType,
                    p.Description,
                    p.CreatedAt,
                    User = new
                    {
                        p.User.Id,
                        p.User.Email,
                        p.User.FirstName,
                        p.User.LastName,
                        FullName = p.User.FirstName + " " + p.User.LastName
                    }
                })
                .ToListAsync();

            var result = new
            {
                data = payments,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(ApiResponse<object>.SuccessResult(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to get payments"));
        }
    }

    /// <summary>
    /// Refund a payment
    /// </summary>
    [HttpPost("payments/{id}/refund")]
    public async Task<ActionResult<ApiResponse<object>>> RefundPayment(Guid id, [FromBody] string? reason = null)
    {
        try
        {
            var result = await _paymentService.RefundPaymentAsync(id, reason ?? "Admin initiated refund");
            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<object>.ErrorResult(result.Error ?? "Failed to refund payment"));
            }
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Payment refunded successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding payment {PaymentId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to refund payment"));
        }
    }

    /// <summary>
    /// Get revenue analytics
    /// </summary>
    [HttpGet("analytics/revenue")]
    public async Task<ActionResult<ApiResponse<object>>> GetRevenueAnalytics([FromQuery] string period = "month")
    {
        try
        {
            var now = DateTime.UtcNow;
            DateTime startDate = period switch
            {
                "week" => now.AddDays(-7),
                "month" => now.AddMonths(-1),
                "year" => now.AddYears(-1),
                _ => now.AddMonths(-1)
            };

            // Get all payments in memory first to avoid SQLite Date issues
            var allPayments = await _context.Payments
                .Where(p => p.Status == PaymentStatus.Succeeded && p.CreatedAt >= startDate)
                .Select(p => new { p.CreatedAt, p.Amount })
                .ToListAsync();

            // Group by date in memory
            var payments = allPayments
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    value = g.Sum(p => p.Amount)
                })
                .OrderBy(x => x.date)
                .ToList();

            // Fill missing dates with 0 values
            var data = new List<object>();
            var currentDate = startDate.Date;
            while (currentDate <= now.Date)
            {
                var existingData = payments.FirstOrDefault(p => p.date == currentDate.ToString("yyyy-MM-dd"));
                data.Add(new
                {
                    date = currentDate.ToString("yyyy-MM-dd"),
                    value = existingData?.value ?? 0
                });
                currentDate = currentDate.AddDays(1);
            }

            return Ok(ApiResponse<object>.SuccessResult(new { data }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting revenue analytics");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to get revenue analytics"));
        }
    }

    /// <summary>
    /// Get user growth analytics
    /// </summary>
    [HttpGet("analytics/users")]
    public async Task<ActionResult<ApiResponse<object>>> GetUserGrowthAnalytics([FromQuery] string period = "month")
    {
        try
        {
            var now = DateTime.UtcNow;
            DateTime startDate = period switch
            {
                "week" => now.AddDays(-7),
                "month" => now.AddMonths(-1),
                "year" => now.AddYears(-1),
                _ => now.AddMonths(-1)
            };

            // Get all users in memory first to avoid SQLite Date issues
            var allUsers = await _context.Users
                .Where(u => u.CreatedAt >= startDate)
                .Select(u => new { u.CreatedAt })
                .ToListAsync();

            // Group by date in memory
            var users = allUsers
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    value = g.Count()
                })
                .OrderBy(x => x.date)
                .ToList();

            // Fill missing dates with 0 values
            var data = new List<object>();
            var currentDate = startDate.Date;
            while (currentDate <= now.Date)
            {
                var existingData = users.FirstOrDefault(u => u.date == currentDate.ToString("yyyy-MM-dd"));
                data.Add(new
                {
                    date = currentDate.ToString("yyyy-MM-dd"),
                    value = existingData?.value ?? 0
                });
                currentDate = currentDate.AddDays(1);
            }

            return Ok(ApiResponse<object>.SuccessResult(new { data }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user growth analytics");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to get user growth analytics"));
        }
    }

    /// <summary>
    /// Get license distribution analytics
    /// </summary>
    [HttpGet("analytics/license-distribution")]
    public async Task<ActionResult<ApiResponse<object>>> GetLicenseDistribution()
    {
        try
        {
            // Get all licenses and calculate in memory to avoid SQLite issues
            var allLicenses = await _context.Licenses.ToListAsync();
            var totalCount = allLicenses.Count;

            if (totalCount == 0)
            {
                return Ok(ApiResponse<object>.SuccessResult(new List<object>()));
            }

            var distribution = allLicenses
                .GroupBy(l => l.Type)
                .Select(g => new
                {
                    type = g.Key.ToString(),
                    count = g.Count(),
                    percentage = Math.Round((double)g.Count() * 100 / totalCount, 2)
                })
                .ToList();

            return Ok(ApiResponse<object>.SuccessResult(distribution));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting license distribution");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to get license distribution"));
        }
    }

    /// <summary>
    /// Send notification to specific users
    /// </summary>
    [HttpPost("notifications/send")]
    public async Task<ActionResult<ApiResponse<object>>> SendNotification([FromBody] SendNotificationRequest request)
    {
        try
        {
            foreach (var userId in request.UserIds)
            {
                await _notificationService.SendNotificationAsync(userId, "Admin Notification", request.Message, request.Type);
            }
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Notification sent successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to send notification"));
        }
    }

    /// <summary>
    /// Broadcast notification to all users
    /// </summary>
    [HttpPost("notifications/broadcast")]
    public async Task<ActionResult<ApiResponse<object>>> BroadcastNotification([FromBody] BroadcastNotificationRequest request)
    {
        try
        {
            await _notificationService.BroadcastNotificationAsync("Admin Broadcast", request.Message, request.Type);
            return Ok(ApiResponse<object>.SuccessResult(new { }, "Notification broadcast successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting notification");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to broadcast notification"));
        }
    }

    /// <summary>
    /// Get all admin settings
    /// </summary>
    [HttpGet("settings")]
    public async Task<ActionResult<ApiResponse<AdminSettings>>> GetSettings()
    {
        try
        {
            _logger.LogInformation("Getting admin settings request received");
            var settings = await _adminSettingsService.GetSettingsAsync();
            _logger.LogInformation("Admin settings retrieved: {@Settings}", settings);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin settings");
            return StatusCode(500, ApiResponse<AdminSettings>.ErrorResult("Failed to get settings"));
        }
    }

    /// <summary>
    /// Update admin settings
    /// </summary>
    [HttpPut("settings")]
    public async Task<ActionResult<ApiResponse<AdminSettings>>> UpdateSettings([FromBody] AdminSettings request)
    {
        try
        {
            _logger.LogInformation("Received settings update request: {@Settings}", request);
            var result = await _adminSettingsService.UpdateSettingsAsync(request);
            _logger.LogInformation("Settings update result: {@Result}", result);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating admin settings");
            return StatusCode(500, ApiResponse<AdminSettings>.ErrorResult("Failed to update settings"));
        }
    }

    /// <summary>
    /// Update user status
    /// </summary>
    [HttpPost("users/{id}/suspend")]
    public async Task<ActionResult<ApiResponse<object>>> SuspendUser(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("User not found"));
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(new { }, "User suspended successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending user {UserId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to suspend user"));
        }
    }

    /// <summary>
    /// Reactivate user
    /// </summary>
    [HttpPost("users/{id}/unsuspend")]
    public async Task<ActionResult<ApiResponse<object>>> UnsuspendUser(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("User not found"));
            }

            user.IsActive = true;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(new { }, "User reactivated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating user {UserId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to reactivate user"));
        }
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost("users")]
    public async Task<ActionResult<ApiResponse<object>>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request.Email, request.Password, $"{request.FirstName} {request.LastName}");
            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<object>.ErrorResult(result.Error ?? "Failed to create user"));
            }
            return Ok(ApiResponse<object>.SuccessResult(result.Data, "User created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to create user"));
        }
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("users/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("User not found"));
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                user.Email = request.Email;
            }
            if (!string.IsNullOrEmpty(request.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }
            if (!string.IsNullOrEmpty(request.FirstName))
            {
                user.FirstName = request.FirstName;
            }
            if (!string.IsNullOrEmpty(request.LastName))
            {
                user.LastName = request.LastName;
            }
            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResult(user, "User updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to update user"));
        }
    }

    /// <summary>
    /// Delete a user permanently
    /// </summary>
    [HttpDelete("users/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(Guid id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("User not found"));
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResult(new { }, "User deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to delete user"));
        }
    }

    /// <summary>
    /// Revoke license
    /// </summary>
    [HttpPost("licenses/{id}/revoke")]
    public async Task<ActionResult<ApiResponse<object>>> RevokeLicense(Guid id)
    {
        try
        {
            var license = await _context.Licenses.FindAsync(id);
            if (license == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("License not found"));
            }

            license.IsActive = false;
            license.Status = LicenseStatus.Cancelled;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(new { }, "License revoked successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking license {LicenseId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to revoke license"));
        }
    }

    /// <summary>
    /// Extend license duration
    /// </summary>
    [HttpPost("licenses/{id}/extend")]
    public async Task<ActionResult<ApiResponse<object>>> ExtendLicense(Guid id, [FromBody] ExtendLicenseRequest request)
    {
        try
        {
            var license = await _context.Licenses.FindAsync(id);
            if (license == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("License not found"));
            }

            if (!license.IsActive)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Cannot extend inactive license"));
            }

            // Extend the license
            if (license.ExpiresAt.HasValue)
            {
                license.ExpiresAt = license.ExpiresAt.Value.AddDays(request.Days);
            }
            else
            {
                // If it's a lifetime license, we can't extend it
                return BadRequest(ApiResponse<object>.ErrorResult("Cannot extend lifetime license"));
            }

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(new { 
                newExpiryDate = license.ExpiresAt,
                daysAdded = request.Days
            }, $"License extended by {request.Days} days"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending license {LicenseId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to extend license"));
        }
    }

    /// <summary>
    /// Delete license permanently
    /// </summary>
    [HttpDelete("licenses/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteLicense(Guid id)
    {
        try
        {
            var license = await _context.Licenses
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == id);
                
            if (license == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("License not found"));
            }

            // Check if there are any payments associated with this license
            var hasPayments = await _context.Payments.AnyAsync(p => p.LicenseId == id);
            if (hasPayments)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Cannot delete license with associated payments. Revoke it instead."));
            }

            _context.Licenses.Remove(license);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(new { }, "License deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting license {LicenseId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to delete license"));
        }
    }

    /// <summary>
    /// Create a new license
    /// </summary>
    [HttpPost("licenses")]
    public async Task<ActionResult<ApiResponse<object>>> CreateLicense([FromBody] CreateLicenseRequest request)
    {
        try
        {
            var result = await _licenseService.CreateLicenseAsync(request.UserId, request.Type, null, null, null, CancellationToken.None);
            if (!result.IsSuccess)
            {
                return BadRequest(ApiResponse<object>.ErrorResult(result.Error ?? "Failed to create license"));
            }
            return Ok(ApiResponse<object>.SuccessResult(result.Data, "License created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating license");
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to create license"));
        }
    }

    /// <summary>
    /// Update an existing license
    /// </summary>
    [HttpPut("licenses/{id}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateLicense(Guid id, [FromBody] UpdateLicenseRequest request)
    {
        try
        {
            var license = await _context.Licenses.FindAsync(id);
            if (license == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("License not found"));
            }

            if (request.Type.HasValue)
            {
                license.Type = request.Type.Value;
            }
            if (request.Status.HasValue)
            {
                license.Status = request.Status.Value;
            }
            if (request.IsActive.HasValue)
            {
                license.IsActive = request.IsActive.Value;
            }
            if (request.ExpiresAt.HasValue)
            {
                license.ExpiresAt = request.ExpiresAt.Value;
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResult(license, "License updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating license {LicenseId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResult("Failed to update license"));
        }
    }
}