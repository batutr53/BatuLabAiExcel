using BatuLabAiExcel.Models.DTOs;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Payment service interface for Stripe integration
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Get available subscription plans
    /// </summary>
    Task<List<SubscriptionPlan>> GetSubscriptionPlansAsync();

    /// <summary>
    /// Create Stripe checkout session for subscription
    /// </summary>
    Task<PaymentResponse> CreateCheckoutSessionAsync(CreatePaymentRequest request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handle Stripe webhook events
    /// </summary>
    Task<bool> HandleWebhookAsync(string json, string signature, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify payment completion and update license
    /// </summary>
    Task<bool> VerifyPaymentAndUpdateLicenseAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel subscription
    /// </summary>
    Task<bool> CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get customer billing portal URL
    /// </summary>
    Task<string?> GetBillingPortalUrlAsync(string customerId, string returnUrl, CancellationToken cancellationToken = default);
}