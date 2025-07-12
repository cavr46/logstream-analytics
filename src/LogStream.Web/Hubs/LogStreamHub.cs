using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace LogStream.Web.Hubs;

[Authorize]
public class LogStreamHub : Hub
{
    private readonly ILogger<LogStreamHub> _logger;

    public LogStreamHub(ILogger<LogStreamHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinTenantGroup(string tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
        _logger.LogDebug("User {UserId} joined tenant group {TenantId}", Context.UserIdentifier, tenantId);
    }

    public async Task LeaveTenantGroup(string tenantId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
        _logger.LogDebug("User {UserId} left tenant group {TenantId}", Context.UserIdentifier, tenantId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("User {UserId} disconnected", Context.UserIdentifier);
        await base.OnDisconnectedAsync(exception);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogDebug("User {UserId} connected", Context.UserIdentifier);
        await base.OnConnectedAsync();
    }
}