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