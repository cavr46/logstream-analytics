using LogStream.Domain.Entities;

namespace LogStream.Functions.Services;

public interface IAlertService
{
    Task ProcessAlertsAsync(LogEntry logEntry, CancellationToken cancellationToken = default);
    Task CheckThresholdAlertsAsync(string tenantId, CancellationToken cancellationToken = default);
    Task SendAlertNotificationAsync(AlertNotification notification, CancellationToken cancellationToken = default);
}

public class AlertNotification
{
    public string TenantId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}