using BatuLabAiExcel.Models.Entities;

namespace BatuLabAiExcel.Models.DTOs;

/// <summary>
/// Subscription plan information
/// </summary>
public class SubscriptionPlan
{
    public LicenseType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string StripePriceId { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
    public bool IsPopular { get; set; } = false;
    public string ButtonText { get; set; } = "Subscribe";
    public string PriceText { get; set; } = string.Empty;
}

/// <summary>
/// Payment intent creation request
/// </summary>
public class CreatePaymentRequest
{
    public LicenseType LicenseType { get; set; }
    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

/// <summary>
/// Payment intent response
/// </summary>
public class PaymentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
    public string? SessionId { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Stripe webhook event data
/// </summary>
public class StripeWebhookData
{
    public string EventType { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
    public string? CustomerId { get; set; }
    public string? SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTime EventTime { get; set; }
}

/// <summary>
/// License validation request
/// </summary>
public class LicenseValidationRequest
{
    public Guid UserId { get; set; }
    public string? LicenseKey { get; set; }
    public string MachineId { get; set; } = string.Empty;
}

/// <summary>
/// License validation response
/// </summary>
public class LicenseValidationResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public LicenseInfo? License { get; set; }
    public List<string> Errors { get; set; } = new();
}