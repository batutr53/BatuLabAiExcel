using BatuLabAiExcel.Models.DTOs;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Web API client interface for secure server communication
/// </summary>
public interface IWebApiClient
{
    /// <summary>
    /// Authenticate user with the Web API
    /// </summary>
    Task<Result<AuthenticationResult>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Register new user through the Web API
    /// </summary>
    Task<Result<AuthenticationResult>> RegisterAsync(string email, string password, string fullName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate JWT token and get user information
    /// </summary>
    Task<UserInfo?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate license key through the Web API
    /// </summary>
    Task<Result<LicenseInfo>> ValidateLicenseAsync(string licenseKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current user license information
    /// </summary>
    Task<Result<LicenseInfo>> GetUserLicenseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available subscription plans
    /// </summary>
    Task<Result<List<SubscriptionPlan>>> GetSubscriptionPlansAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create Stripe checkout session
    /// </summary>
    Task<Result<PaymentResponse>> CreateCheckoutSessionAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify payment and update license
    /// </summary>
    Task<Result> VerifyPaymentAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel current subscription
    /// </summary>
    Task<Result> CancelSubscriptionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Extend trial license
    /// </summary>
    Task<Result<LicenseInfo>> ExtendTrialAsync(int days, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set authentication token
    /// </summary>
    void SetAuthToken(string token);

    /// <summary>
    /// Clear authentication token
    /// </summary>
    void ClearAuthToken();

    /// <summary>
    /// Check if client has valid authentication token
    /// </summary>
    bool IsAuthenticated { get; }
}