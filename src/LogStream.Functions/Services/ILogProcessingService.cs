using LogStream.Domain.Entities;

namespace LogStream.Functions.Services;

public interface ILogProcessingService
{
    Task ProcessLogEntryAsync(LogEntry logEntry, CancellationToken cancellationToken = default);
    Task ProcessLogEntriesBatchAsync(IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken = default);
    Task EnrichLogEntryAsync(LogEntry logEntry, CancellationToken cancellationToken = default);
    Task DetectAnomaliesAsync(LogEntry logEntry, CancellationToken cancellationToken = default);
    Task IndexLogEntryAsync(LogEntry logEntry, CancellationToken cancellationToken = default);
}