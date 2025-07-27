using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Models.DTOs;
using BatuLabAiExcel.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BatuLabAiExcel.WebApi.Controllers;

/// <summary>
/// Payment and subscription controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("Payment")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Get available subscription plans
    /// </summary>
    [HttpGet("plans")]
    public async Task<ActionResult<ApiResponse<List<SubscriptionPlan>>>> GetSubscriptionPlans()
    {
        var result = await _paymentService.GetSubscriptionPlansAsync();
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse<List<SubscriptionPlan>>.ErrorResult(result.Error ?? "Failed to get plans"));
        }
        return Ok(ApiResponse<List<SubscriptionPlan>>.SuccessResult(result.Value!, "Subscription plans retrieved successfully"));
    }

    /// <summary>
    /// Create Stripe checkout session (requires authentication)
    /// </summary>
    [HttpPost("create-checkout")]
    [Authorize]
    public async Task<ActionResult<ApiPaymentResponse>> CreateCheckoutSession(
        [FromBody] ApiPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiPaymentResponse
            {
                Success = false,
                Message = "Invalid request data",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        var userIdClaim = User.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ApiPaymentResponse
            {
                Success = false,
                Message = "Invalid user token",
                Errors = new List<string> { "User not authenticated" }
            });
        }

        var paymentRequest = new CreatePaymentRequest
        {
            LicenseType = request.LicenseType,
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl
        };

        var result = await _paymentService.CreateCheckoutSessionAsync(userId, paymentRequest, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiPaymentResponse
            {
                Success = false,
                Message = result.Error ?? "Checkout creation failed",
                Errors = new List<string> { result.Error ?? "Unknown error" }
            });
        }

        var apiResponse = new ApiPaymentResponse
        {
            Success = result.Value!.Success,
            Message = result.Value.Message,
            CheckoutUrl = result.Value.CheckoutUrl,
            SessionId = result.Value.SessionId,
            Errors = result.Value.Errors
        };

        return Ok(apiResponse);
    }

    /// <summary>
    /// Verify payment and update license (requires authentication)
    /// </summary>
    [HttpPost("verify/{sessionId}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> VerifyPayment(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest(ApiResponse.ErrorResult("Session ID is required"));
        }

        var result = await _paymentService.VerifyPaymentAndUpdateLicenseAsync(sessionId, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse.ErrorResult("Payment verification failed"));
        }

        return Ok(ApiResponse.SuccessResult("Payment verified and license updated successfully"));
    }

    /// <summary>
    /// Get billing portal URL (requires authentication)
    /// </summary>
    [HttpPost("billing-portal")]
    [Authorize]
    public Task<ActionResult<ApiResponse<string>>> GetBillingPortalUrl(
        [FromBody] string returnUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return Task.FromResult<ActionResult<ApiResponse<string>>>(BadRequest(ApiResponse<string>.ErrorResult("Return URL is required")));
        }

        var userIdClaim = User.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Task.FromResult<ActionResult<ApiResponse<string>>>(Unauthorized(ApiResponse<string>.ErrorResult("Invalid user token")));
        }

        // This would require additional implementation to get customer ID from user
        // For now, return not implemented
        return Task.FromResult<ActionResult<ApiResponse<string>>>(StatusCode(501, ApiResponse<string>.ErrorResult("Billing portal not implemented yet")));
    }
}