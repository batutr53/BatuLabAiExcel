using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using Stripe.BillingPortal;
using BatuLabAiExcel.Data;
using BatuLabAiExcel.Models.DTOs;
using BatuLabAiExcel.Models.Entities;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Stripe payment service implementation
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PaymentService> _logger;
    private readonly ILicenseService _licenseService;
    private readonly IEmailService _emailService;
    private readonly AppConfiguration.StripeSettings _stripeSettings;
    private readonly Stripe.Checkout.SessionService _sessionService;
    private readonly CustomerService _customerService;
    private readonly SubscriptionService _subscriptionService;
    private readonly Stripe.BillingPortal.SessionService _billingPortalService;

    public PaymentService(
        AppDbContext context,
        ILogger<PaymentService> logger,
        ILicenseService licenseService,
        IEmailService emailService,
        IOptions<AppConfiguration.StripeSettings> stripeSettings)
    {
        _context = context;
        _logger = logger;
        _licenseService = licenseService;
        _emailService = emailService;
        _stripeSettings = stripeSettings.Value;

        // Initialize Stripe services
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey ?? 
                                   throw new InvalidOperationException("Stripe SecretKey is not configured in appsettings.json");

        _sessionService = new Stripe.Checkout.SessionService();
        _customerService = new CustomerService();
        _subscriptionService = new SubscriptionService();
        _billingPortalService = new Stripe.BillingPortal.SessionService();
    }

    public Task<List<SubscriptionPlan>> GetSubscriptionPlansAsync()
    {
        var plans = new List<SubscriptionPlan>
        {
            new()
            {
                Type = LicenseType.Monthly,
                Name = "Monthly Plan",
                Description = "Perfect for short-term projects",
                Price = 29.99m,
                Currency = "USD",
                StripePriceId = _stripeSettings.MonthlyPriceId,
                Features = new List<string>
                {
                    "✅ Full Excel AI capabilities",
                    "✅ Claude, Gemini & Groq AI support",
                    "✅ Unlimited file processing",
                    "✅ Email support",
                    "✅ Monthly updates"
                },
                ButtonText = "Start Monthly",
                PriceText = "$29.99/month"
            },
            new()
            {
                Type = LicenseType.Yearly,
                Name = "Yearly Plan",
                Description = "Best value for regular users",
                Price = 299.99m,
                Currency = "USD",
                StripePriceId = _stripeSettings.YearlyPriceId,
                Features = new List<string>
                {
                    "✅ All Monthly features",
                    "✅ 2 months FREE (16% savings)",
                    "✅ Priority support",
                    "✅ Advanced AI features",
                    "✅ Beta access to new features"
                },
                IsPopular = true,
                ButtonText = "Get Yearly",
                PriceText = "$299.99/year"
            },
            new()
            {
                Type = LicenseType.Lifetime,
                Name = "Lifetime License",
                Description = "One-time payment, lifetime access",
                Price = 999.99m,
                Currency = "USD",
                StripePriceId = _stripeSettings.LifetimePriceId,
                Features = new List<string>
                {
                    "✅ All Yearly features",
                    "✅ Lifetime updates",
                    "✅ VIP support",
                    "✅ Early access to all features",
                    "✅ Commercial license included"
                },
                ButtonText = "Buy Lifetime",
                PriceText = "$999.99 once"
            }
        };

        return Task.FromResult(plans);
    }

    public async Task<PaymentResponse> CreateCheckoutSessionAsync(CreatePaymentRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating checkout session for user: {UserId}, License: {LicenseType}", userId, request.LicenseType);

            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user == null)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = "User not found",
                    Errors = ["User not found"]
                };
            }

            var plans = await GetSubscriptionPlansAsync();
            var selectedPlan = plans.FirstOrDefault(p => p.Type == request.LicenseType);

            if (selectedPlan == null)
            {
                return new PaymentResponse
                {
                    Success = false,
                    Message = "Invalid license type",
                    Errors = ["Invalid license type"]
                };
            }

            // Create or get Stripe customer
            var customer = await GetOrCreateStripeCustomerAsync(user);

            var sessionOptions = new Stripe.Checkout.SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Customer = customer.Id,
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new()
                    {
                        Price = selectedPlan.StripePriceId,
                        Quantity = 1
                    }
                },
                Mode = request.LicenseType == LicenseType.Lifetime ? "payment" : "subscription",
                SuccessUrl = request.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = request.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(),
                    ["license_type"] = request.LicenseType.ToString()
                }
            };

            var session = await _sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);

            _logger.LogInformation("Checkout session created: {SessionId} for user: {UserId}", session.Id, userId);

            return new PaymentResponse
            {
                Success = true,
                Message = "Checkout session created successfully",
                CheckoutUrl = session.Url,
                SessionId = session.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session for user: {UserId}", userId);
            return new PaymentResponse
            {
                Success = false,
                Message = "Failed to create checkout session",
                Errors = ["Payment service error"]
            };
        }
    }

    public async Task<bool> HandleWebhookAsync(string json, string signature, CancellationToken cancellationToken = default)
    {
        try
        {
            var webhookSecret = _stripeSettings.WebhookSecret;
            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogError("Stripe webhook secret not configured in appsettings.json");
                return false;
            }

            var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);
            _logger.LogInformation("Processing Stripe webhook: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompletedAsync(stripeEvent, cancellationToken);
                    break;

                case "invoice.payment_succeeded":
                    await HandleInvoicePaymentSucceededAsync(stripeEvent, cancellationToken);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeletedAsync(stripeEvent, cancellationToken);
                    break;

                case "invoice.payment_failed":
                    await HandlePaymentFailedAsync(stripeEvent, cancellationToken);
                    break;

                default:
                    _logger.LogDebug("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return false;
        }
    }

    public async Task<bool> VerifyPaymentAndUpdateLicenseAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _sessionService.GetAsync(sessionId, cancellationToken: cancellationToken);

            if (session.PaymentStatus != "paid")
            {
                _logger.LogWarning("Payment not completed for session: {SessionId}", sessionId);
                return false;
            }

            if (!session.Metadata.TryGetValue("user_id", out var userIdStr) ||
                !Guid.TryParse(userIdStr, out var userId))
            {
                _logger.LogError("Invalid user ID in session metadata: {SessionId}", sessionId);
                return false;
            }

            if (!session.Metadata.TryGetValue("license_type", out var licenseTypeStr) ||
                !Enum.TryParse<LicenseType>(licenseTypeStr, out var licenseType))
            {
                _logger.LogError("Invalid license type in session metadata: {SessionId}", sessionId);
                return false;
            }

            // Update license
            var subscriptionId = session.SubscriptionId ?? string.Empty;
            var license = await _licenseService.UpdateLicenseFromPaymentAsync(userId, licenseType, subscriptionId, cancellationToken);

            if (license == null)
            {
                _logger.LogError("Failed to update license for user: {UserId}, Session: {SessionId}", userId, sessionId);
                return false;
            }

            // Record payment
            var payment = new Models.Entities.Payment
            {
                UserId = userId,
                LicenseId = license.Id,
                StripePaymentIntentId = session.PaymentIntentId ?? sessionId,
                Amount = (session.AmountTotal ?? 0) / 100m, // Convert from cents
                Currency = session.Currency?.ToUpperInvariant() ?? "USD",
                Status = PaymentStatus.Succeeded,
                LicenseType = licenseType,
                PaidAt = DateTime.UtcNow,
                Description = $"License upgrade to {licenseType}"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken);

            // Send license key email to user
            var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
            if (user != null)
            {
                var emailResult = await _emailService.SendLicenseKeyEmailAsync(
                    user.Email, 
                    user.FullName, 
                    license.LicenseKey, 
                    licenseType.ToString(), 
                    cancellationToken);

                if (emailResult.IsSuccess)
                {
                    _logger.LogInformation("License key email sent successfully to {Email}", user.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to send license key email to {Email}: {Error}", user.Email, emailResult.Error);
                }
            }

            _logger.LogInformation("Payment verified and license updated for user: {UserId}, License: {LicenseType}", userId, licenseType);
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
            await _subscriptionService.CancelAsync(subscriptionId, cancellationToken: cancellationToken);
            _logger.LogInformation("Subscription cancelled: {SubscriptionId}", subscriptionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription: {SubscriptionId}", subscriptionId);
            return false;
        }
    }

    public async Task<string?> GetBillingPortalUrlAsync(string customerId, string returnUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = returnUrl
            };

            var session = await _billingPortalService.CreateAsync(options, cancellationToken: cancellationToken);
            return session.Url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating billing portal session for customer: {CustomerId}", customerId);
            return null;
        }
    }

    private async Task<Customer> GetOrCreateStripeCustomerAsync(Models.Entities.User user)
    {
        // Check if customer already exists
        var existingCustomers = await _customerService.ListAsync(new CustomerListOptions
        {
            Email = user.Email,
            Limit = 1
        });

        if (existingCustomers.Data.Any())
        {
            return existingCustomers.Data.First();
        }

        // Create new customer
        var customerOptions = new CustomerCreateOptions
        {
            Email = user.Email,
            Name = user.FullName,
            Metadata = new Dictionary<string, string>
            {
                ["user_id"] = user.Id.ToString()
            }
        };

        return await _customerService.CreateAsync(customerOptions);
    }

    private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session != null)
        {
            await VerifyPaymentAndUpdateLicenseAsync(session.Id, cancellationToken);
        }
    }

    private async Task HandleInvoicePaymentSucceededAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice?.SubscriptionId != null)
        {
            // Handle recurring subscription payment
            var subscription = await _subscriptionService.GetAsync(invoice.SubscriptionId, cancellationToken: cancellationToken);
            
            if (subscription.Metadata.TryGetValue("user_id", out var userIdStr) &&
                Guid.TryParse(userIdStr, out var userId))
            {
                // Extend license for recurring payment
                var license = await _context.Licenses
                    .FirstOrDefaultAsync(l => l.StripeSubscriptionId == invoice.SubscriptionId && l.IsActive, cancellationToken);

                if (license != null)
                {
                    license.ExpiresAt = license.Type == LicenseType.Monthly 
                        ? license.ExpiresAt.AddMonths(1) 
                        : license.ExpiresAt.AddYears(1);

                    await _context.SaveChangesAsync(cancellationToken);

                    // Send renewal confirmation email
                    var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
                    if (user != null)
                    {
                        var emailResult = await _emailService.SendLicenseKeyEmailAsync(
                            user.Email, 
                            user.FullName, 
                            license.LicenseKey, 
                            $"{license.Type} (Renewed)", 
                            cancellationToken);

                        if (emailResult.IsSuccess)
                        {
                            _logger.LogInformation("License renewal email sent successfully to {Email}", user.Email);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to send license renewal email to {Email}: {Error}", user.Email, emailResult.Error);
                        }
                    }

                    _logger.LogInformation("License extended for recurring payment - User: {UserId}", userId);
                }
            }
        }
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription?.Metadata.TryGetValue("user_id", out var userIdStr) == true &&
            Guid.TryParse(userIdStr, out var userId))
        {
            await _licenseService.CancelLicenseAsync(userId, "Subscription cancelled by customer", cancellationToken);
        }
    }

    private async Task HandlePaymentFailedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice?.CustomerId != null)
        {
            _logger.LogWarning("Payment failed for customer: {CustomerId}, Invoice: {InvoiceId}", invoice.CustomerId, invoice.Id);
            // Could implement email notification or grace period logic here
        }
    }
}