namespace LogStream.Infrastructure.Search;

public interface ISearchService
{
    Task IndexLogEntryAsync(LogEntry logEntry, CancellationToken cancellationToken = default);
    Task IndexLogEntriesAsync(IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken = default);
    Task DeleteLogEntryAsync(Guid logEntryId, TenantId tenantId, CancellationToken cancellationToken = default);
    Task<SearchResult<LogEntry>> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
    Task<SearchResult<LogEntry>> SearchByTenantAsync(TenantId tenantId, SearchRequest request, CancellationToken cancellationToken = default);
    Task CreateIndexAsync(TenantId tenantId, CancellationToken cancellationToken = default);
    Task DeleteIndexAsync(TenantId tenantId, CancellationToken cancellationToken = default);
    Task<bool> IndexExistsAsync(TenantId tenantId, CancellationToken cancellationToken = default);
}

public record SearchRequest
{
    public string? Query { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Level { get; init; }
    public string? Application { get; init; }
    public string? Environment { get; init; }
    public string? Server { get; init; }
    public string? Component { get; init; }
    public string[]? Tags { get; init; }
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 100;
    public string SortBy { get; init; } = "timestamp";
    public bool SortDescending { get; init; } = true;
}

public record SearchResult<T>
{
    public IReadOnlyList<T> Results { get; init; } = Array.Empty<T>();
    public long Total { get; init; }
    public int Page { get; init; }
    public int Size { get; init; }
    public TimeSpan Elapsed { get; init; }
    public IReadOnlyDictionary<string, long> Aggregations { get; init; } = new Dictionary<string, long>();
}