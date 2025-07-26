using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using BatuLabAiExcel.Data;
using BatuLabAiExcel.Models.DTOs;
using BatuLabAiExcel.Models.Entities;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Authentication service implementation
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly ISecureStorageService _secureStorage;
    private readonly ILicenseService _licenseService;
    private readonly string _jwtSecret;
    private const int TokenExpiryHours = 24;

    public AuthenticationService(
        AppDbContext context,
        ILogger<AuthenticationService> logger,
        ISecureStorageService secureStorage,
        ILicenseService licenseService)
    {
        _context = context;
        _logger = logger;
        _secureStorage = secureStorage;
        _licenseService = licenseService;
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "your-super-secret-jwt-key-change-in-production";
    }

    public UserInfo? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    public event EventHandler<bool>? AuthenticationStateChanged;

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Login attempt for user: {Email}", request.Email);

            var user = await _context.Users
                .Include(u => u.Licenses)
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found {Email}", request.Email);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password",
                    Errors = ["Invalid credentials"]
                };
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: Invalid password for {Email}", request.Email);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Invalid email or password",
                    Errors = ["Invalid credentials"]
                };
            }

            // Check license validity
            var activeLicense = user.ActiveLicense;
            if (activeLicense == null || !activeLicense.IsValid)
            {
                _logger.LogWarning("Login blocked: No valid license for {Email}", request.Email);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Your license has expired. Please renew your subscription to continue using the application.",
                    Errors = ["License expired"]
                };
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            var token = GenerateJwtToken(user);
            CurrentUser = UserInfo.FromEntity(user);

            // Store credentials securely if RememberMe is true
            if (request.RememberMe)
            {
                await _secureStorage.StoreCredentialsAsync(request.Email, token);
            }

            _logger.LogInformation("User logged in successfully: {Email}", request.Email);
            AuthenticationStateChanged?.Invoke(this, true);

            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                User = CurrentUser,
                License = LicenseInfo.FromEntity(activeLicense)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during login. Please try again.",
                Errors = ["Internal error"]
            };
        }
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Registration attempt for user: {Email}", request.Email);

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (existingUser != null)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "An account with this email already exists",
                    Errors = ["Email already registered"]
                };
            }

            // Create new user
            var user = new User
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            // Create 1-day trial license
            var trialLicense = await _licenseService.CreateTrialLicenseAsync(user.Id, cancellationToken);

            if (trialLicense == null)
            {
                _logger.LogError("Failed to create trial license for user: {Email}", request.Email);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Registration completed but failed to create trial license",
                    Errors = ["Trial license creation failed"]
                };
            }

            var token = GenerateJwtToken(user);
            CurrentUser = UserInfo.FromEntity(user);

            _logger.LogInformation("User registered successfully: {Email}", request.Email);
            AuthenticationStateChanged?.Invoke(this, true);

            return new AuthResponse
            {
                Success = true,
                Message = "Registration successful! You have been granted a 1-day trial license.",
                Token = token,
                User = CurrentUser,
                License = LicenseInfo.FromEntity(trialLicense)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "An error occurred during registration. Please try again.",
                Errors = ["Internal error"]
            };
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            if (CurrentUser != null)
            {
                _logger.LogInformation("User logged out: {Email}", CurrentUser.Email);
            }

            await _secureStorage.ClearCredentialsAsync();
            CurrentUser = null;
            AuthenticationStateChanged?.Invoke(this, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    public Task<UserInfo?> GetCurrentUserAsync()
    {
        return Task.FromResult(CurrentUser);
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "userId")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return false;

            // Load user and verify still active with valid license
            var user = await _context.Users
                .Include(u => u.Licenses)
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user?.ActiveLicense?.IsValid != true)
                return false;

            CurrentUser = UserInfo.FromEntity(user);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(string token)
    {
        if (!await ValidateTokenAsync(token) || CurrentUser == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "Invalid token",
                Errors = ["Token validation failed"]
            };
        }

        var user = await _context.Users
            .Include(u => u.Licenses)
            .FirstOrDefaultAsync(u => u.Id == CurrentUser.Id);

        if (user == null)
        {
            return new AuthResponse
            {
                Success = false,
                Message = "User not found",
                Errors = ["User not found"]
            };
        }

        var newToken = GenerateJwtToken(user);
        var activeLicense = user.ActiveLicense;

        return new AuthResponse
        {
            Success = true,
            Message = "Token refreshed",
            Token = newToken,
            User = UserInfo.FromEntity(user),
            License = activeLicense != null ? LicenseInfo.FromEntity(activeLicense) : null
        };
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSecret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("userId", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("fullName", user.FullName)
            }),
            Expires = DateTime.UtcNow.AddHours(TokenExpiryHours),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}