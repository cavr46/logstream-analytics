using LogStream.Domain.Entities;

namespace LogStream.Functions.Services;

public interface IArchivalService
{
    Task ArchiveOldLogsAsync(string tenantId, CancellationToken cancellationToken = default);
    Task ArchiveLogsBatchAsync(IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken = default);
    Task DeleteArchivedLogsAsync(string tenantId, DateTime olderThan, CancellationToken cancellationToken = default);
    Task CompressAndStoreAsync(IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken = default);
}