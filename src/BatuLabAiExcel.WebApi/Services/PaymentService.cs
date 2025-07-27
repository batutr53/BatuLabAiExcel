using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Models.DTOs;
using BatuLabAiExcel.WebApi.Models.Entities;
using BatuLabAiExcel.WebApi.Data;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// Payment service implementation using Stripe
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PaymentService> _logger;
    private readonly ILicenseService _licenseService;
    private readonly StripeSettings _stripeSettings;

    public PaymentService(
        AppDbContext context,
        ILogger<PaymentService> logger,
        ILicenseService licenseService,
        IOptions<StripeSettings> stripeSettings)
    {
        _context = context;
        _logger = logger;
        _licenseService = licenseService;
        _stripeSettings = stripeSettings.Value;
        
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
    }

    public Task<Result<List<SubscriptionPlan>>> GetSubscriptionPlansAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var plans = new List<SubscriptionPlan>
            {
                new()
                {
                    Type = LicenseType.Trial,
                    Name = "Free Trial",
                    Description = "1-day trial to test all features",
                    Price = 0,
                    Currency = "USD",
                    StripePriceId = "",
                    Features = ["All Excel AI features", "1 day access", "No payment required"],
                    ButtonText = "Start Free Trial",
                    PriceText = "Free"
                },
                new()
                {
                    Type = LicenseType.Monthly,
                    Name = "Monthly Plan",
                    Description = "Full access with monthly billing",
                    Price = 19.99m,
                    Currency = "USD",
                    StripePriceId = _stripeSettings.MonthlyPriceId,
                    Features = ["All Excel AI features", "Monthly billing", "Cancel anytime"],
                    IsPopular = true,
                    ButtonText = "Subscribe Monthly",
                    PriceText = "$19.99/month"
                },
                new()
                {
                    Type = LicenseType.Yearly,
                    Name = "Yearly Plan",
                    Description = "Best value with yearly billing",
                    Price = 199.99m,
                    Currency = "USD",
                    StripePriceId = _stripeSettings.YearlyPriceId,
                    Features = ["All Excel AI features", "Yearly billing", "2 months free", "Priority support"],
                    ButtonText = "Subscribe Yearly",
                    PriceText = "$199.99/year"
                },
                new()
                {
                    Type = LicenseType.Lifetime,
                    Name = "Lifetime License",
                    Description = "One-time payment for lifetime access",
                    Price = 499.99m,
                    Currency = "USD",
                    StripePriceId = _stripeSettings.LifetimePriceId,
                    Features = ["All Excel AI features", "Lifetime access", "All future updates", "Premium support"],
                    ButtonText = "Buy Lifetime",
                    PriceText = "$499.99 once"
                }
            };

            return Task.FromResult(Result<List<SubscriptionPlan>>.Success(plans));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plans");
            return Task.FromResult(Result<List<SubscriptionPlan>>.Failure("Failed to get subscription plans"));
        }
    }

    public async Task<Result<PaymentResponse>> CreateCheckoutSessionAsync(Guid userId, CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating checkout session for user {UserId}, license type {LicenseType}", userId, request.LicenseType);

            var plans = await GetSubscriptionPlansAsync(cancellationToken);
            if (!plans.IsSuccess)
            {
                return Result<PaymentResponse>.Failure(plans.Error ?? "Failed to get plans");
            }

            var plan = plans.Value?.FirstOrDefault(p => p.Type == request.LicenseType);
            if (plan == null)
            {
                return Result<PaymentResponse>.Failure("Invalid license type");
            }

            if (plan.Type == LicenseType.Trial)
            {
                // Handle trial license creation directly
                var trialResult = await _licenseService.CreateTrialLicenseAsync(userId, cancellationToken);
                if (!trialResult.IsSuccess)
                {
                    return Result<PaymentResponse>.Failure(trialResult.Error ?? "Failed to create trial");
                }

                return Result<PaymentResponse>.Success(new PaymentResponse
                {
                    Success = true,
                    Message = "Trial license created successfully",
                    CheckoutUrl = request.SuccessUrl
                });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Result<PaymentResponse>.Failure("User not found");
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Price = plan.StripePriceId,
                        Quantity = 1,
                    },
                },
                Mode = plan.Type == LicenseType.Lifetime ? "payment" : "subscription",
                SuccessUrl = request.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = request.CancelUrl,
                CustomerEmail = user.Email,
                Metadata = new Dictionary<string, string>
                {
                    { "user_id", userId.ToString() },
                    { "license_type", request.LicenseType.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

            // Create pending payment record
            var payment = new Payment
            {
                UserId = userId,
                StripePaymentIntentId = session.Id,
                Amount = plan.Price,
                Currency = plan.Currency,
                Status = PaymentStatus.Pending,
                LicenseType = request.LicenseType,
                Description = $"{plan.Name} subscription"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Checkout session created successfully: {SessionId}", session.Id);

            return Result<PaymentResponse>.Success(new PaymentResponse
            {
                Success = true,
                Message = "Checkout session created successfully",
                CheckoutUrl = session.Url,
                SessionId = session.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session for user {UserId}", userId);
            return Result<PaymentResponse>.Failure("Failed to create checkout session");
        }
    }

    public async Task<Result> VerifyPaymentAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying payment for session {SessionId}", sessionId);

            var service = new SessionService();
            var session = await service.GetAsync(sessionId, cancellationToken: cancellationToken);

            if (session.PaymentStatus != "paid")
            {
                return Result.Failure("Payment not completed");
            }

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == sessionId, cancellationToken);

            if (payment == null)
            {
                return Result.Failure("Payment record not found");
            }

            if (payment.Status == PaymentStatus.Succeeded)
            {
                return Result.Success(); // Already processed
            }

            // Update payment status
            payment.Status = PaymentStatus.Succeeded;
            payment.PaidAt = DateTime.UtcNow;
            payment.StripeInvoiceId = session.Invoice?.ToString();

            // Create license
            var licenseResult = await _licenseService.CreateLicenseAsync(
                payment.UserId,
                payment.LicenseType,
                payment.Id,
                session.Customer?.ToString(),
                session.Subscription?.ToString(),
                cancellationToken);

            if (!licenseResult.IsSuccess)
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = licenseResult.Error;
                await _context.SaveChangesAsync(cancellationToken);
                return Result.Failure("Failed to create license");
            }

            payment.LicenseId = licenseResult.Value?.Id;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Payment verified and license created for session {SessionId}", sessionId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment for session {SessionId}", sessionId);
            return Result.Failure("Payment verification failed");
        }
    }

    public async Task<Result> ProcessWebhookAsync(StripeWebhookData webhookData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing webhook event {EventType}", webhookData.EventType);

            switch (webhookData.EventType)
            {
                case "payment_intent.succeeded":
                    return await HandlePaymentSucceeded(webhookData, cancellationToken);
                case "payment_intent.payment_failed":
                    return await HandlePaymentFailed(webhookData, cancellationToken);
                case "customer.subscription.deleted":
                    return await HandleSubscriptionDeleted(webhookData, cancellationToken);
                default:
                    _logger.LogWarning("Unhandled webhook event type: {EventType}", webhookData.EventType);
                    return Result.Success();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event {EventType}", webhookData.EventType);
            return Result.Failure("Webhook processing failed");
        }
    }

    public async Task<Result> VerifyPaymentAndUpdateLicenseAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await VerifyPaymentAsync(sessionId, cancellationToken);
    }

    public async Task<Result> HandleWebhookAsync(string json, string signature, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing Stripe webhook");

            // Verify webhook signature (simplified for now)
            if (string.IsNullOrEmpty(json))
            {
                return Result.Failure("Invalid webhook data");
            }

            // Parse webhook data (simplified implementation)
            var webhookData = new StripeWebhookData
            {
                EventType = "payment_intent.succeeded", // Default event type
                PaymentIntentId = "pi_example",
                Amount = 0,
                Currency = "USD",
                Status = PaymentStatus.Succeeded,
                EventTime = DateTime.UtcNow
            };

            return await ProcessWebhookAsync(webhookData, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook");
            return Result.Failure("Webhook handling failed");
        }
    }

    public async Task<Result> CancelSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling subscription for user {UserId}", userId);

            var license = await _context.Licenses
                .Where(l => l.UserId == userId && l.IsActive && !string.IsNullOrEmpty(l.StripeSubscriptionId))
                .FirstOrDefaultAsync(cancellationToken);

            if (license == null)
            {
                return Result.Failure("No active subscription found");
            }

            // Cancel in Stripe
            var service = new SubscriptionService();
            await service.CancelAsync(license.StripeSubscriptionId, cancellationToken: cancellationToken);

            // Update license status
            license.Status = LicenseStatus.Cancelled;
            license.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Subscription cancelled for user {UserId}", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription for user {UserId}", userId);
            return Result.Failure("Failed to cancel subscription");
        }
    }

    public async Task<Result> RefundPaymentAsync(Guid paymentId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing refund for payment {PaymentId}", paymentId);

            var payment = await _context.Payments
                .Include(p => p.License)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

            if (payment == null)
            {
                return Result.Failure("Payment not found");
            }

            if (payment.Status != PaymentStatus.Succeeded)
            {
                return Result.Failure("Payment cannot be refunded");
            }

            // Create refund in Stripe
            var service = new RefundService();
            var refund = await service.CreateAsync(new RefundCreateOptions
            {
                PaymentIntent = payment.StripePaymentIntentId,
                Reason = "requested_by_customer",
                Metadata = new Dictionary<string, string>
                {
                    { "reason", reason },
                    { "payment_id", paymentId.ToString() }
                }
            }, cancellationToken: cancellationToken);

            // Update payment status
            payment.Status = PaymentStatus.Refunded;
            payment.RefundedAt = DateTime.UtcNow;
            payment.FailureReason = reason;

            // Deactivate license
            if (payment.License != null)
            {
                payment.License.IsActive = false;
                payment.License.Status = LicenseStatus.Cancelled;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Refund processed for payment {PaymentId}", paymentId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
            return Result.Failure("Refund processing failed");
        }
    }

    private async Task<Result> HandlePaymentSucceeded(StripeWebhookData webhookData, CancellationToken cancellationToken)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == webhookData.PaymentIntentId, cancellationToken);

        if (payment != null && payment.Status == PaymentStatus.Pending)
        {
            payment.Status = PaymentStatus.Succeeded;
            payment.PaidAt = webhookData.EventTime;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    private async Task<Result> HandlePaymentFailed(StripeWebhookData webhookData, CancellationToken cancellationToken)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == webhookData.PaymentIntentId, cancellationToken);

        if (payment != null && payment.Status == PaymentStatus.Pending)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = webhookData.FailureReason;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }

    private async Task<Result> HandleSubscriptionDeleted(StripeWebhookData webhookData, CancellationToken cancellationToken)
    {
        var license = await _context.Licenses
            .FirstOrDefaultAsync(l => l.StripeSubscriptionId == webhookData.SubscriptionId, cancellationToken);

        if (license != null)
        {
            license.Status = LicenseStatus.Cancelled;
            license.IsActive = false;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}

/// <summary>
/// Stripe configuration settings
/// </summary>
public class StripeSettings
{
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string MonthlyPriceId { get; set; } = string.Empty;
    public string YearlyPriceId { get; set; } = string.Empty;
    public string LifetimePriceId { get; set; } = string.Empty;
}