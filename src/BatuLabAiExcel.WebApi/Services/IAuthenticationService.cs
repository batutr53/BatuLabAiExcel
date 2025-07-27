using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Models.Entities;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// Authentication service interface for Web API
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticate user with email and password
    /// </summary>
    Task<Result<User>> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Register new user
    /// </summary>
    Task<Result<User>> RegisterAsync(string email, string password, string fullName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by ID
    /// </summary>
    Task<Result<User>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by email
    /// </summary>
    Task<Result<User>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Change password
    /// </summary>
    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reset password
    /// </summary>
    Task<Result> ResetPasswordAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update last login
    /// </summary>
    Task<Result> UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate user
    /// </summary>
    Task<Result> DeactivateUserAsync(Guid userId, string reason, CancellationToken cancellationToken = default);
}