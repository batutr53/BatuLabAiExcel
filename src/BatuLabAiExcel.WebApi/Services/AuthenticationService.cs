using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using BCrypt.Net;
using BatuLabAiExcel.WebApi.Data;
using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Models.Entities;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// Authentication service implementation for Web API
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly AppDbContext _context;
    private readonly ILicenseService _licenseService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly AppConfiguration.AuthenticationSettings _authSettings;

    public AuthenticationService(
        AppDbContext context,
        ILicenseService licenseService,
        IEmailService emailService,
        ILogger<AuthenticationService> logger,
        IOptions<AppConfiguration.AuthenticationSettings> authSettings)
    {
        _context = context;
        _licenseService = licenseService;
        _emailService = emailService;
        _logger = logger;
        _authSettings = authSettings.Value;
    }

    public async Task<Result<User>> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Authenticating user: {Email}", email);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User not found or inactive: {Email}", email);
                return Result<User>.Failure("Invalid email or password");
            }

            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password for user: {Email}", email);
                return Result<User>.Failure("Invalid email or password");
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Authentication successful for user: {UserId}", user.Id);
            return Result<User>.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating user: {Email}", email);
            return Result<User>.Failure("Authentication failed");
        }
    }

    public async Task<Result<User>> RegisterAsync(string email, string password, string fullName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Registering user: {Email}", email);

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (existingUser != null)
            {
                _logger.LogWarning("User already exists: {Email}", email);
                return Result<User>.Failure("User with this email already exists");
            }

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email.ToLowerInvariant(),
                FirstName = fullName.Split(' ').FirstOrDefault() ?? fullName,
                LastName = fullName.Contains(' ') ? string.Join(" ", fullName.Split(' ').Skip(1)) : "",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            // Create trial license
            await _licenseService.CreateTrialLicenseAsync(user.Id, cancellationToken);

            // Send welcome email
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send welcome email to {Email}", user.Email);
                // Don't fail registration if email fails
            }

            _logger.LogInformation("Registration successful for user: {UserId}", user.Id);
            return Result<User>.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user: {Email}", email);
            return Result<User>.Failure("Registration failed");
        }
    }

    public async Task<Result<User>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

            if (user == null)
            {
                return Result<User>.Failure("User not found");
            }

            return Result<User>.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
            return Result<User>.Failure("Failed to get user");
        }
    }

    public async Task<Result<User>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);

            if (user == null)
            {
                return Result<User>.Failure("User not found");
            }

            return Result<User>.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email: {Email}", email);
            return Result<User>.Failure("Failed to get user");
        }
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, cancellationToken);

            if (user == null)
            {
                return Result.Failure("User not found");
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                return Result.Failure("Current password is incorrect");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Password changed for user: {UserId}", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
            return Result.Failure("Failed to change password");
        }
    }

    public async Task<Result> ResetPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, cancellationToken);

            if (user == null)
            {
                // Don't reveal if user exists or not
                return Result.Success();
            }

            // Generate temporary password
            var tempPassword = Guid.NewGuid().ToString("N")[..8];
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

            await _context.SaveChangesAsync(cancellationToken);

            // Send password reset email
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, tempPassword, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
                return Result.Failure("Failed to send password reset email");
            }

            _logger.LogInformation("Password reset for user: {Email}", email);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user: {Email}", email);
            return Result.Failure("Failed to reset password");
        }
    }

    public async Task<Result> UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return Result.Failure("User not found");
            }

            user.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating last login for user: {UserId}", userId);
            return Result.Failure("Failed to update last login");
        }
    }

    public async Task<Result> DeactivateUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return Result.Failure("User not found");
            }

            user.IsActive = false;

            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("User deactivated: {UserId}, Reason: {Reason}", userId, reason);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user: {UserId}", userId);
            return Result.Failure("Failed to deactivate user");
        }
    }
}