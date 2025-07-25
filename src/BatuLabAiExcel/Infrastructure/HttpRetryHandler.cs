using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace BatuLabAiExcel.Infrastructure;

/// <summary>
/// HTTP retry handler for handling transient failures and rate limiting
/// </summary>
public class HttpRetryHandler : DelegatingHandler
{
    private readonly ILogger<HttpRetryHandler> _logger;
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;

    public HttpRetryHandler(ILogger<HttpRetryHandler> logger, int maxRetries = 3, TimeSpan? baseDelay = null)
        : base(new HttpClientHandler())
    {
        _logger = logger;
        _maxRetries = maxRetries;
        _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        Exception? lastException = null;

        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                // Clone request for retry attempts
                var requestCopy = await CloneRequestAsync(request);
                response = await base.SendAsync(requestCopy, cancellationToken);

                // Check if we should retry
                if (ShouldRetry(response) && attempt < _maxRetries)
                {
                    _logger.LogWarning("HTTP request failed with {StatusCode}, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})",
                        response.StatusCode, GetRetryDelay(attempt).TotalMilliseconds, attempt + 1, _maxRetries);

                    response.Dispose();
                    await Task.Delay(GetRetryDelay(attempt), cancellationToken);
                    continue;
                }

                // Success or final attempt
                return response;
            }
            catch (Exception ex) when (IsTransientException(ex) && attempt < _maxRetries)
            {
                lastException = ex;
                _logger.LogWarning(ex, "HTTP request threw exception, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})",
                    GetRetryDelay(attempt).TotalMilliseconds, attempt + 1, _maxRetries);

                await Task.Delay(GetRetryDelay(attempt), cancellationToken);
            }
        }

        // If we get here, all retries failed
        if (lastException != null)
        {
            throw lastException;
        }

        return response!;
    }

    private static bool ShouldRetry(HttpResponseMessage response)
    {
        return response.StatusCode == HttpStatusCode.TooManyRequests ||
               response.StatusCode == HttpStatusCode.InternalServerError ||
               response.StatusCode == HttpStatusCode.BadGateway ||
               response.StatusCode == HttpStatusCode.ServiceUnavailable ||
               response.StatusCode == HttpStatusCode.GatewayTimeout;
    }

    private static bool IsTransientException(Exception exception)
    {
        return exception is HttpRequestException ||
               exception is TaskCanceledException ||
               exception is SocketException;
    }

    private TimeSpan GetRetryDelay(int attempt)
    {
        // Exponential backoff with jitter
        var delay = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
        return delay + jitter;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri)
        {
            Version = original.Version
        };

        // Copy headers
        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copy content
        if (original.Content != null)
        {
            var originalContent = await original.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(originalContent);

            // Copy content headers
            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}