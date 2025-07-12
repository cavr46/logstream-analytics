using LogStream.Domain.Entities;
using LogStream.Domain.Interfaces;
using LogStream.Infrastructure.Search;
using Microsoft.Extensions.Logging;

namespace LogStream.Functions.Services;

public class LogProcessingService : ILogProcessingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISearchService _searchService;
    private readonly ILogger<LogProcessingService> _logger;

    public LogProcessingService(
        IUnitOfWork unitOfWork,
        ISearchService searchService,
        ILogger<LogProcessingService> logger)
    {
        _unitOfWork = unitOfWork;
        _searchService = searchService;
        _logger = logger;
    }

    public async Task ProcessLogEntryAsync(LogEntry logEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            // Enrich the log entry
            await EnrichLogEntryAsync(logEntry, cancellationToken);

            // Detect anomalies
            await DetectAnomaliesAsync(logEntry, cancellationToken);

            // Index in Elasticsearch for search
            await IndexLogEntryAsync(logEntry, cancellationToken);

            // Mark as processed
            logEntry.MarkAsProcessed("log-processor");

            // Save changes
            _unitOfWork.LogEntries.Update(logEntry);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Processed log entry {LogEntryId} for tenant {TenantId}", 
                logEntry.Id, logEntry.TenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing log entry {LogEntryId}", logEntry.Id);
            throw;
        }
    }

    public async Task ProcessLogEntriesBatchAsync(IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken = default)
    {
        var logEntryList = logEntries.ToList();
        
        try
        {
            var tasks = logEntryList.Select(async logEntry =>
            {
                try
                {
                    await EnrichLogEntryAsync(logEntry, cancellationToken);
                    await DetectAnomaliesAsync(logEntry, cancellationToken);
                    logEntry.MarkAsProcessed("batch-processor");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing log entry {LogEntryId} in batch", logEntry.Id);
                }
            });

            await Task.WhenAll(tasks);

            // Bulk index in Elasticsearch
            await _searchService.IndexLogEntriesAsync(logEntryList, cancellationToken);

            // Bulk update in database
            foreach (var logEntry in logEntryList)
            {
                _unitOfWork.LogEntries.Update(logEntry);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Processed batch of {Count} log entries", logEntryList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing log entries batch");
            throw;
        }
    }

    public async Task EnrichLogEntryAsync(LogEntry logEntry, CancellationToken cancellationToken = default)
    {
        // GeoIP enrichment (placeholder)
        if (!string.IsNullOrEmpty(logEntry.IpAddress))
        {
            // In a real implementation, you would use a GeoIP service
            // var geoInfo = await _geoIpService.LookupAsync(logEntry.IpAddress);
            // logEntry.UpdateMetadata("country", geoInfo.Country);
            // logEntry.UpdateMetadata("city", geoInfo.City);
        }

        // User-Agent parsing (placeholder)
        if (!string.IsNullOrEmpty(logEntry.UserAgent))
        {
            // In a real implementation, you would parse the User-Agent string
            // var userAgentInfo = UserAgentParser.Parse(logEntry.UserAgent);
            // logEntry.UpdateMetadata("browser", userAgentInfo.Browser);
            // logEntry.UpdateMetadata("os", userAgentInfo.OperatingSystem);
        }

        await Task.CompletedTask;
    }

    public async Task DetectAnomaliesAsync(LogEntry logEntry, CancellationToken cancellationToken = default)
    {
        // Simple anomaly detection based on error patterns
        if (logEntry.Level.Value == "ERROR" || logEntry.Level.Value == "FATAL")
        {
            // Check for specific error patterns
            var errorPatterns = new[]
            {
                "OutOfMemoryException",
                "StackOverflowException",
                "TimeoutException",
                "ConnectionException",
                "SecurityException"
            };

            var hasKnownErrorPattern = errorPatterns.Any(pattern =>
                logEntry.Message.Content.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
                (logEntry.Exception?.Contains(pattern, StringComparison.OrdinalIgnoreCase) == true));

            if (hasKnownErrorPattern)
            {
                // In a real implementation, you would trigger alerts here
                _logger.LogWarning("Anomaly detected in log entry {LogEntryId}: Known error pattern found", 
                    logEntry.Id);
            }
        }

        // Check for unusual log volumes (placeholder)
        var recentLogCount = await _unitOfWork.LogEntries.CountAsync(
            l => l.TenantId == logEntry.TenantId && 
                 l.Timestamp >= DateTime.UtcNow.AddMinutes(-5),
            cancellationToken);

        if (recentLogCount > 1000) // Threshold for anomaly
        {
            _logger.LogWarning("High log volume detected for tenant {TenantId}: {Count} logs in last 5 minutes", 
                logEntry.TenantId, recentLogCount);
        }

        await Task.CompletedTask;
    }

    public async Task IndexLogEntryAsync(LogEntry logEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            await _searchService.IndexLogEntryAsync(logEntry, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing log entry {LogEntryId} in Elasticsearch", logEntry.Id);
            // Don't rethrow - indexing failure shouldn't break the processing pipeline
        }
    }
}