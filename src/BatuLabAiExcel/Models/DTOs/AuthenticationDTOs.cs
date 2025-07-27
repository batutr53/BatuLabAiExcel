using System.ComponentModel.DataAnnotations;
using BatuLabAiExcel.Models.Entities;

namespace BatuLabAiExcel.Models.DTOs;

/// <summary>
/// Login request DTO
/// </summary>
public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}

/// <summary>
/// Registration request DTO
/// </summary>
public class RegisterRequest
{
    [Required(ErrorMessage = "First name is required")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [MaxLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "You must accept the terms and conditions")]
    public bool AcceptTerms { get; set; } = false;
}

/// <summary>
/// Authentication response DTO
/// </summary>
public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public UserInfo? User { get; set; }
    public LicenseInfo? License { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// User information DTO
/// </summary>
public class UserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public static UserInfo FromEntity(User user)
    {
        return new UserInfo
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}

/// <summary>
/// License information DTO
/// </summary>
public class LicenseInfo
{
    public Guid Id { get; set; }
    public LicenseType Type { get; set; }
    public LicenseStatus Status { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsValid { get; set; }
    public bool IsExpired { get; set; }
    public int RemainingDays { get; set; }
    public TimeSpan RemainingTime { get; set; }
    public string TypeDisplayName { get; set; } = string.Empty;
    public string StatusDisplayName { get; set; } = string.Empty;

    public static LicenseInfo FromEntity(License license)
    {
        return new LicenseInfo
        {
            Id = license.Id,
            Type = license.Type,
            Status = license.Status,
            LicenseKey = license.LicenseKey,
            StartDate = license.StartDate,
            ExpiresAt = license.ExpiresAt,
            IsValid = license.IsValid,
            IsExpired = license.IsExpired,
            RemainingDays = license.RemainingDays,
            RemainingTime = license.RemainingTime,
            TypeDisplayName = GetLicenseTypeDisplayName(license.Type),
            StatusDisplayName = GetLicenseStatusDisplayName(license.Status)
        };
    }

    private static string GetLicenseTypeDisplayName(LicenseType type) => type switch
    {
        LicenseType.Trial => "Trial (1 Day)",
        LicenseType.Monthly => "Monthly Plan",
        LicenseType.Yearly => "Yearly Plan",
        LicenseType.Lifetime => "Lifetime License",
        _ => "Unknown"
    };

    private static string GetLicenseStatusDisplayName(LicenseStatus status) => status switch
    {
        LicenseStatus.Pending => "Pending",
        LicenseStatus.Active => "Active",
        LicenseStatus.Expired => "Expired",
        LicenseStatus.Cancelled => "Cancelled",
        LicenseStatus.Suspended => "Suspended",
        _ => "Unknown"
    };
}