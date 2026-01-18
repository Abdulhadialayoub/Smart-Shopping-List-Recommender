using System.Net;
using Polly;
using Polly.Retry;
using Microsoft.Extensions.Options;

namespace SmartShopper.Api.Services;

/// <summary>
/// Service for managing HTTP requests to Cimri.com with retry policies and rate limiting
/// </summary>
public class CimriHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly IUserAgentProvider _userAgentProvider;
    private readonly ILogger<CimriHttpClientService> _logger;
    private readonly CimriScraperOptions _options;
    private readonly Random _random = new();
    private DateTime _lastRequestTime = DateTime.MinValue;
    private readonly SemaphoreSlim _rateLimitSemaphore = new(1, 1);
    private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

    public CimriHttpClientService(
        HttpClient httpClient,
        IUserAgentProvider userAgentProvider,
        ILogger<CimriHttpClientService> logger,
        IOptions<CimriScraperOptions> options)
    {
        _httpClient = httpClient;
        _userAgentProvider = userAgentProvider;
        _logger = logger;
        _options = options.Value;

        // Configure HttpClient timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);

        // Configure Polly retry policy with exponential backoff
        _retryPolicy = Policy
            .HandleResult<HttpResponseMessage>(r => 
                !r.IsSuccessStatusCode && r.StatusCode != HttpStatusCode.NotFound)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: _options.MaxRetries,
                sleepDurationProvider: (retryAttempt, result, context) =>
                {
                    // Special handling for HTTP 429 (Too Many Requests)
                    if (result.Result?.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogWarning("HTTP 429 received, waiting 5 seconds before retry {RetryAttempt}/{MaxRetries}", 
                            retryAttempt, _options.MaxRetries);
                        return TimeSpan.FromSeconds(5);
                    }

                    // Exponential backoff for other errors
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * _options.RetryDelaySeconds);
                    _logger.LogWarning("Retry attempt {RetryAttempt}/{MaxRetries}, waiting {Delay}ms", 
                        retryAttempt, _options.MaxRetries, delay.TotalMilliseconds);
                    return delay;
                },
                onRetryAsync: async (result, timespan, retryAttempt, context) =>
                {
                    var statusCode = result.Result?.StatusCode.ToString() ?? "Exception";
                    var exception = result.Exception?.Message ?? "N/A";
                    _logger.LogWarning(
                        "Retry {RetryAttempt}/{MaxRetries} after {Delay}ms. Status: {StatusCode}, Exception: {Exception}",
                        retryAttempt, _options.MaxRetries, timespan.TotalMilliseconds, statusCode, exception);
                    await Task.CompletedTask;
                });
    }

    /// <summary>
    /// Sends an HTTP GET request with retry policy and rate limiting
    /// </summary>
    public async Task<HttpResponseMessage> GetAsync(string url)
    {
        // Apply rate limiting (1-3 seconds between requests)
        await ApplyRateLimitAsync();

        // Get random User Agent for this request
        var userAgent = _userAgentProvider.GetRandomUserAgent();
        
        _logger.LogInformation("Making request to {Url} with User-Agent: {UserAgent}", url, userAgent);

        // Execute request with retry policy
        var response = await _retryPolicy.ExecuteAsync(async () =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            // Set User-Agent
            request.Headers.Add("User-Agent", userAgent);
            
            // Add realistic browser headers
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            request.Headers.Add("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            request.Headers.Add("Cache-Control", "max-age=0");
            request.Headers.Add("sec-ch-ua", "\"Chromium\";v=\"118\", \"Google Chrome\";v=\"118\", \"Not=A?Brand\";v=\"99\"");
            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
            request.Headers.Add("Sec-Fetch-Dest", "document");
            request.Headers.Add("Sec-Fetch-Mode", "navigate");
            request.Headers.Add("Sec-Fetch-Site", "none");
            request.Headers.Add("Sec-Fetch-User", "?1");
            request.Headers.Add("Upgrade-Insecure-Requests", "1");
            
            // Add referer if this is not the first request
            if (url.Contains("?"))
            {
                var baseUrl = url.Split('?')[0];
                request.Headers.Add("Referer", "https://www.cimri.com/");
            }

            return await _httpClient.SendAsync(request);
        });

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Successfully fetched {Url}, Status: {StatusCode}", url, response.StatusCode);
        }
        else
        {
            _logger.LogError("Failed to fetch {Url} after all retries, Status: {StatusCode}", url, response.StatusCode);
        }

        return response;
    }

    /// <summary>
    /// Applies rate limiting by ensuring minimum delay between requests
    /// </summary>
    private async Task ApplyRateLimitAsync()
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            var minDelay = TimeSpan.FromMilliseconds(_options.MinDelayMs);
            var maxDelay = TimeSpan.FromMilliseconds(_options.MaxDelayMs);

            if (timeSinceLastRequest < minDelay)
            {
                // Calculate random delay between min and max
                var randomDelayMs = _random.Next(_options.MinDelayMs, _options.MaxDelayMs);
                var delayTime = TimeSpan.FromMilliseconds(randomDelayMs);
                
                _logger.LogDebug("Rate limiting: waiting {Delay}ms before next request", delayTime.TotalMilliseconds);
                await Task.Delay(delayTime);
            }
            else if (timeSinceLastRequest < maxDelay)
            {
                // If we're between min and max, add a small random delay
                var remainingDelay = maxDelay - timeSinceLastRequest;
                if (remainingDelay > TimeSpan.Zero)
                {
                    var randomAdditionalMs = _random.Next(0, (int)remainingDelay.TotalMilliseconds);
                    await Task.Delay(randomAdditionalMs);
                }
            }

            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets the last request timestamp (for testing purposes)
    /// </summary>
    public DateTime GetLastRequestTime() => _lastRequestTime;
}
