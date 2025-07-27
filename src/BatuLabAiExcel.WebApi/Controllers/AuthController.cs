using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Services;

namespace BatuLabAiExcel.WebApi.Controllers;

/// <summary>
/// Authentication controller for user login and registration
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("Auth")]
public class AuthController : ControllerBase
{
    private readonly IApiAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IApiAuthenticationService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiAuthResponse>> Login(
        [FromBody] ApiLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiAuthResponse
            {
                Success = false,
                Message = "Invalid request data",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        var result = await _authService.LoginAsync(request, cancellationToken);
        
        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Register new user
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiAuthResponse>> Register(
        [FromBody] ApiRegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiAuthResponse
            {
                Success = false,
                Message = "Invalid request data",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
            });
        }

        var result = await _authService.RegisterAsync(request, cancellationToken);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get current user information (requires authentication)
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ApiUserInfo>>> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<ApiUserInfo>.ErrorResult("Invalid user token"));
        }

        var userInfo = await _authService.GetUserWithLicenseAsync(userId, cancellationToken);
        if (userInfo == null)
        {
            return NotFound(ApiResponse<ApiUserInfo>.ErrorResult("User not found"));
        }

        return Ok(ApiResponse<ApiUserInfo>.SuccessResult(userInfo, "User information retrieved successfully"));
    }

    /// <summary>
    /// Validate token (for testing purposes)
    /// </summary>
    [HttpPost("validate")]
    public async Task<ActionResult<ApiResponse<ApiUserInfo>>> ValidateToken(
        [FromBody] string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(ApiResponse<ApiUserInfo>.ErrorResult("Token is required"));
        }

        var userInfo = await _authService.ValidateTokenAsync(token, cancellationToken);
        if (userInfo == null)
        {
            return Unauthorized(ApiResponse<ApiUserInfo>.ErrorResult("Invalid or expired token"));
        }

        return Ok(ApiResponse<ApiUserInfo>.SuccessResult(userInfo, "Token is valid"));
    }
}