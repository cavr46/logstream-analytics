using LogStream.Domain.Entities;
using LogStream.Domain.ValueObjects;

namespace LogStream.Domain.Interfaces;

public interface ILogEntryRepository : IRepository<LogEntry>
{
    Task<PagedResult<LogEntry>> GetLogsByTenantAsync(
        TenantId tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        LogLevel? level = null,
        string? keyword = null,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LogEntry>> GetRecentLogsByTenantAsync(
        TenantId tenantId,
        int count = 100,
        CancellationToken cancellationToken = default);

    Task<long> GetLogCountByTenantAndDateAsync(
        TenantId tenantId,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LogEntry>> GetUnprocessedLogsAsync(
        int batchSize = 1000,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LogEntry>> GetLogsForArchivalAsync(
        DateTime beforeDate,
        int batchSize = 1000,
        CancellationToken cancellationToken = default);

    Task BulkInsertAsync(IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken = default);
    
    Task<long> GetTotalSizeBytesAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}