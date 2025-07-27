using Microsoft.Extensions.Logging;
using BatuLabAiExcel.Models;
using BatuLabAiExcel.Models.DTOs;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Payment service that uses Web API for secure payment operations
/// </summary>
public class WebApiPaymentService : IPaymentService
{
    private readonly IWebApiClient _webApiClient;
    private readonly ILogger<WebApiPaymentService> _logger;

    public WebApiPaymentService(IWebApiClient webApiClient, ILogger<WebApiPaymentService> logger)
    {
        _webApiClient = webApiClient;
        _logger = logger;
    }

    public async Task<List<SubscriptionPlan>> GetSubscriptionPlansAsync()
    {
        try
        {
            _logger.LogInformation("Getting subscription plans through Web API");

            var result = await _webApiClient.GetSubscriptionPlansAsync();
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to get subscription plans: {Error}", result.Error);
                return new List<SubscriptionPlan>();
            }

            _logger.LogInformation("Subscription plans retrieved successfully");
            return result.Data ?? new List<SubscriptionPlan>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plans");
            return new List<SubscriptionPlan>();
        }
    }

    public async Task<PaymentResponse> CreateCheckoutSessionAsync(CreatePaymentRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating checkout session through Web API for user: {UserId}", userId);

            var result = await _webApiClient.CreateCheckoutSessionAsync(request, cancellationToken);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to create checkout session for user: {UserId}", userId);
                return new PaymentResponse
                {
                    Success = false,
                    Message = result.Error ?? "Failed to create checkout session",
                    Errors = new List<string> { result.Error ?? "Checkout creation failed" }
                };
            }

            _logger.LogInformation("Checkout session created successfully for user: {UserId}", userId);
            return result.Data!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session for user: {UserId}", userId);
            return new PaymentResponse
            {
                Success = false,
                Message = "Payment service error",
                Errors = new List<string> { "Checkout creation service error" }
            };
        }
    }

    public async Task<bool> HandleWebhookAsync(string json, string signature, CancellationToken cancellationToken = default)
    {
        // Webhooks are handled by the Web API, not the desktop client
        _logger.LogWarning("Webhook handling should be done by Web API, not desktop client");
        return await Task.FromResult(false);
    }

    public async Task<bool> VerifyPaymentAndUpdateLicenseAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying payment through Web API for session: {SessionId}", sessionId);

            var result = await _webApiClient.VerifyPaymentAsync(sessionId, cancellationToken);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Payment verification failed for session: {SessionId}", sessionId);
                return false;
            }

            _logger.LogInformation("Payment verification successful for session: {SessionId}", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment for session: {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<bool> CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling subscription through Web API");

            var result = await _webApiClient.CancelSubscriptionAsync(cancellationToken);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Subscription cancellation failed");
                return false;
            }

            _logger.LogInformation("Subscription cancellation successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription");
            return false;
        }
    }

    public async Task<string?> GetBillingPortalUrlAsync(string customerId, string returnUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting billing portal URL through Web API");

            // This would require a new API endpoint for billing portal
            _logger.LogWarning("Billing portal URL not implemented in Web API mode");
            return await Task.FromResult<string?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting billing portal URL");
            return null;
        }
    }
}