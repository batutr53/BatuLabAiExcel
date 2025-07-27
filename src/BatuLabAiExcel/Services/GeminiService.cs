using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Service for interacting with Gemini API
/// </summary>
public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly AppConfiguration.GeminiSettings _settings;
    private readonly IUserSettingsService _userSettings;
    private readonly ILogger<GeminiService> _logger;
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private static readonly object _requestLock = new object();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public GeminiService(
        HttpClient httpClient,
        IOptions<AppConfiguration.GeminiSettings> settings,
        IUserSettingsService userSettings,
        ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _userSettings = userSettings;
        _logger = logger;

        ConfigureHttpClient();
    }

    public async Task<Result<GeminiResponse>> SendMessageAsync(
        List<GeminiContent> contents,
        List<GeminiFunctionDeclaration>? functions = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Rate limiting: ensure delay between requests
            await EnsureRequestDelayAsync(cancellationToken);
            
            // Get API key from user settings first, fallback to config
            var apiKey = await _userSettings.GetApiKeyAsync("Gemini") ?? _settings.ApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                return Result<GeminiResponse>.Failure("Gemini API key is not configured");
            }

            var request = new GeminiRequest
            {
                Contents = contents,
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = _settings.Temperature,
                    TopP = _settings.TopP,
                    TopK = _settings.TopK,
                    MaxOutputTokens = _settings.MaxTokens
                }
            };

            if (functions?.Any() == true)
            {
                request.Tools = new List<GeminiTool>
                {
                    new GeminiTool { FunctionDeclarations = functions }
                };
            }

            var jsonRequest = JsonSerializer.Serialize(request, JsonOptions);
            _logger.LogDebug("Sending Gemini request: {Request}", 
                jsonRequest.Length > 1000 ? $"{jsonRequest[..1000]}..." : jsonRequest);

            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            
            var endpoint = $"/models/{_settings.Model}:generateContent?key={apiKey}";
            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);

                try
                {
                    var errorResponse = JsonSerializer.Deserialize<GeminiErrorResponse>(responseContent, JsonOptions);
                    return Result<GeminiResponse>.Failure(
                        $"Gemini API error ({response.StatusCode}): {errorResponse?.Error?.Message ?? responseContent}");
                }
                catch
                {
                    return Result<GeminiResponse>.Failure(
                        $"Gemini API error ({response.StatusCode}): {responseContent}");
                }
            }

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent, JsonOptions);
            if (geminiResponse == null)
            {
                return Result<GeminiResponse>.Failure("Failed to deserialize Gemini response");
            }

            _logger.LogInformation("Gemini response received: {TokensUsed} tokens used",
                geminiResponse.UsageMetadata?.TotalTokenCount ?? 0);

            return Result<GeminiResponse>.Success(geminiResponse);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Gemini request timed out");
            return Result<GeminiResponse>.Failure("Request timed out. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Gemini API");
            return Result<GeminiResponse>.Failure($"Network error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON serialization error");
            return Result<GeminiResponse>.Failure($"Data format error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Gemini API");
            return Result<GeminiResponse>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        _logger.LogInformation("Gemini service configured");
    }

    private async Task EnsureRequestDelayAsync(CancellationToken cancellationToken)
    {
        TimeSpan waitTime;
        
        lock (_requestLock)
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var requiredDelay = TimeSpan.FromMilliseconds(_settings.RequestDelayMs);
            
            if (timeSinceLastRequest < requiredDelay)
            {
                waitTime = requiredDelay - timeSinceLastRequest;
            }
            else
            {
                waitTime = TimeSpan.Zero;
            }
        }
        
        if (waitTime > TimeSpan.Zero)
        {
            _logger.LogDebug("Rate limiting: waiting {WaitMs}ms before next request", waitTime.TotalMilliseconds);
            await Task.Delay(waitTime, cancellationToken);
        }
        
        lock (_requestLock)
        {
            _lastRequestTime = DateTime.UtcNow;
        }
    }
}