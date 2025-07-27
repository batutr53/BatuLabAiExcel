using BatuLabAiExcel.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace BatuLabAiExcel.WebApi.Controllers;

/// <summary>
/// Webhook controller for handling external webhook events (Stripe, etc.)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IPaymentService paymentService, ILogger<WebhookController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Handle Stripe webhook events
    /// </summary>
    [HttpPost("stripe")]
    public async Task<IActionResult> HandleStripeWebhook(CancellationToken cancellationToken = default)
    {
        try
        {
            // Read the raw body
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();

            // Get the Stripe signature
            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Stripe webhook received without signature");
                return BadRequest(new { error = "Missing signature" });
            }

            _logger.LogInformation("Processing Stripe webhook with signature: {Signature}",
                signature.Substring(0, Math.Min(signature.Length, 20)) + "...");

            var success = await _paymentService.HandleWebhookAsync(json, signature, cancellationToken);

            if (!success.IsSuccess)
            {
                _logger.LogError("Stripe webhook processing failed");
                return BadRequest(new { error = "Webhook processing failed" });
            }

            _logger.LogInformation("Stripe webhook processed successfully");
            return Ok(new { message = "Webhook processed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Health check endpoint for webhook monitoring
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Office AI - Batu Lab Webhook Service"
        });
    }

    /// <summary>
    /// Test endpoint for webhook validation
    /// </summary>
    [HttpPost("test")]
    public IActionResult TestWebhook([FromBody] object payload)
    {
        _logger.LogInformation("Test webhook received: {Payload}", payload);
        return Ok(new
        {
            message = "Test webhook received successfully",
            timestamp = DateTime.UtcNow,
            payload = payload
        });
    }
}