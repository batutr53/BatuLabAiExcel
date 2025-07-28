using System.ComponentModel.DataAnnotations;
using BatuLabAiExcel.WebApi.Models.Entities;

namespace BatuLabAiExcel.WebApi.Models;

/// <summary>
/// API login request model
/// </summary>
public class ApiLoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// API register request model
/// </summary>
public class ApiRegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string FullName { get; set; } = string.Empty;
}

/// <summary>
/// API authentication response model
/// </summary>
public class ApiAuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public ApiUserInfo? User { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// API user information model
/// </summary>
public class ApiUserInfo
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public ApiLicenseInfo? License { get; set; }
}

/// <summary>
/// API license information model
/// </summary>
public class ApiLicenseInfo
{
    public Guid Id { get; set; }
    public LicenseType Type { get; set; }
    public string LicenseKey { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int DaysRemaining { get; set; }
}

/// <summary>
/// License validation request model
/// </summary>
public class LicenseValidationRequest
{
    [Required]
    public string LicenseKey { get; set; } = string.Empty;
}

/// <summary>
/// License validation response model
/// </summary>
public class LicenseValidationResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public ApiLicenseInfo? License { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Payment request model for API
/// </summary>
public class ApiPaymentRequest
{
    [Required]
    public LicenseType LicenseType { get; set; }

    [Required]
    [Url]
    public string SuccessUrl { get; set; } = string.Empty;

    [Required]
    [Url]
    public string CancelUrl { get; set; } = string.Empty;
}

/// <summary>
/// Payment response model for API
/// </summary>
public class ApiPaymentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
    public string? SessionId { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Standard API response wrapper
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> SuccessResult(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}

/// <summary>
/// Empty API response for operations without return data
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse SuccessResult(string message = "Success")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }

    public static new ApiResponse ErrorResult(string message, List<string>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }
}

/// <summary>
/// License extension request model
/// </summary>
public class ExtendLicenseRequest
{
    [Required]
    [Range(1, 365, ErrorMessage = "Days must be between 1 and 365")]
    public int Days { get; set; }
}

/// <summary>
/// Create user request model for API
/// </summary>
public class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string LastName { get; set; } = string.Empty;
}

/// <summary>
/// Update user request model for API
/// </summary>
public class UpdateUserRequest
{
    [Required]
    public Guid Id { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [MinLength(6)]
    public string? Password { get; set; }

    [MinLength(1)]
    public string? FirstName { get; set; }

    [MinLength(1)]
    public string? LastName { get; set; }

    public bool? IsActive { get; set; }
}

/// <summary>
/// Create license request model for API
/// </summary>
public class CreateLicenseRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public LicenseType Type { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Update license request model for API
/// </summary>
public class UpdateLicenseRequest
{
    [Required]
    public Guid Id { get; set; }

    public LicenseType? Type { get; set; }

    public LicenseStatus? Status { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? ExpiresAt { get; set; }
}

/// <summary>
/// Send notification request model for API
/// </summary>
public class SendNotificationRequest
{
    [Required]
    public List<Guid> UserIds { get; set; } = new();

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Broadcast notification request model for API
/// </summary>
public class BroadcastNotificationRequest
{
    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Admin settings model for API
/// </summary>
public class AdminSettings
{
    public GeneralSettings General { get; set; } = new();
    public UserSettings Users { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public PaymentSettings Payment { get; set; } = new();
    public ApiSettings Api { get; set; } = new();
}

public class GeneralSettings
{
    public string AppName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string TimeZone { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public bool MaintenanceMode { get; set; }
}

public class UserSettings
{
    public bool AllowRegistration { get; set; }
    public bool EmailVerificationRequired { get; set; }
    public bool AutoTrialLicense { get; set; }
    public int MinimumPasswordLength { get; set; }
    public int MaxLoginAttempts { get; set; }
}

public class SecuritySettings
{
    public int TokenExpiryHours { get; set; }
    public int RefreshTokenDurationDays { get; set; }
    public bool EnableRateLimiting { get; set; }
    public int GeneralRateLimit { get; set; }
    public int AuthRateLimit { get; set; }
    public int PaymentRateLimit { get; set; }
}

public class NotificationSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool EnableSsl { get; set; }
    public List<string> EnabledNotificationTypes { get; set; } = new();
}

public class PaymentSettings
{
    public string StripePublishableKey { get; set; } = string.Empty;
    public string StripeSecretKey { get; set; } = string.Empty;
    public string StripeWebhookSecret { get; set; } = string.Empty;
    public decimal MonthlyPlanPrice { get; set; }
    public decimal YearlyPlanPrice { get; set; }
    public decimal LifetimePlanPrice { get; set; }
    public int TrialDurationDays { get; set; }
}

public class ApiSettings
{
    public string ClaudeApiKey { get; set; } = string.Empty;
    public string ClaudeModel { get; set; } = string.Empty;
    public string GeminiApiKey { get; set; } = string.Empty;
    public string GroqApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Notification model for API response
/// </summary>
public class ApiNotification
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}