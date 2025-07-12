using LogStream.Contracts.DTOs;
using LogStream.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace LogStream.Web.Services;

public class RealTimeService : IRealTimeService
{
    private readonly IHubContext<LogStreamHub> _hubContext;
    private readonly ILogger<RealTimeService> _logger;

    public RealTimeService(IHubContext<LogStreamHub> hubContext, ILogger<RealTimeService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyNewLogEntryAsync(string tenantId, LogEntryDto logEntry)
    {
        try
        {
            await _hubContext.Clients.Group($"tenant:{tenantId}")
                .SendAsync("NewLogEntry", logEntry);
            
            _logger.LogDebug("Notified new log entry {LogEntryId} to tenant {TenantId}", 
                logEntry.Id, tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying new log entry to tenant {TenantId}", tenantId);
        }
    }

    public async Task NotifyMetricsUpdateAsync(string tenantId, object metrics)
    {
        try
        {
            await _hubContext.Clients.Group($"tenant:{tenantId}")
                .SendAsync("MetricsUpdate", metrics);
            
            _logger.LogDebug("Notified metrics update to tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying metrics update to tenant {TenantId}", tenantId);
        }
    }

    public async Task NotifyAlertAsync(string tenantId, object alert)
    {
        try
        {
            await _hubContext.Clients.Group($"tenant:{tenantId}")
                .SendAsync("NewAlert", alert);
            
            _logger.LogDebug("Notified alert to tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying alert to tenant {TenantId}", tenantId);
        }
    }

    public async Task NotifySystemStatusAsync(string tenantId, object status)
    {
        try
        {
            await _hubContext.Clients.Group($"tenant:{tenantId}")
                .SendAsync("SystemStatus", status);
            
            _logger.LogDebug("Notified system status to tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying system status to tenant {TenantId}", tenantId);
        }
    }
}