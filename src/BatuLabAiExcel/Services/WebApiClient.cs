using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BatuLabAiExcel.Models;
using BatuLabAiExcel.Models.DTOs;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Web API client implementation for secure server communication
/// </summary>
public class WebApiClient : IWebApiClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _authToken;

    public WebApiClient(HttpClient httpClient, ILogger<WebApiClient> logger, IOptions<AppConfiguration.WebApiSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        var webApiSettings = settings.Value;
        _httpClient.BaseAddress = new Uri(webApiSettings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(webApiSettings.TimeoutSeconds);
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_authToken);

    public async Task<Result<AuthenticationResult>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting API login for email: {Email}", email);

            var request = new
            {
                Email = email,
                Password = password
            };

            var response = await PostAsync<dynamic>("api/auth/login", request, cancellationToken);
            if (!response.IsSuccess)
            {
                return Result<AuthenticationResult>.Failure(response.Message ?? "Login failed");
            }

            JsonElement responseData;
            try
            {
                responseData = (JsonElement)response.Data;
            }
            catch (Exception ex)
            {
                return Result<AuthenticationResult>.Failure($"Failed to parse login response: {ex.Message}");
            }
            
            // Check for success property (case-insensitive)
            if (!responseData.TryGetProperty("success", out var successProp) && 
                !responseData.TryGetProperty("Success", out successProp))
            {
                return Result<AuthenticationResult>.Failure("Invalid login response format");
            }

            if (!successProp.GetBoolean())
            {
                var message = responseData.TryGetProperty("message", out var msgProp) || 
                             responseData.TryGetProperty("Message", out msgProp) ? 
                             msgProp.GetString() : "Login failed";
                return Result<AuthenticationResult>.Failure(message ?? "Login failed");
            }

            // Get token (case-insensitive)
            string? token = null;
            if (responseData.TryGetProperty("token", out var tokenProp) || 
                responseData.TryGetProperty("Token", out tokenProp))
            {
                token = tokenProp.GetString();
            }

            if (string.IsNullOrEmpty(token))
            {
                return Result<AuthenticationResult>.Failure("No token received from server");
            }

            // Get user info (case-insensitive)
            JsonElement user;
            if (!responseData.TryGetProperty("user", out user) && 
                !responseData.TryGetProperty("User", out user))
            {
                return Result<AuthenticationResult>.Failure("No user info received from server");
            }
            
            SetAuthToken(token);

            // Extract user properties safely
            string? userId = null;
            string? userEmail = null;
            string? fullName = null;

            if (user.TryGetProperty("id", out var idProp))
                userId = idProp.GetString();
            else if (user.TryGetProperty("Id", out idProp))
                userId = idProp.GetString();

            if (user.TryGetProperty("email", out var emailProp))
                userEmail = emailProp.GetString();
            else if (user.TryGetProperty("Email", out emailProp))
                userEmail = emailProp.GetString();

            if (user.TryGetProperty("fullName", out var fullNameProp))
                fullName = fullNameProp.GetString();
            else if (user.TryGetProperty("FullName", out fullNameProp))
                fullName = fullNameProp.GetString();

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(fullName))
            {
                return Result<AuthenticationResult>.Failure("Incomplete user data received from server");
            }

            var authResult = new AuthenticationResult
            {
                UserId = Guid.Parse(userId),
                Email = userEmail,
                FullName = fullName,
                Token = token
            };

            _logger.LogInformation("API login successful for user: {UserId}", authResult.UserId);
            return Result<AuthenticationResult>.Success(authResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during API login for email: {Email}", email);
            return Result<AuthenticationResult>.Failure("Login failed: " + ex.Message);
        }
    }

    public async Task<Result<AuthenticationResult>> RegisterAsync(string email, string password, string fullName, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Attempting API registration for email: {Email}", email);

            var request = new
            {
                Email = email,
                Password = password,
                FullName = fullName
            };

            var response = await PostAsync<dynamic>("api/auth/register", request, cancellationToken);
            if (!response.IsSuccess)
            {
                return Result<AuthenticationResult>.Failure(response.Message ?? "Registration failed");
            }

            JsonElement responseData;
            try
            {
                responseData = (JsonElement)response.Data;
            }
            catch (Exception ex)
            {
                return Result<AuthenticationResult>.Failure($"Failed to parse registration response: {ex.Message}");
            }
            
            // Check for success property (case-insensitive)
            if (!responseData.TryGetProperty("success", out var successProp) && 
                !responseData.TryGetProperty("Success", out successProp))
            {
                return Result<AuthenticationResult>.Failure("Invalid registration response format");
            }

            if (!successProp.GetBoolean())
            {
                var message = responseData.TryGetProperty("message", out var msgProp) || 
                             responseData.TryGetProperty("Message", out msgProp) ? 
                             msgProp.GetString() : "Registration failed";
                return Result<AuthenticationResult>.Failure(message ?? "Registration failed");
            }

            // Get token (case-insensitive)
            string? token = null;
            if (responseData.TryGetProperty("token", out var tokenProp) || 
                responseData.TryGetProperty("Token", out tokenProp))
            {
                token = tokenProp.GetString();
            }

            if (string.IsNullOrEmpty(token))
            {
                return Result<AuthenticationResult>.Failure("No token received from server");
            }

            // Get user info (case-insensitive)
            JsonElement user;
            if (!responseData.TryGetProperty("user", out user) && 
                !responseData.TryGetProperty("User", out user))
            {
                return Result<AuthenticationResult>.Failure("No user info received from server");
            }
            
            SetAuthToken(token);

            // Extract user properties safely
            string? userIdReg = null;
            string? emailReg = null;
            string? fullNameReg = null;

            if (user.TryGetProperty("id", out var idPropReg))
                userIdReg = idPropReg.GetString();
            else if (user.TryGetProperty("Id", out idPropReg))
                userIdReg = idPropReg.GetString();

            if (user.TryGetProperty("email", out var emailPropReg))
                emailReg = emailPropReg.GetString();
            else if (user.TryGetProperty("Email", out emailPropReg))
                emailReg = emailPropReg.GetString();

            if (user.TryGetProperty("fullName", out var fullNamePropReg))
                fullNameReg = fullNamePropReg.GetString();
            else if (user.TryGetProperty("FullName", out fullNamePropReg))
                fullNameReg = fullNamePropReg.GetString();

            if (string.IsNullOrEmpty(userIdReg) || string.IsNullOrEmpty(emailReg) || string.IsNullOrEmpty(fullNameReg))
            {
                return Result<AuthenticationResult>.Failure("Incomplete user data received from server");
            }

            var authResult = new AuthenticationResult
            {
                UserId = Guid.Parse(userIdReg),
                Email = emailReg,
                FullName = fullNameReg,
                Token = token
            };

            _logger.LogInformation("API registration successful for user: {UserId}", authResult.UserId);
            return Result<AuthenticationResult>.Success(authResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during API registration for email: {Email}", email);
            return Result<AuthenticationResult>.Failure("Registration failed: " + ex.Message);
        }
    }

    public async Task<UserInfo?> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating token through Web API");

            var response = await PostAsync<dynamic>("api/auth/validate", token, cancellationToken);
            
            if (!response.IsSuccess)
            {
                return null;
            }

            var responseData = (JsonElement)response.Data;
            
            if (!responseData.GetProperty("success").GetBoolean())
            {
                return null;
            }

            var userData = responseData.GetProperty("data");
            var userInfo = JsonSerializer.Deserialize<UserInfo>(userData.GetRawText(), _jsonOptions)!;

            _logger.LogInformation("Token validation successful");
            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token through API");
            return null;
        }
    }

    public async Task<Result<LicenseInfo>> ValidateLicenseAsync(string licenseKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating license key through API");

            var request = new { LicenseKey = licenseKey };
            var response = await PostAsync<dynamic>("api/license/validate", request, cancellationToken);
            
            if (!response.IsSuccess)
            {
                return Result<LicenseInfo>.Failure(response.Message ?? "Operation failed");
            }

            var responseData = (JsonElement)response.Data;
            
            if (!responseData.GetProperty("isValid").GetBoolean())
            {
                var message = responseData.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "License validation failed";
                return Result<LicenseInfo>.Failure(message ?? "License validation failed");
            }

            var license = responseData.GetProperty("license");
            var licenseInfo = JsonSerializer.Deserialize<LicenseInfo>(license.GetRawText(), _jsonOptions)!;

            _logger.LogInformation("License validation successful");
            return Result<LicenseInfo>.Success(licenseInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating license through API");
            return Result<LicenseInfo>.Failure("License validation failed: " + ex.Message);
        }
    }

    public async Task<Result<LicenseInfo>> GetUserLicenseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting user license through API");

            var response = await GetAsync<dynamic>("api/license/current", cancellationToken);
            if (!response.IsSuccess)
            {
                return Result<LicenseInfo>.Failure(response.Message ?? "Operation failed");
            }

            var responseData = (JsonElement)response.Data;
            
            if (!responseData.GetProperty("success").GetBoolean())
            {
                var message = responseData.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Failed to get license";
                return Result<LicenseInfo>.Failure(message ?? "Failed to get license");
            }

            var license = responseData.GetProperty("data");
            var licenseInfo = JsonSerializer.Deserialize<LicenseInfo>(license.GetRawText(), _jsonOptions)!;

            _logger.LogInformation("User license retrieved successfully");
            return Result<LicenseInfo>.Success(licenseInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user license through API");
            return Result<LicenseInfo>.Failure("Failed to get license: " + ex.Message);
        }
    }

    public async Task<Result<List<SubscriptionPlan>>> GetSubscriptionPlansAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting subscription plans through API");

            var response = await GetAsync<dynamic>("api/payment/plans", cancellationToken);
            if (!response.IsSuccess)
            {
                return Result<List<SubscriptionPlan>>.Failure(response.Message ?? "Operation failed");
            }

            var responseData = (JsonElement)response.Data;
            var plansData = responseData.GetProperty("data");
            var plans = JsonSerializer.Deserialize<List<SubscriptionPlan>>(plansData.GetRawText(), _jsonOptions)!;

            _logger.LogInformation("Subscription plans retrieved successfully");
            return Result<List<SubscriptionPlan>>.Success(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plans through API");
            return Result<List<SubscriptionPlan>>.Failure("Failed to get plans: " + ex.Message);
        }
    }

    public async Task<Result<PaymentResponse>> CreateCheckoutSessionAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating checkout session through API");

            var apiRequest = new
            {
                LicenseType = request.LicenseType,
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl
            };

            var response = await PostAsync<dynamic>("api/payment/create-checkout", apiRequest, cancellationToken);
            if (!response.IsSuccess)
            {
                return Result<PaymentResponse>.Failure(response.Message ?? "Operation failed");
            }

            var responseData = (JsonElement)response.Data;
            
            if (!responseData.GetProperty("success").GetBoolean())
            {
                var message = responseData.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Checkout creation failed";
                return Result<PaymentResponse>.Failure(message ?? "Checkout creation failed");
            }

            var paymentResponse = new PaymentResponse
            {
                Success = true,
                Message = responseData.GetProperty("message").GetString() ?? "Success",
                CheckoutUrl = responseData.GetProperty("checkoutUrl").GetString(),
                SessionId = responseData.GetProperty("sessionId").GetString()
            };

            _logger.LogInformation("Checkout session created successfully");
            return Result<PaymentResponse>.Success(paymentResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session through API");
            return Result<PaymentResponse>.Failure("Checkout creation failed: " + ex.Message);
        }
    }

    public async Task<Result> VerifyPaymentAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying payment through API");

            var response = await PostAsync<dynamic>($"api/payment/verify/{sessionId}", null, cancellationToken);
            if (!response.IsSuccess)
            {
                return Result.Failure(response.Message ?? "Operation failed");
            }

            _logger.LogInformation("Payment verification successful");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment through API");
            return Result.Failure("Payment verification failed: " + ex.Message);
        }
    }

    public async Task<Result> CancelSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Cancelling subscription through API");

            var response = await PostAsync<dynamic>("api/license/cancel-subscription", null, cancellationToken);
            if (!response.IsSuccess)
            {
                return Result.Failure(response.Message ?? "Operation failed");
            }

            _logger.LogInformation("Subscription cancellation successful");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription through API");
            return Result.Failure("Subscription cancellation failed: " + ex.Message);
        }
    }

    public async Task<Result<LicenseInfo>> ExtendTrialAsync(int days, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Extending trial through API");

            var response = await PostAsync<dynamic>("api/license/extend-trial", days, cancellationToken);
            if (!response.IsSuccess)
            {
                return Result<LicenseInfo>.Failure(response.Message ?? "Operation failed");
            }

            var responseData = (JsonElement)response.Data;
            var license = responseData.GetProperty("data");
            var licenseInfo = JsonSerializer.Deserialize<LicenseInfo>(license.GetRawText(), _jsonOptions)!;

            _logger.LogInformation("Trial extension successful");
            return Result<LicenseInfo>.Success(licenseInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending trial through API");
            return Result<LicenseInfo>.Failure("Trial extension failed: " + ex.Message);
        }
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearAuthToken()
    {
        _authToken = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    private async Task<Result<T>> GetAsync<T>(string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            return await ProcessResponse<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GET request to {Endpoint}", endpoint);
            return Result<T>.Failure("Request failed: " + ex.Message);
        }
    }

    private async Task<Result<T>> PostAsync<T>(string endpoint, object? data, CancellationToken cancellationToken)
    {
        try
        {
            var json = data != null ? JsonSerializer.Serialize(data, _jsonOptions) : "{}";
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            return await ProcessResponse<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during POST request to {Endpoint}", endpoint);
            return Result<T>.Failure("Request failed: " + ex.Message);
        }
    }

    private async Task<Result<T>> ProcessResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("API request failed with status {StatusCode}: {Content}", response.StatusCode, content);
            
            try
            {
                var errorResponse = JsonSerializer.Deserialize<JsonElement>(content, _jsonOptions);
                var message = errorResponse.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "API request failed";
                return Result<T>.Failure(message ?? "API request failed");
            }
            catch
            {
                return Result<T>.Failure($"API request failed with status {response.StatusCode}");
            }
        }

        try
        {
            var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
            return Result<T>.Success(data!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing API response");
            return Result<T>.Failure("Invalid response format");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}