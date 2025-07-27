using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Models.DTOs;
using BatuLabAiExcel.WebApi.Models.Entities;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// Payment service interface for handling Stripe payments and subscriptions
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Get available subscription plans
    /// </summary>
    Task<Result<List<SubscriptionPlan>>> GetSubscriptionPlansAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create Stripe checkout session for subscription
    /// </summary>
    Task<Result<PaymentResponse>> CreateCheckoutSessionAsync(Guid userId, CreatePaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify payment completion and activate license
    /// </summary>
    Task<Result> VerifyPaymentAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify payment and update license (alias for VerifyPaymentAsync)
    /// </summary>
    Task<Result> VerifyPaymentAndUpdateLicenseAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process Stripe webhook events
    /// </summary>
    Task<Result> ProcessWebhookAsync(StripeWebhookData webhookData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handle Stripe webhook requests (raw JSON + signature)
    /// </summary>
    Task<Result> HandleWebhookAsync(string json, string signature, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel user subscription
    /// </summary>
    Task<Result> CancelSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refund payment for a license
    /// </summary>
    Task<Result> RefundPaymentAsync(Guid paymentId, string reason, CancellationToken cancellationToken = default);
}