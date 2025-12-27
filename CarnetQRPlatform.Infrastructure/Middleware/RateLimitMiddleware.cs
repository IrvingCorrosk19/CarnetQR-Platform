using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net;

namespace CarnetQRPlatform.Infrastructure.Middleware;

public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    
    // Rate limit configuration
    private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimitStore = new();
    private static readonly TimeSpan _window = TimeSpan.FromMinutes(1);
    private const int _maxRequestsPerWindow = 30; // 30 requests per minute per IP
    private const int _maxRequestsPerWindowQR = 10; // 10 requests per minute for QR endpoint

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var isQrEndpoint = path.StartsWith("/q/");

        // Skip rate limiting for authenticated users on non-QR endpoints
        if (context.User?.Identity?.IsAuthenticated == true && !isQrEndpoint)
        {
            await _next(context);
            return;
        }

        var clientIp = GetClientIpAddress(context);
        var key = $"{clientIp}:{path}";
        var maxRequests = isQrEndpoint ? _maxRequestsPerWindowQR : _maxRequestsPerWindow;

        var rateLimitInfo = _rateLimitStore.AddOrUpdate(
            key,
            new RateLimitInfo { Count = 1, ResetTime = DateTime.UtcNow.Add(_window) },
            (k, existing) =>
            {
                if (DateTime.UtcNow > existing.ResetTime)
                {
                    return new RateLimitInfo { Count = 1, ResetTime = DateTime.UtcNow.Add(_window) };
                }
                return new RateLimitInfo { Count = existing.Count + 1, ResetTime = existing.ResetTime };
            }
        );

        // Clean up old entries periodically
        if (DateTime.UtcNow.Second % 30 == 0)
        {
            CleanupOldEntries();
        }

        if (rateLimitInfo.Count > maxRequests)
        {
            _logger.LogWarning("Rate limit exceeded for IP: {IpAddress}, Path: {Path}, Count: {Count}", 
                clientIp, path, rateLimitInfo.Count);
            
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = ((int)(rateLimitInfo.ResetTime - DateTime.UtcNow).TotalSeconds).ToString();
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        context.Response.Headers["X-RateLimit-Limit"] = maxRequests.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, maxRequests - rateLimitInfo.Count).ToString();
        context.Response.Headers["X-RateLimit-Reset"] = ((DateTimeOffset)rateLimitInfo.ResetTime).ToUnixTimeSeconds().ToString();

        await _next(context);
    }

    private string GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded IP (behind proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',');
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private void CleanupOldEntries()
    {
        var keysToRemove = _rateLimitStore
            .Where(kvp => DateTime.UtcNow > kvp.Value.ResetTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _rateLimitStore.TryRemove(key, out _);
        }
    }

    private class RateLimitInfo
    {
        public int Count { get; set; }
        public DateTime ResetTime { get; set; }
    }
}

