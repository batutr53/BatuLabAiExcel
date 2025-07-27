using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BatuLabAiExcel.Models;

namespace BatuLabAiExcel.Services;

/// <summary>
/// Service for interacting with Claude API
/// </summary>
public class ClaudeService : IClaudeService
{
    private readonly HttpClient _httpClient;
    private readonly AppConfiguration.ClaudeSettings _settings;
    private readonly IUserSettingsService _userSettings;
    private readonly ILogger<ClaudeService> _logger;
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private static readonly object _requestLock = new object();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public ClaudeService(
        HttpClient httpClient,
        IOptions<AppConfiguration.ClaudeSettings> settings,
        IUserSettingsService userSettings,
        ILogger<ClaudeService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _userSettings = userSettings;
        _logger = logger;

        ConfigureHttpClient();
    }

    public async Task<Result<ClaudeResponse>> SendMessageAsync(
        List<ClaudeMessage> messages,
        List<ClaudeTool>? tools = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Rate limiting: ensure delay between requests
            await EnsureRequestDelayAsync(cancellationToken);
            // Get API key from user settings first, fallback to config
            var apiKey = await _userSettings.GetApiKeyAsync("Claude") ?? _settings.ApiKey;
            if (string.IsNullOrEmpty(apiKey))
            {
                return Result<ClaudeResponse>.Failure("Claude API key is not configured");
            }

            var request = new ClaudeRequest
            {
                Model = _settings.Model,
                MaxTokens = _settings.MaxTokens,
                Messages = messages,
                Temperature = _settings.Temperature,
                TopP = _settings.TopP,
                TopK = _settings.TopK,
                Stream = false
            };

            if (tools?.Any() == true)
            {
                request.Tools = tools;
                request.ToolChoice = new { type = "auto" };
            }
            else
            {
                // Ensure tool_choice is not set when no tools are provided
                request.ToolChoice = null;
            }

            var jsonRequest = JsonSerializer.Serialize(request, JsonOptions);
            _logger.LogDebug("Sending Claude request: {Request}", 
                jsonRequest.Length > 1000 ? $"{jsonRequest[..1000]}..." : jsonRequest);

            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            
            // Set authorization header with dynamic API key
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/v1/messages") { Content = content };
            requestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
            requestMessage.Headers.Add("anthropic-version", _settings.ApiVersion);

            using var response = await _httpClient.SendAsync(requestMessage, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Claude API error: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);

                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ClaudeErrorResponse>(responseContent, JsonOptions);
                    return Result<ClaudeResponse>.Failure(
                        $"Claude API error ({response.StatusCode}): {errorResponse?.Error?.Message ?? responseContent}");
                }
                catch
                {
                    return Result<ClaudeResponse>.Failure(
                        $"Claude API error ({response.StatusCode}): {responseContent}");
                }
            }

            var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseContent, JsonOptions);
            if (claudeResponse == null)
            {
                return Result<ClaudeResponse>.Failure("Failed to deserialize Claude response");
            }

            _logger.LogInformation("Claude response received: {TokensUsed} tokens used",
                claudeResponse.Usage?.OutputTokens ?? 0);

            return Result<ClaudeResponse>.Success(claudeResponse);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError("Claude request timed out");
            return Result<ClaudeResponse>.Failure("Request timed out. Please try again.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Claude API");
            return Result<ClaudeResponse>.Failure($"Network error: {ex.Message}");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON serialization error");
            return Result<ClaudeResponse>.Failure($"Data format error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Claude API");
            return Result<ClaudeResponse>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    private void ConfigureHttpClient()
    {
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("anthropic-beta", "tools-2024-04-04");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        _logger.LogInformation("Claude service configured");
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