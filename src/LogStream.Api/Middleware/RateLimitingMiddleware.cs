using System.Collections.Concurrent;

namespace LogStream.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, RateLimitCounter> _counters = new();
    private readonly int _requestsPerMinute = 1000; // Configurable in production

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantId = context.Items["TenantId"]?.ToString();
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        
        var key = !string.IsNullOrEmpty(tenantId) ? $"tenant:{tenantId}" : $"ip:{clientIp}";
        
        var counter = _counters.GetOrAdd(key, _ => new RateLimitCounter());
        
        if (!counter.TryConsumeRequest())
        {
            _logger.LogWarning("Rate limit exceeded for {Key}", key);
            context.Response.StatusCode = 429;
            context.Response.Headers.Add("Retry-After", "60");
            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        await _next(context);
    }

    private class RateLimitCounter
    {
        private readonly object _lock = new();
        private DateTime _windowStart = DateTime.UtcNow;
        private int _requestCount = 0;
        private readonly int _maxRequests = 1000; // Per minute

        public bool TryConsumeRequest()
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                
                // Reset window if a minute has passed
                if (now - _windowStart >= TimeSpan.FromMinutes(1))
                {
                    _windowStart = now;
                    _requestCount = 0;
                }

                if (_requestCount >= _maxRequests)
                {
                    return false;
                }

                _requestCount++;
                return true;
            }
        }
    }
}