using LogStream.Contracts.DTOs;

namespace LogStream.Web.Services;

public interface IRealTimeService
{
    Task NotifyNewLogEntryAsync(string tenantId, LogEntryDto logEntry);
    Task NotifyMetricsUpdateAsync(string tenantId, object metrics);
    Task NotifyAlertAsync(string tenantId, object alert);
    Task NotifySystemStatusAsync(string tenantId, object status);
}