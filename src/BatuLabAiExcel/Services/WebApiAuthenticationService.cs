using Microsoft.Extensions.Logging;
using BatuLabAiExcel.Models.DTOs;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Authentication service that uses Web API for secure authentication
/// </summary>
public class WebApiAuthenticationService : IAuthenticationService
{
    private readonly IWebApiClient _webApiClient;
    private readonly ILogger<WebApiAuthenticationService> _logger;
    private UserInfo? _currentUser;
    private bool _isAuthenticated;

    public WebApiAuthenticationService(IWebApiClient webApiClient, ILogger<WebApiAuthenticationService> logger)
    {
        _webApiClient = webApiClient;
        _logger = logger;
    }

    public bool IsAuthenticated => _isAuthenticated;
    public UserInfo? CurrentUser => _currentUser;
    public event EventHandler<bool>? AuthenticationStateChanged;

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Authenticating user through Web API: {Email}", request.Email);

            var loginResult = await _webApiClient.LoginAsync(request.Email, request.Password, cancellationToken);
            
            if (!loginResult.IsSuccess)
            {
                _logger.LogWarning("Authentication failed for user: {Email}", request.Email);
                
                // Check if it's a connection error and provide helpful message
                var errorMessage = loginResult.Error ?? "Authentication failed";
                if (errorMessage.Contains("Connection") || errorMessage.Contains("HttpRequest") || errorMessage.Contains("timeout"))
                {
                    errorMessage = "Web API sunucusuna bağlanılamadı. Lütfen sunucunun çalıştığından emin olun.";
                }
                
                return new AuthResponse
                {
                    Success = false,
                    Message = errorMessage,
                    Errors = new List<string> { errorMessage }
                };
            }

            var authResult = loginResult.Data!;
            
            // Set current user info
            _currentUser = new UserInfo
            {
                Id = authResult.UserId,
                Email = authResult.Email,
                FullName = authResult.FullName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            _isAuthenticated = true;
            _webApiClient.SetAuthToken(authResult.Token!);
            AuthenticationStateChanged?.Invoke(this, true);

            _logger.LogInformation("Authentication successful for user: {UserId}", authResult.UserId);
            return new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                Token = authResult.Token,
                ExpiresAt = DateTime.UtcNow.AddHours(24), // Default 24 hours
                User = _currentUser
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during authentication for user: {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "Authentication service error",
                Errors = new List<string> { "Authentication service error" }
            };
        }
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Registering user through Web API: {Email}", request.Email);

            var registerResult = await _webApiClient.RegisterAsync(request.Email, request.Password, request.FullName, cancellationToken);
            
            if (!registerResult.IsSuccess)
            {
                _logger.LogWarning("Registration failed for user: {Email}", request.Email);
                return new AuthResponse
                {
                    Success = false,
                    Message = registerResult.Error ?? "Registration failed",
                    Errors = new List<string> { registerResult.Error ?? "Registration failed" }
                };
            }

            var authResult = registerResult.Data!;
            
            // Set current user info
            _currentUser = new UserInfo
            {
                Id = authResult.UserId,
                Email = authResult.Email,
                FullName = authResult.FullName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            _isAuthenticated = true;
            _webApiClient.SetAuthToken(authResult.Token!);
            AuthenticationStateChanged?.Invoke(this, true);

            _logger.LogInformation("Registration successful for user: {UserId}", authResult.UserId);
            return new AuthResponse
            {
                Success = true,
                Message = "Registration successful",
                Token = authResult.Token,
                ExpiresAt = DateTime.UtcNow.AddHours(24), // Default 24 hours
                User = _currentUser
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user: {Email}", request.Email);
            return new AuthResponse
            {
                Success = false,
                Message = "Registration service error",
                Errors = new List<string> { "Registration service error" }
            };
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            _logger.LogInformation("Logging out user: {UserId}", _currentUser?.Id);
            
            _currentUser = null;
            _isAuthenticated = false;
            _webApiClient.ClearAuthToken();
            AuthenticationStateChanged?.Invoke(this, false);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
        }
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        try
        {
            if (!_isAuthenticated || _currentUser == null)
            {
                return null;
            }

            // Optionally refresh user info from API
            return await Task.FromResult(_currentUser);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            _logger.LogInformation("Validating token through Web API");
            
            var userInfo = await _webApiClient.ValidateTokenAsync(token, CancellationToken.None);
            
            if (userInfo == null)
            {
                _logger.LogWarning("Token validation failed");
                return false;
            }

            _logger.LogInformation("Token validation successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return false;
        }
    }

    public async Task<AuthResponse> RefreshTokenAsync(string token)
    {
        try
        {
            _logger.LogInformation("Token refresh through Web API not implemented");
            
            // Token refresh would require a new API endpoint
            return await Task.FromResult(new AuthResponse
            {
                Success = false,
                Message = "Token refresh not implemented in Web API mode",
                Errors = new List<string> { "Token refresh not implemented" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new AuthResponse
            {
                Success = false,
                Message = "Token refresh service error",
                Errors = new List<string> { "Token refresh service error" }
            };
        }
    }
}