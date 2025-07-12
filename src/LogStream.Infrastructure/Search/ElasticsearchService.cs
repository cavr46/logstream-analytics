using Elasticsearch.Net;
using Nest;
using System.Diagnostics;

namespace LogStream.Infrastructure.Search;

public class ElasticsearchService : ISearchService
{
    private readonly IElasticClient _client;
    private readonly ILogger<ElasticsearchService> _logger;
    private const string IndexPrefix = "logstream-logs";

    public ElasticsearchService(IElasticClient client, ILogger<ElasticsearchService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task IndexLogEntryAsync(LogEntry logEntry, CancellationToken cancellationToken = default)
    {
        try
        {
            var indexName = GetIndexName(logEntry.TenantId);
            var document = MapToElasticsearchDocument(logEntry);

            var response = await _client.IndexAsync(document, idx => idx
                .Index(indexName)
                .Id(logEntry.Id.ToString()), cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to index log entry {LogEntryId}: {Error}", 
                    logEntry.Id, response.ServerError?.Error?.Reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing log entry {LogEntryId}", logEntry.Id);
        }
    }

    public async Task IndexLogEntriesAsync(IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken = default)
    {
        try
        {
            var bulkDescriptor = new BulkDescriptor();

            foreach (var logEntry in logEntries)
            {
                var indexName = GetIndexName(logEntry.TenantId);
                var document = MapToElasticsearchDocument(logEntry);

                bulkDescriptor.Index<ElasticsearchLogDocument>(op => op
                    .Index(indexName)
                    .Id(logEntry.Id.ToString())
                    .Document(document));
            }

            var response = await _client.BulkAsync(bulkDescriptor, cancellationToken);

            if (response.Errors)
            {
                foreach (var item in response.ItemsWithErrors)
                {
                    _logger.LogError("Failed to index log entry {LogEntryId}: {Error}", 
                        item.Id, item.Error?.Reason);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk indexing log entries");
        }
    }

    public async Task DeleteLogEntryAsync(Guid logEntryId, TenantId tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var indexName = GetIndexName(tenantId);
            var response = await _client.DeleteAsync<ElasticsearchLogDocument>(logEntryId.ToString(), 
                d => d.Index(indexName), cancellationToken);

            if (!response.IsValid && response.Result != Result.NotFound)
            {
                _logger.LogError("Failed to delete log entry {LogEntryId}: {Error}", 
                    logEntryId, response.ServerError?.Error?.Reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting log entry {LogEntryId}", logEntryId);
        }
    }

    public async Task<SearchResult<LogEntry>> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use SearchByTenantAsync for tenant-specific searches");
    }

    public async Task<SearchResult<LogEntry>> SearchByTenantAsync(TenantId tenantId, SearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var indexName = GetIndexName(tenantId);

            var searchDescriptor = new SearchDescriptor<ElasticsearchLogDocument>()
                .Index(indexName)
                .From((request.Page - 1) * request.Size)
                .Size(request.Size);

            // Build query
            var queries = new List<QueryContainer>();

            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                queries.Add(Query<ElasticsearchLogDocument>.MultiMatch(m => m
                    .Query(request.Query)
                    .Fields(f => f
                        .Field(doc => doc.Message)
                        .Field(doc => doc.Exception)
                        .Field(doc => doc.Tags)
                        .Field(doc => doc.RawContent))));
            }

            if (request.StartDate.HasValue)
            {
                queries.Add(Query<ElasticsearchLogDocument>.DateRange(r => r
                    .Field(doc => doc.Timestamp)
                    .GreaterThanOrEquals(request.StartDate.Value)));
            }

            if (request.EndDate.HasValue)
            {
                queries.Add(Query<ElasticsearchLogDocument>.DateRange(r => r
                    .Field(doc => doc.Timestamp)
                    .LessThanOrEquals(request.EndDate.Value)));
            }

            if (!string.IsNullOrWhiteSpace(request.Level))
            {
                queries.Add(Query<ElasticsearchLogDocument>.Term(t => t
                    .Field(doc => doc.Level)
                    .Value(request.Level)));
            }

            if (!string.IsNullOrWhiteSpace(request.Application))
            {
                queries.Add(Query<ElasticsearchLogDocument>.Term(t => t
                    .Field(doc => doc.Application)
                    .Value(request.Application)));
            }

            if (!string.IsNullOrWhiteSpace(request.Environment))
            {
                queries.Add(Query<ElasticsearchLogDocument>.Term(t => t
                    .Field(doc => doc.Environment)
                    .Value(request.Environment)));
            }

            if (request.Tags?.Length > 0)
            {
                foreach (var tag in request.Tags)
                {
                    queries.Add(Query<ElasticsearchLogDocument>.Wildcard(w => w
                        .Field(doc => doc.Tags)
                        .Value($"*{tag}*")));
                }
            }

            if (queries.Any())
            {
                searchDescriptor = searchDescriptor.Query(q => q.Bool(b => b.Must(queries.ToArray())));
            }

            // Apply sorting
            if (request.SortBy.Equals("timestamp", StringComparison.OrdinalIgnoreCase))
            {
                searchDescriptor = request.SortDescending
                    ? searchDescriptor.Sort(s => s.Descending(doc => doc.Timestamp))
                    : searchDescriptor.Sort(s => s.Ascending(doc => doc.Timestamp));
            }

            // Add aggregations
            searchDescriptor = searchDescriptor.Aggregations(a => a
                .Terms("levels", t => t.Field(doc => doc.Level))
                .Terms("applications", t => t.Field(doc => doc.Application))
                .Terms("environments", t => t.Field(doc => doc.Environment)));

            var response = await _client.SearchAsync<ElasticsearchLogDocument>(searchDescriptor, cancellationToken);
            stopwatch.Stop();

            if (!response.IsValid)
            {
                _logger.LogError("Search failed: {Error}", response.ServerError?.Error?.Reason);
                return new SearchResult<LogEntry>
                {
                    Results = Array.Empty<LogEntry>(),
                    Total = 0,
                    Page = request.Page,
                    Size = request.Size,
                    Elapsed = stopwatch.Elapsed
                };
            }

            var results = response.Documents
                .Select(MapFromElasticsearchDocument)
                .ToList();

            var aggregations = new Dictionary<string, long>();
            if (response.Aggregations.ContainsKey("levels"))
            {
                var levelAgg = response.Aggregations.Terms("levels");
                foreach (var bucket in levelAgg.Buckets)
                {
                    aggregations[$"level:{bucket.Key}"] = bucket.DocCount ?? 0;
                }
            }

            return new SearchResult<LogEntry>
            {
                Results = results,
                Total = response.Total,
                Page = request.Page,
                Size = request.Size,
                Elapsed = stopwatch.Elapsed,
                Aggregations = aggregations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching logs for tenant {TenantId}", tenantId);
            return new SearchResult<LogEntry>
            {
                Results = Array.Empty<LogEntry>(),
                Total = 0,
                Page = request.Page,
                Size = request.Size,
                Elapsed = TimeSpan.Zero
            };
        }
    }

    public async Task CreateIndexAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var indexName = GetIndexName(tenantId);
            var response = await _client.Indices.CreateAsync(indexName, c => c
                .Map<ElasticsearchLogDocument>(m => m
                    .Properties(p => p
                        .Keyword(k => k.Name(n => n.TenantId))
                        .Date(d => d.Name(n => n.Timestamp))
                        .Keyword(k => k.Name(n => n.Level))
                        .Text(t => t.Name(n => n.Message).Analyzer("standard"))
                        .Keyword(k => k.Name(n => n.Application))
                        .Keyword(k => k.Name(n => n.Environment))
                        .Keyword(k => k.Name(n => n.Server))
                        .Keyword(k => k.Name(n => n.Component))
                        .Keyword(k => k.Name(n => n.TraceId))
                        .Keyword(k => k.Name(n => n.CorrelationId))
                        .Text(t => t.Name(n => n.Exception).Analyzer("standard"))
                        .Text(t => t.Name(n => n.Tags).Analyzer("keyword"))
                        .Text(t => t.Name(n => n.RawContent).Analyzer("standard"))
                        .Ip(i => i.Name(n => n.IpAddress))
                        .Boolean(b => b.Name(n => n.IsProcessed))
                        .Boolean(b => b.Name(n => n.IsArchived))))
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(1)
                    .RefreshInterval("5s")), cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to create index {IndexName}: {Error}", 
                    indexName, response.ServerError?.Error?.Reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating index for tenant {TenantId}", tenantId);
        }
    }

    public async Task DeleteIndexAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var indexName = GetIndexName(tenantId);
            var response = await _client.Indices.DeleteAsync(indexName, cancellationToken);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to delete index {IndexName}: {Error}", 
                    indexName, response.ServerError?.Error?.Reason);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting index for tenant {TenantId}", tenantId);
        }
    }

    public async Task<bool> IndexExistsAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var indexName = GetIndexName(tenantId);
            var response = await _client.Indices.ExistsAsync(indexName, cancellationToken);
            return response.Exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if index exists for tenant {TenantId}", tenantId);
            return false;
        }
    }

    private static string GetIndexName(TenantId tenantId)
    {
        return $"{IndexPrefix}-{tenantId.Value}";
    }

    private static ElasticsearchLogDocument MapToElasticsearchDocument(LogEntry logEntry)
    {
        return new ElasticsearchLogDocument
        {
            Id = logEntry.Id,
            TenantId = logEntry.TenantId,
            Timestamp = logEntry.Timestamp,
            Level = logEntry.Level,
            Message = logEntry.Message.Content,
            MessageTemplate = logEntry.Message.Template,
            Application = logEntry.Source.Application,
            Environment = logEntry.Source.Environment,
            Server = logEntry.Source.Server,
            Component = logEntry.Source.Component,
            TraceId = logEntry.TraceId,
            SpanId = logEntry.SpanId,
            UserId = logEntry.UserId,
            SessionId = logEntry.SessionId,
            CorrelationId = logEntry.CorrelationId,
            Exception = logEntry.Exception,
            Tags = logEntry.Tags,
            OriginalFormat = logEntry.OriginalFormat,
            RawContent = logEntry.RawContent,
            SizeBytes = logEntry.SizeBytes,
            IpAddress = logEntry.IpAddress,
            UserAgent = logEntry.UserAgent,
            IsProcessed = logEntry.IsProcessed,
            IsArchived = logEntry.IsArchived,
            CreatedAt = logEntry.CreatedAt
        };
    }

    private static LogEntry MapFromElasticsearchDocument(ElasticsearchLogDocument doc)
    {
        // This is a simplified mapping - in production, you'd want a more robust mapping
        var message = new LogMessage(doc.Message, doc.MessageTemplate);
        var source = new LogSource(doc.Application, doc.Environment, doc.Server, doc.Component);
        var level = new LogLevel(doc.Level);
        
        return new LogEntry(
            doc.TenantId,
            doc.Timestamp,
            level,
            message,
            source,
            doc.OriginalFormat,
            doc.RawContent,
            doc.TraceId,
            doc.SpanId,
            doc.UserId,
            doc.SessionId,
            doc.CorrelationId,
            doc.Exception,
            null, // metadata would need special handling
            doc.Tags,
            doc.IpAddress,
            doc.UserAgent);
    }
}

public class ElasticsearchLogDocument
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? MessageTemplate { get; set; }
    public string Application { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string? Server { get; set; }
    public string? Component { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public string? CorrelationId { get; set; }
    public string? Exception { get; set; }
    public string? Tags { get; set; }
    public string OriginalFormat { get; set; } = string.Empty;
    public string RawContent { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsProcessed { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
}