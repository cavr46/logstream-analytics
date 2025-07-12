using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using LogStream.Domain.Interfaces;
using LogStream.Functions.Services;

namespace LogStream.Functions.Functions;

public class LogProcessingFunction
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogProcessingService _logProcessingService;
    private readonly IAlertService _alertService;
    private readonly ILogger<LogProcessingFunction> _logger;

    public LogProcessingFunction(
        IUnitOfWork unitOfWork,
        ILogProcessingService logProcessingService,
        IAlertService alertService,
        ILogger<LogProcessingFunction> logger)
    {
        _unitOfWork = unitOfWork;
        _logProcessingService = logProcessingService;
        _alertService = alertService;
        _logger = logger;
    }

    [Function("ProcessUnprocessedLogs")]
    public async Task ProcessUnprocessedLogs([TimerTrigger("0 */1 * * * *")] TimerInfo timer) // Every minute
    {
        _logger.LogInformation("Starting unprocessed logs processing at {Time}", DateTime.UtcNow);

        try
        {
            var unprocessedLogs = await _unitOfWork.LogEntries.GetUnprocessedLogsAsync(1000);
            
            if (!unprocessedLogs.Any())
            {
                _logger.LogDebug("No unprocessed logs found");
                return;
            }

            _logger.LogInformation("Processing {Count} unprocessed logs", unprocessedLogs.Count);

            await _logProcessingService.ProcessLogEntriesBatchAsync(unprocessedLogs);

            // Process alerts for each log entry
            foreach (var logEntry in unprocessedLogs)
            {
                await _alertService.ProcessAlertsAsync(logEntry);
            }

            _logger.LogInformation("Completed processing {Count} unprocessed logs", unprocessedLogs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing unprocessed logs");
            throw;
        }
    }

    [Function("ProcessLogFromServiceBus")]
    public async Task ProcessLogFromServiceBus(
        [ServiceBusTrigger("log-processing-queue", Connection = "ConnectionStrings:ServiceBus")] string message,
        FunctionContext context)
    {
        var logger = context.GetLogger("ProcessLogFromServiceBus");
        
        try
        {
            logger.LogInformation("Processing log message from Service Bus: {Message}", message);

            // Parse the message to get log entry ID
            if (!Guid.TryParse(message, out var logEntryId))
            {
                logger.LogError("Invalid log entry ID in message: {Message}", message);
                return;
            }

            var logEntry = await _unitOfWork.LogEntries.GetByIdAsync(logEntryId);
            if (logEntry == null)
            {
                logger.LogWarning("Log entry {LogEntryId} not found", logEntryId);
                return;
            }

            await _logProcessingService.ProcessLogEntryAsync(logEntry);
            await _alertService.ProcessAlertsAsync(logEntry);

            logger.LogInformation("Successfully processed log entry {LogEntryId}", logEntryId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing log from Service Bus: {Message}", message);
            throw;
        }
    }

    [Function("ProcessLogFromEventHub")]
    public async Task ProcessLogFromEventHub(
        [EventHubTrigger("log-events", Connection = "ConnectionStrings:EventHubs")] string[] events,
        FunctionContext context)
    {
        var logger = context.GetLogger("ProcessLogFromEventHub");
        
        try
        {
            logger.LogInformation("Processing {Count} events from Event Hub", events.Length);

            var logEntryIds = new List<Guid>();
            
            foreach (var eventData in events)
            {
                if (Guid.TryParse(eventData, out var logEntryId))
                {
                    logEntryIds.Add(logEntryId);
                }
                else
                {
                    logger.LogWarning("Invalid log entry ID in event: {EventData}", eventData);
                }
            }

            if (!logEntryIds.Any())
            {
                logger.LogWarning("No valid log entry IDs found in events");
                return;
            }

            var logEntries = new List<LogStream.Domain.Entities.LogEntry>();
            foreach (var logEntryId in logEntryIds)
            {
                var logEntry = await _unitOfWork.LogEntries.GetByIdAsync(logEntryId);
                if (logEntry != null)
                {
                    logEntries.Add(logEntry);
                }
            }

            if (logEntries.Any())
            {
                await _logProcessingService.ProcessLogEntriesBatchAsync(logEntries);
                
                foreach (var logEntry in logEntries)
                {
                    await _alertService.ProcessAlertsAsync(logEntry);
                }
            }

            logger.LogInformation("Successfully processed {Count} log entries from Event Hub", logEntries.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing logs from Event Hub");
            throw;
        }
    }
}