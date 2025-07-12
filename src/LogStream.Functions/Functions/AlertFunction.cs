using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using LogStream.Domain.Interfaces;
using LogStream.Functions.Services;

namespace LogStream.Functions.Functions;

public class AlertFunction
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertFunction> _logger;

    public AlertFunction(
        IUnitOfWork unitOfWork,
        IAlertService alertService,
        ILogger<AlertFunction> logger)
    {
        _unitOfWork = unitOfWork;
        _alertService = alertService;
        _logger = logger;
    }

    [Function("CheckThresholdAlerts")]
    public async Task CheckThresholdAlerts([TimerTrigger("0 */5 * * * *")] TimerInfo timer) // Every 5 minutes
    {
        _logger.LogInformation("Starting threshold alerts check at {Time}", DateTime.UtcNow);

        try
        {
            var activeTenants = await _unitOfWork.Tenants.GetActiveTenants();
            
            _logger.LogInformation("Checking threshold alerts for {Count} active tenants", activeTenants.Count);

            var tasks = activeTenants.Select(async tenant =>
            {
                try
                {
                    await _alertService.CheckThresholdAlertsAsync(tenant.TenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking threshold alerts for tenant {TenantId}", tenant.TenantId);
                }
            });

            await Task.WhenAll(tasks);

            _logger.LogInformation("Completed threshold alerts check for {Count} tenants", activeTenants.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during threshold alerts check");
            throw;
        }
    }

    [Function("ProcessCriticalAlert")]
    public async Task ProcessCriticalAlert(
        [ServiceBusTrigger("critical-alerts-queue", Connection = "ConnectionStrings:ServiceBus")] string message,
        FunctionContext context)
    {
        var logger = context.GetLogger("ProcessCriticalAlert");
        
        try
        {
            logger.LogInformation("Processing critical alert: {Message}", message);

            // Parse the message to get log entry ID
            if (!Guid.TryParse(message, out var logEntryId))
            {
                logger.LogError("Invalid log entry ID in critical alert: {Message}", message);
                return;
            }

            var logEntry = await _unitOfWork.LogEntries.GetByIdAsync(logEntryId);
            if (logEntry == null)
            {
                logger.LogWarning("Log entry {LogEntryId} not found for critical alert", logEntryId);
                return;
            }

            var alertNotification = new AlertNotification
            {
                TenantId = logEntry.TenantId,
                Title = "URGENT: Critical System Alert",
                Message = $"Critical issue detected in {logEntry.Source.Application}: {logEntry.Message.Content}",
                Severity = "CRITICAL",
                Metadata = new Dictionary<string, object>
                {
                    { "logEntryId", logEntry.Id },
                    { "application", logEntry.Source.Application },
                    { "environment", logEntry.Source.Environment },
                    { "level", logEntry.Level.Value },
                    { "timestamp", logEntry.Timestamp },
                    { "urgent", true }
                }
            };

            await _alertService.SendAlertNotificationAsync(alertNotification);

            logger.LogInformation("Successfully processed critical alert for log entry {LogEntryId}", logEntryId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing critical alert: {Message}", message);
            throw;
        }
    }

    [Function("ProcessAlertEscalation")]
    public async Task ProcessAlertEscalation([TimerTrigger("0 */15 * * * *")] TimerInfo timer) // Every 15 minutes
    {
        _logger.LogInformation("Starting alert escalation check at {Time}", DateTime.UtcNow);

        try
        {
            // In a real implementation, you would:
            // 1. Check for unacknowledged critical alerts
            // 2. Escalate to higher-level contacts
            // 3. Send notifications via multiple channels
            // 4. Create incident tickets

            var fifteenMinutesAgo = DateTime.UtcNow.AddMinutes(-15);
            var activeTenants = await _unitOfWork.Tenants.GetActiveTenants();

            foreach (var tenant in activeTenants)
            {
                var criticalLogs = await _unitOfWork.LogEntries.FindAsync(
                    l => l.TenantId == tenant.TenantId &&
                         l.Level.Value == "FATAL" &&
                         l.Timestamp >= fifteenMinutesAgo);

                if (criticalLogs.Any())
                {
                    var escalationNotification = new AlertNotification
                    {
                        TenantId = tenant.TenantId,
                        Title = $"ESCALATION: {criticalLogs.Count()} Unresolved Critical Issues",
                        Message = $"There are {criticalLogs.Count()} unresolved critical issues that require immediate attention.",
                        Severity = "CRITICAL",
                        Metadata = new Dictionary<string, object>
                        {
                            { "criticalCount", criticalLogs.Count() },
                            { "escalated", true },
                            { "timeWindow", "15_minutes" }
                        }
                    };

                    await _alertService.SendAlertNotificationAsync(escalationNotification);
                    
                    _logger.LogWarning("Escalated {Count} critical issues for tenant {TenantId}", 
                        criticalLogs.Count(), tenant.TenantId);
                }
            }

            _logger.LogInformation("Completed alert escalation check");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during alert escalation check");
            throw;
        }
    }
}