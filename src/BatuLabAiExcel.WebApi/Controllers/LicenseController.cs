using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Services;

namespace BatuLabAiExcel.WebApi.Controllers;

/// <summary>
/// License management controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("General")]
public class LicenseController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<LicenseController> _logger;

    public LicenseController(IUserManagementService userManagementService, ILogger<LicenseController> logger)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Validate license key
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<LicenseValidationResponse>> ValidateLicenseKey(
        [FromBody] LicenseValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new LicenseValidationResponse
            {
                IsValid = false,
                Message = "Invalid request data",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        var result = await _userManagementService.ValidateLicenseKeyAsync(request.LicenseKey, cancellationToken);
        
        if (!result.IsValid)
        {
            return Ok(result); // Return 200 OK even for invalid licenses, client should check IsValid
        }

        return Ok(result);
    }

    /// <summary>
    /// Get current user's license information (requires authentication)
    /// </summary>
    [HttpGet("current")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ApiLicenseInfo>>> GetCurrentLicense(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<ApiLicenseInfo>.ErrorResult("Invalid user token"));
        }

        var result = await _userManagementService.GetUserLicenseAsync(userId, cancellationToken);
        
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Extend trial license (requires authentication)
    /// </summary>
    [HttpPost("extend-trial")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ApiLicenseInfo>>> ExtendTrial(
        [FromBody] int days,
        CancellationToken cancellationToken = default)
    {
        if (days <= 0 || days > 30)
        {
            return BadRequest(ApiResponse<ApiLicenseInfo>.ErrorResult("Days must be between 1 and 30"));
        }

        var userIdClaim = User.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<ApiLicenseInfo>.ErrorResult("Invalid user token"));
        }

        var result = await _userManagementService.ExtendTrialAsync(userId, days, cancellationToken);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Cancel current subscription (requires authentication)
    /// </summary>
    [HttpPost("cancel-subscription")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> CancelSubscription(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse.ErrorResult("Invalid user token"));
        }

        var result = await _userManagementService.CancelSubscriptionAsync(userId, cancellationToken);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}