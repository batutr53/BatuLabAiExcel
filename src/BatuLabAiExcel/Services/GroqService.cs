using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Service for interacting with Groq API
/// </summary>
public class GroqService : IGroqService
{
    private readonly HttpClient _httpClient;
    private readonly AppConfiguration.GroqSettings _settings;
    private readonly ILogger<GroqService> _logger;
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private static readonly object _requestLock = new object();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public GroqService(
        HttpClient httpClient,
        IOptions<AppConfiguration.GroqSettings> settings,
        ILogger<GroqService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        ConfigureHttpClient();
    }

    public async Task<Result<GroqResponse>> SendMessageAsync(
        List<GroqMessage> messages,
        List<GroqFunction>? functions = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Rate limiting: ensure delay between requests
            await EnsureRequestDelayAsync(cancellationToken);
            
            if (string.IsNullOrEmpty(_settings.ApiKey))
            {
                return Result<GroqResponse>.Failure("Groq API key is not configured");
            }

            var request = new GroqRequest
            {
                Model = _settings.Model,
                Messages = messages,
                Temperature = _settings.Temperature,
                MaxTokens = _settings.MaxTokens,
                TopP = _settings.TopP,
                Stream = false
            };

            if (functions?.Any() == true)
            {
                request.Tools = functions.Select(f => new GroqTool 
                { 
                    Type = "function", 
                    Function = f 
                }).ToList();
                request.ToolChoice = "auto";
            }

            var jsonRequest = JsonSerializer.Serialize(request, JsonOptions);
            _logger.LogDebug("Sending Groq request: {Request}", 
                jsonRequest.Length > 1000 ? $"{jsonRequest[..1000]}..." : jsonRequest);

            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            
            var endpoint = "chat/completions";
            using var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Groq API error: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);

                try
                {
                    var errorResponse = JsonSerializer.Deserialize<GroqErrorResponse>(responseContent, JsonOptions);
                    return Result<GroqResponse>.Failure(
                        $"Groq API error ({response.StatusCode}): {errorResponse?.Error?.Message ?? responseContent}");
                }
                catch
                {
                    return Result<GroqResponse>.Failure(
                        $"Groq API error ({response.StatusCode}): {responseContent}");
                }
            }

            var groqResponse = JsonSerializer.Deserialize<GroqResponse>(responseContent, JsonOptions);
            if (groqResponse == null)
            {
                return Result<GroqResponse>.Failure("Failed to deserialize Groq response");
            }

            _logger.LogInformation("Groq response received: {TokensUsed} tokens used",
                groqResponse.Usage?.TotalTokens ?? 0);

            return Result<GroqResponse>.Success(groqResponse);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Groq request timed out");
            return Result<GroqResponse>.Failure("Request timed out. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Groq API");
            return Result<GroqResponse>.Failure($"Network error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON serialization error");
            return Result<GroqResponse>.Failure($"Data format error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Groq API");
            return Result<GroqResponse>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        _logger.LogInformation("Groq service configured with API key: {MaskedKey}", 
            _settings.GetMaskedApiKey());
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