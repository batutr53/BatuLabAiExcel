using BatuLabAiExcel.WebApi.Data;
using BatuLabAiExcel.WebApi.Models;
using BatuLabAiExcel.WebApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BatuLabAiExcel.WebApi.Services;

/// <summary>
/// API authentication service implementation
/// </summary>
public class ApiAuthenticationService : IApiAuthenticationService
{
    private readonly AppDbContext _context;
    private readonly IAuthenticationService _authService;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<ApiAuthenticationService> _logger;
    private readonly AppConfiguration.AuthenticationSettings _authSettings;

    public ApiAuthenticationService(
        AppDbContext context,
        IAuthenticationService authService,
        ILicenseService licenseService,
        ILogger<ApiAuthenticationService> logger,
        IOptions<AppConfiguration.AuthenticationSettings> authSettings)
    {
        _context = context;
        _authService = authService;
        _licenseService = licenseService;
        _logger = logger;
        _authSettings = authSettings.Value;
    }

    public async Task<ApiAuthResponse> LoginAsync(ApiLoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("API login attempt for email: {Email}", request.Email);

            var authResult = await _authService.AuthenticateAsync(request.Email, request.Password, cancellationToken);

            if (!authResult.IsSuccess)
            {
                return new ApiAuthResponse
                {
                    Success = false,
                    Message = authResult.Error ?? "Authentication failed",
                    Errors = new List<string> { authResult.Error ?? "Invalid credentials" }
                };
            }

            var user = authResult.Data;
            if (user == null)
            {
                return new ApiAuthResponse
                {
                    Success = false,
                    Message = "User not found",
                    Errors = new List<string> { "User not found" }
                };
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(_authSettings.TokenExpiryHours);

            // Get user with license info
            var userInfo = await GetUserWithLicenseAsync(user.Id, cancellationToken);

            _logger.LogInformation("API login successful for user: {UserId}", user.Id);

            return new ApiAuthResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                ExpiresAt = expiresAt,
                User = userInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during API login for email: {Email}", request.Email);
            return new ApiAuthResponse
            {
                Success = false,
                Message = "Internal server error",
                Errors = new List<string> { "Authentication service error" }
            };
        }
    }

    public async Task<ApiAuthResponse> RegisterAsync(ApiRegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("API registration attempt for email: {Email}", request.Email);

            var registerResult = await _authService.RegisterAsync(request.Email, request.Password, request.FullName, cancellationToken);

            if (!registerResult.IsSuccess)
            {
                return new ApiAuthResponse
                {
                    Success = false,
                    Message = registerResult.Error ?? "Registration failed",
                    Errors = new List<string> { registerResult.Error ?? "Registration failed" }
                };
            }

            var user = registerResult.Data;
            if (user == null)
            {
                return new ApiAuthResponse
                {
                    Success = false,
                    Message = "Registration failed",
                    Errors = new List<string> { "Failed to create user" }
                };
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(_authSettings.TokenExpiryHours);

            // Get user with license info (should have trial license)
            var userInfo = await GetUserWithLicenseAsync(user.Id, cancellationToken);

            _logger.LogInformation("API registration successful for user: {UserId}", user.Id);

            return new ApiAuthResponse
            {
                Success = true,
                Message = "Registration successful",
                Token = token,
                ExpiresAt = expiresAt,
                User = userInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during API registration for email: {Email}", request.Email);
            return new ApiAuthResponse
            {
                Success = false,
                Message = "Internal server error",
                Errors = new List<string> { "Registration service error" }
            };
        }
    }

    public async Task<ApiUserInfo?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_authSettings.JwtSecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _authSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _authSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            return await GetUserWithLicenseAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    public async Task<ApiUserInfo?> GetUserWithLicenseAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.Licenses.Where(l => l.IsActive))
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                return null;
            }

            var activeLicense = user.Licenses.FirstOrDefault(l => l.IsActive);
            ApiLicenseInfo? licenseInfo = null;

            if (activeLicense != null)
            {
                var daysRemaining = activeLicense.ExpiresAt.HasValue
                    ? Math.Max(0, (int)(activeLicense.ExpiresAt.Value - DateTime.UtcNow).TotalDays)
                    : int.MaxValue;

                licenseInfo = new ApiLicenseInfo
                {
                    Id = activeLicense.Id,
                    Type = activeLicense.Type,
                    LicenseKey = activeLicense.LicenseKey,
                    IsActive = activeLicense.IsActive,
                    ExpiresAt = activeLicense.ExpiresAt,
                    CreatedAt = activeLicense.CreatedAt,
                    DaysRemaining = daysRemaining
                };
            }

            return new ApiUserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                CreatedAt = user.CreatedAt,
                IsActive = user.IsActive,
                License = licenseInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user with license: {UserId}", userId);
            return null;
        }
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_authSettings.JwtSecretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("user_id", user.Id.ToString()),
                new Claim("email", user.Email)
            }),
            Expires = DateTime.UtcNow.AddHours(_authSettings.TokenExpiryHours),
            Issuer = _authSettings.Issuer,
            Audience = _authSettings.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}