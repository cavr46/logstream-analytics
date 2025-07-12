using LogStream.Infrastructure.Data;

namespace LogStream.Infrastructure.Repositories;

public class LogEntryRepository : Repository<LogEntry>, ILogEntryRepository
{
    public LogEntryRepository(LogStreamDbContext context) : base(context)
    {
    }

    public async Task<PagedResult<LogEntry>> GetLogsByTenantAsync(
        TenantId tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        LogLevel? level = null,
        string? keyword = null,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(l => l.TenantId == tenantId);

        if (startDate.HasValue)
        {
            query = query.Where(l => l.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(l => l.Timestamp <= endDate.Value);
        }

        if (level != null)
        {
            query = query.Where(l => l.Level == level);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(l =>
                l.Message.Content.Contains(keyword) ||
                (l.Exception != null && l.Exception.Contains(keyword)) ||
                (l.Tags != null && l.Tags.Contains(keyword)) ||
                l.RawContent.Contains(keyword));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<LogEntry>.Create(items, totalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyList<LogEntry>> GetRecentLogsByTenantAsync(
        TenantId tenantId,
        int count = 100,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetLogCountByTenantAndDateAsync(
        TenantId tenantId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await DbSet
            .Where(l => l.TenantId == tenantId && 
                       l.Timestamp >= startOfDay && 
                       l.Timestamp < endOfDay)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LogEntry>> GetUnprocessedLogsAsync(
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(l => !l.IsProcessed)
            .OrderBy(l => l.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LogEntry>> GetLogsForArchivalAsync(
        DateTime beforeDate,
        int batchSize = 1000,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(l => !l.IsArchived && l.CreatedAt < beforeDate)
            .OrderBy(l => l.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task BulkInsertAsync(IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken = default)
    {
        await DbSet.AddRangeAsync(logEntries, cancellationToken);
    }

    public async Task<long> GetTotalSizeBytesAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(l => l.TenantId == tenantId)
            .SumAsync(l => (long)l.SizeBytes, cancellationToken);
    }
}