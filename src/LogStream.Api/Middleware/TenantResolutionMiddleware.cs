namespace LogStream.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract tenant ID from route
        var tenantId = context.Request.RouteValues["tenantId"]?.ToString();
        
        if (!string.IsNullOrEmpty(tenantId))
        {
            context.Items["TenantId"] = tenantId;
            _logger.LogDebug("Resolved tenant ID: {TenantId}", tenantId);
        }

        // Extract API key from headers
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault() ??
                    context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            context.Items["ApiKey"] = apiKey;
            _logger.LogDebug("Found API key in request");
        }

        await _next(context);
    }
}