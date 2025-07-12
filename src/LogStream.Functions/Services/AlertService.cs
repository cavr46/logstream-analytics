using LogStream.Domain.Entities;
using LogStream.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LogStream.Functions.Services;

public class AlertService : IAlertService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AlertService> _logger;

    public AlertService(IUnitOfWork unitOfWork, ILogger<AlertService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessAlertsAsync(LogEntry logEntry, CancellationToken cancellationToken = default)
    {
        // Check for critical errors
        if (logEntry.Level.Value == "FATAL" || logEntry.Level.Value == "ERROR")
        {
            var alertNotification = new AlertNotification
            {
                TenantId = logEntry.TenantId,
                Title = $"Critical Error Detected in {logEntry.Source.Application}",
                Message = $"Critical error occurred: {logEntry.Message.Content}",
                Severity = logEntry.Level.Value,
                Metadata = new Dictionary<string, object>
                {
                    { "logEntryId", logEntry.Id },
                    { "application", logEntry.Source.Application },
                    { "environment", logEntry.Source.Environment },
                    { "server", logEntry.Source.Server ?? "unknown" },
                    { "timestamp", logEntry.Timestamp }
                }
            };

            await SendAlertNotificationAsync(alertNotification, cancellationToken);
        }

        // Check for specific error patterns
        var criticalPatterns = new[]
        {
            "database connection failed",
            "out of memory",
            "service unavailable",
            "authentication failed",
            "security breach"
        };

        var hasCriticalPattern = criticalPatterns.Any(pattern =>
            logEntry.Message.Content.Contains(pattern, StringComparison.OrdinalIgnoreCase));

        if (hasCriticalPattern)
        {
            var alertNotification = new AlertNotification
            {
                TenantId = logEntry.TenantId,
                Title = "Critical System Issue Detected",
                Message = $"Critical pattern detected in logs: {logEntry.Message.Content}",
                Severity = "CRITICAL",
                Metadata = new Dictionary<string, object>
                {
                    { "logEntryId", logEntry.Id },
                    { "application", logEntry.Source.Application },
                    { "pattern", "critical_system_issue" }
                }
            };

            await SendAlertNotificationAsync(alertNotification, cancellationToken);
        }
    }

    public async Task CheckThresholdAlertsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByTenantIdAsync(tenantId, cancellationToken);
        if (tenant == null) return;

        var now = DateTime.UtcNow;
        var fiveMinutesAgo = now.AddMinutes(-5);

        // Check error rate threshold
        var totalLogs = await _unitOfWork.LogEntries.CountAsync(
            l => l.TenantId == tenantId && l.Timestamp >= fiveMinutesAgo,
            cancellationToken);

        var errorLogs = await _unitOfWork.LogEntries.CountAsync(
            l => l.TenantId == tenantId && 
                 l.Timestamp >= fiveMinutesAgo &&
                 (l.Level.Value == "ERROR" || l.Level.Value == "FATAL"),
            cancellationToken);

        if (totalLogs > 0)
        {
            var errorRate = (double)errorLogs / totalLogs;
            if (errorRate > 0.1) // 10% error rate threshold
            {
                var alertNotification = new AlertNotification
                {
                    TenantId = tenantId,
                    Title = "High Error Rate Alert",
                    Message = $"Error rate of {errorRate:P1} detected in the last 5 minutes ({errorLogs}/{totalLogs})",
                    Severity = "WARNING",
                    Metadata = new Dictionary<string, object>
                    {
                        { "errorRate", errorRate },
                        { "errorCount", errorLogs },
                        { "totalCount", totalLogs },
                        { "timeWindow", "5_minutes" }
                    }
                };

                await SendAlertNotificationAsync(alertNotification, cancellationToken);
            }
        }

        // Check volume threshold
        if (totalLogs > 10000) // High volume threshold
        {
            var alertNotification = new AlertNotification
            {
                TenantId = tenantId,
                Title = "High Log Volume Alert",
                Message = $"High log volume detected: {totalLogs} logs in the last 5 minutes",
                Severity = "INFO",
                Metadata = new Dictionary<string, object>
                {
                    { "logCount", totalLogs },
                    { "timeWindow", "5_minutes" }
                }
            };

            await SendAlertNotificationAsync(alertNotification, cancellationToken);
        }
    }

    public async Task SendAlertNotificationAsync(AlertNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation, you would send notifications via:
            // - Email (SendGrid, AWS SES)
            // - SMS (Twilio, AWS SNS)
            // - Slack/Teams webhooks
            // - PagerDuty
            // - Custom webhooks

            _logger.LogWarning("ALERT: {Title} for tenant {TenantId} - {Message}",
                notification.Title, notification.TenantId, notification.Message);

            // Simulate sending notification
            await Task.Delay(100, cancellationToken);

            // Log to Application Insights or other monitoring system
            _logger.LogInformation("Alert notification sent: {Title} for tenant {TenantId}",
                notification.Title, notification.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send alert notification for tenant {TenantId}", notification.TenantId);
        }
    }
}