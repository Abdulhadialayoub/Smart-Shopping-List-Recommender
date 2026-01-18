using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace SmartShopper.Api.Middleware;

/// <summary>
/// Middleware for rate limiting API requests
/// Requirements: 10.4 - Rate limiting for API endpoints
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private const int MaxRequestsPerMinute = 10;
    private const string RateLimitKeyPrefix = "ratelimit_";

    public RateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _cache = cache;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply rate limiting to AI endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (!path.StartsWith("/api/ai/verified"))
        {
            await _next(context);
            return;
        }

        var clientId = GetClientIdentifier(context);
        var cacheKey = $"{RateLimitKeyPrefix}{clientId}";

        // Get or create request tracker for this client
        var requestTracker = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return new ConcurrentQueue<DateTime>();
        });

        if (requestTracker == null)
        {
            requestTracker = new ConcurrentQueue<DateTime>();
            _cache.Set(cacheKey, requestTracker, TimeSpan.FromMinutes(1));
        }

        var now = DateTime.UtcNow;
        var oneMinuteAgo = now.AddMinutes(-1);

        // Remove old requests
        while (requestTracker.TryPeek(out var oldestRequest) && oldestRequest < oneMinuteAgo)
        {
            requestTracker.TryDequeue(out _);
        }

        // Check if rate limit exceeded
        if (requestTracker.Count >= MaxRequestsPerMinute)
        {
            _logger.LogWarning("Rate limit exceeded for client: {ClientId}, path: {Path}", clientId, path);
            
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";
            
            var errorResponse = new
            {
                error = "Rate limit exceeded",
                message = $"Maximum {MaxRequestsPerMinute} requests per minute allowed. Please try again later.",
                retryAfter = 60 // seconds
            };

            await context.Response.WriteAsJsonAsync(errorResponse);
            return;
        }

        // Add current request
        requestTracker.Enqueue(now);
        _cache.Set(cacheKey, requestTracker, TimeSpan.FromMinutes(1));

        // Add rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = MaxRequestsPerMinute.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = (MaxRequestsPerMinute - requestTracker.Count).ToString();
        context.Response.Headers["X-RateLimit-Reset"] = now.AddMinutes(1).ToString("o");

        await _next(context);
    }

    private string GetClientIdentifier(HttpContext context)
    {
        // Try to get user ID from authentication if available
        var userId = context.User?.Identity?.Name;
        if (!string.IsNullOrEmpty(userId))
        {
            return $"user_{userId}";
        }

        // Fall back to IP address
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        // Check for X-Forwarded-For header (for proxies/load balancers)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var firstIp = forwardedFor.ToString().Split(',').FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(firstIp))
            {
                ipAddress = firstIp;
            }
        }

        return $"ip_{ipAddress}";
    }
}

/// <summary>
/// Extension methods for registering rate limiting middleware
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>();
    }
}
