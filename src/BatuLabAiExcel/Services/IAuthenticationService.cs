using BatuLabAiExcel.Models.DTOs;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Authentication service interface
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate user with email and password
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Register new user with 1-day trial license
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout current user
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Get current authenticated user
    /// </summary>
    Task<UserInfo?> GetCurrentUserAsync();

    /// <summary>
    /// Validate JWT token
    /// </summary>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Refresh authentication token
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(string token);

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Current user information
    /// </summary>
    UserInfo? CurrentUser { get; }

    /// <summary>
    /// Authentication state changed event
    /// </summary>
    event EventHandler<bool> AuthenticationStateChanged;
}