using BatuLabAiExcel.WebApi.Models;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// API authentication service interface
/// </summary>
public interface IApiAuthenticationService
{
    /// <summary>
    /// Authenticate user with email and password
    /// </summary>
    Task<ApiAuthResponse> LoginAsync(ApiLoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Register new user
    /// </summary>
    Task<ApiAuthResponse> RegisterAsync(ApiRegisterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate JWT token and get user information
    /// </summary>
    Task<ApiUserInfo?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by ID with license information
    /// </summary>
    Task<ApiUserInfo?> GetUserWithLicenseAsync(Guid userId, CancellationToken cancellationToken = default);
}