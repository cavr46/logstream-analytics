using LogStream.Web.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;

namespace LogStream.Web.Services;

public class SearchService : ISearchService
{
    private readonly ILogger<SearchService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private static readonly List<SearchLogEntry> _mockLogs = new();
    private static readonly List<SavedSearch> _savedSearches = new();
    private static readonly object _lockObject = new();

    public SearchService(ILogger<SearchService> logger, IMemoryCache cache, IConfiguration configuration)
    {
        _logger = logger;
        _cache = cache;
        _configuration = configuration;
        
        // Initialize mock data on first run
        if (!_mockLogs.Any())
        {
            InitializeMockData();
        }
    }

    public async Task<SearchResponse> SearchLogsAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing search for tenant {TenantId} with query: {Query}", request.TenantId, request.Query);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // Simulate processing delay for large datasets
            await Task.Delay(Random.Shared.Next(100, 500), cancellationToken);

            var filteredLogs = await FilterLogsAsync(request, cancellationToken);
            var totalCount = filteredLogs.Count();
            
            // Apply pagination
            var pagedResults = filteredLogs
                .Skip((request.Page - 1) * request.Size)
                .Take(request.Size)
                .ToList();

            // Generate aggregations
            var aggregations = await GenerateAggregationsAsync(filteredLogs, cancellationToken);

            stopwatch.Stop();

            return new SearchResponse
            {
                Results = pagedResults,
                TotalCount = totalCount,
                Page = request.Page,
                Size = request.Size,
                Duration = stopwatch.Elapsed,
                Aggregations = aggregations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing search for tenant {TenantId}", request.TenantId);
            throw;
        }
    }

    public async Task<long> GetTotalLogsCountAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate database call
        
        return _mockLogs.Count(log => string.IsNullOrEmpty(tenantId) || log.TenantId == tenantId);
    }

    public async Task<IEnumerable<string>> GetAvailableApplicationsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        
        return _mockLogs
            .Where(log => string.IsNullOrEmpty(tenantId) || log.TenantId == tenantId)
            .Select(log => log.Application)
            .Distinct()
            .OrderBy(app => app)
            .ToList();
    }

    public async Task<IEnumerable<string>> GetAvailableHostsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        
        return _mockLogs
            .Where(log => string.IsNullOrEmpty(tenantId) || log.TenantId == tenantId)
            .Select(log => log.Host)
            .Distinct()
            .OrderBy(host => host)
            .ToList();
    }

    public async Task<IEnumerable<SearchHistogram>> GetSearchHistogramAsync(SearchRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        
        var filteredLogs = await FilterLogsAsync(request, cancellationToken);
        
        var histogram = filteredLogs
            .GroupBy(log => new DateTime(log.Timestamp.Year, log.Timestamp.Month, log.Timestamp.Day, log.Timestamp.Hour, 0, 0))
            .Select(group => new SearchHistogram
            {
                Timestamp = group.Key,
                Count = group.Count(),
                ErrorCount = group.Count(log => log.Level == "ERROR" || log.Level == "FATAL"),
                WarningCount = group.Count(log => log.Level == "WARN")
            })
            .OrderBy(h => h.Timestamp)
            .ToList();

        return histogram;
    }

    public async Task<IEnumerable<FieldSuggestion>> GetFieldSuggestionsAsync(string tenantId, string fieldName, CancellationToken cancellationToken = default)
    {
        await Task.Delay(20, cancellationToken);
        
        var suggestions = new List<FieldSuggestion>();
        
        switch (fieldName.ToLower())
        {
            case "level":
                suggestions.Add(new FieldSuggestion
                {
                    FieldName = "level",
                    DataType = "string",
                    DocumentCount = _mockLogs.Count,
                    SampleValues = new[] { "TRACE", "DEBUG", "INFO", "WARN", "ERROR", "FATAL" },
                    IsSearchable = true,
                    IsAggregatable = true
                });
                break;
                
            case "application":
                var apps = await GetAvailableApplicationsAsync(tenantId, cancellationToken);
                suggestions.Add(new FieldSuggestion
                {
                    FieldName = "application",
                    DataType = "string",
                    DocumentCount = _mockLogs.Count,
                    SampleValues = apps.Take(10),
                    IsSearchable = true,
                    IsAggregatable = true
                });
                break;
                
            case "host":
                var hosts = await GetAvailableHostsAsync(tenantId, cancellationToken);
                suggestions.Add(new FieldSuggestion
                {
                    FieldName = "host",
                    DataType = "string",
                    DocumentCount = _mockLogs.Count,
                    SampleValues = hosts.Take(10),
                    IsSearchable = true,
                    IsAggregatable = true
                });
                break;
        }

        return suggestions;
    }

    public async Task<IEnumerable<QuerySuggestion>> GetQuerySuggestionsAsync(string tenantId, string partialQuery, CancellationToken cancellationToken = default)
    {
        await Task.Delay(30, cancellationToken);
        
        var suggestions = new List<QuerySuggestion>();
        
        if (partialQuery.Length < 2)
            return suggestions;

        // Field suggestions
        var fields = new[] { "level", "application", "environment", "host", "message", "timestamp" };
        foreach (var field in fields.Where(f => f.StartsWith(partialQuery, StringComparison.OrdinalIgnoreCase)))
        {
            suggestions.Add(new QuerySuggestion
            {
                Text = $"{field}:",
                Type = "field",
                Description = $"Search by {field}",
                Icon = "Icons.Material.Filled.Search",
                Score = 10
            });
        }

        // Operator suggestions
        var operators = new[] { "AND", "OR", "NOT" };
        foreach (var op in operators.Where(o => o.StartsWith(partialQuery, StringComparison.OrdinalIgnoreCase)))
        {
            suggestions.Add(new QuerySuggestion
            {
                Text = op,
                Type = "operator",
                Description = $"Boolean operator {op}",
                Icon = "Icons.Material.Filled.Code",
                Score = 8
            });
        }

        // Value suggestions based on existing data
        if (partialQuery.Contains(":"))
        {
            var parts = partialQuery.Split(':');
            if (parts.Length == 2)
            {
                var fieldName = parts[0];
                var valuePrefix = parts[1];
                
                var values = await GetUniqueValuesForField(tenantId, fieldName, cancellationToken);
                foreach (var value in values.Where(v => v.StartsWith(valuePrefix, StringComparison.OrdinalIgnoreCase)).Take(5))
                {
                    suggestions.Add(new QuerySuggestion
                    {
                        Text = $"{fieldName}:{value}",
                        Type = "value",
                        Description = $"Filter by {fieldName} = {value}",
                        Icon = "Icons.Material.Filled.FilterAlt",
                        Score = 6
                    });
                }
            }
        }

        return suggestions.OrderByDescending(s => s.Score).Take(10);
    }

    public async Task SaveSearchAsync(string tenantId, string name, SearchRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        
        lock (_lockObject)
        {
            var existingSearch = _savedSearches.FirstOrDefault(s => s.TenantId == tenantId && s.Name == name);
            if (existingSearch != null)
            {
                existingSearch.Request = request;
                existingSearch.LastUsed = DateTime.UtcNow;
                existingSearch.UseCount++;
            }
            else
            {
                _savedSearches.Add(new SavedSearch
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    TenantId = tenantId,
                    CreatedBy = "system", // In real app, get from current user
                    CreatedAt = DateTime.UtcNow,
                    Request = request,
                    UseCount = 1,
                    IsPublic = false,
                    Tags = Array.Empty<string>()
                });
            }
        }

        _logger.LogInformation("Saved search '{SearchName}' for tenant {TenantId}", name, tenantId);
    }

    public async Task<IEnumerable<SavedSearch>> GetSavedSearchesAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        
        return _savedSearches
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.LastUsed ?? s.CreatedAt)
            .ToList();
    }

    public async Task DeleteSavedSearchAsync(string tenantId, Guid searchId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        
        lock (_lockObject)
        {
            var search = _savedSearches.FirstOrDefault(s => s.Id == searchId && s.TenantId == tenantId);
            if (search != null)
            {
                _savedSearches.Remove(search);
                _logger.LogInformation("Deleted saved search {SearchId} for tenant {TenantId}", searchId, tenantId);
            }
        }
    }

    public async Task<SearchStats> GetSearchStatsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(50, cancellationToken);
        
        var tenantLogs = _mockLogs.Where(log => string.IsNullOrEmpty(tenantId) || log.TenantId == tenantId).ToList();
        
        if (!tenantLogs.Any())
        {
            return new SearchStats
            {
                TotalDocuments = 0,
                TotalSizeBytes = 0,
                OldestLog = DateTime.MinValue,
                NewestLog = DateTime.MinValue,
                LevelDistribution = new List<SearchBucket>(),
                ApplicationDistribution = new List<SearchBucket>(),
                EnvironmentDistribution = new List<SearchBucket>(),
                AverageLogsPerDay = 0,
                AverageLogSizeBytes = 0
            };
        }

        var totalDocs = tenantLogs.Count;
        var avgLogSize = tenantLogs.Average(log => log.Message.Length + log.Application.Length + log.Host.Length + 100); // Approximate

        return new SearchStats
        {
            TotalDocuments = totalDocs,
            TotalSizeBytes = (long)(totalDocs * avgLogSize),
            OldestLog = tenantLogs.Min(log => log.Timestamp),
            NewestLog = tenantLogs.Max(log => log.Timestamp),
            LevelDistribution = tenantLogs
                .GroupBy(log => log.Level)
                .Select(g => new SearchBucket
                {
                    Key = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / totalDocs * 100
                })
                .OrderByDescending(b => b.Count)
                .ToList(),
            ApplicationDistribution = tenantLogs
                .GroupBy(log => log.Application)
                .Select(g => new SearchBucket
                {
                    Key = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / totalDocs * 100
                })
                .OrderByDescending(b => b.Count)
                .ToList(),
            EnvironmentDistribution = tenantLogs
                .GroupBy(log => log.Environment)
                .Select(g => new SearchBucket
                {
                    Key = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / totalDocs * 100
                })
                .OrderByDescending(b => b.Count)
                .ToList(),
            AverageLogsPerDay = totalDocs / Math.Max(1, (tenantLogs.Max(log => log.Timestamp) - tenantLogs.Min(log => log.Timestamp)).TotalDays),
            AverageLogSizeBytes = avgLogSize
        };
    }

    private async Task<IEnumerable<SearchLogEntry>> FilterLogsAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var query = _mockLogs.AsQueryable();

        // Filter by tenant
        if (!string.IsNullOrEmpty(request.TenantId))
        {
            query = query.Where(log => log.TenantId == request.TenantId);
        }

        // Filter by level
        if (!string.IsNullOrEmpty(request.Level))
        {
            query = query.Where(log => log.Level == request.Level);
        }

        // Filter by application
        if (!string.IsNullOrEmpty(request.Application))
        {
            query = query.Where(log => log.Application.Contains(request.Application, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by environment
        if (!string.IsNullOrEmpty(request.Environment))
        {
            query = query.Where(log => log.Environment == request.Environment);
        }

        // Filter by host
        if (!string.IsNullOrEmpty(request.Host))
        {
            query = query.Where(log => log.Host.Contains(request.Host, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by correlation ID
        if (!string.IsNullOrEmpty(request.CorrelationId))
        {
            query = query.Where(log => log.CorrelationId == request.CorrelationId);
        }

        // Filter by date range
        if (request.StartDate.HasValue)
        {
            query = query.Where(log => log.Timestamp >= request.StartDate.Value);
        }
        if (request.EndDate.HasValue)
        {
            query = query.Where(log => log.Timestamp <= request.EndDate.Value);
        }

        // Filter by query (simple text search)
        if (!string.IsNullOrEmpty(request.Query))
        {
            var searchTerms = ParseQuery(request.Query);
            foreach (var term in searchTerms)
            {
                query = query.Where(log => log.Message.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                         log.Application.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                                         log.Host.Contains(term, StringComparison.OrdinalIgnoreCase));
            }
        }

        // Apply sorting
        query = request.SortOrder.ToLower() == "asc" 
            ? query.OrderBy(log => log.Timestamp)
            : query.OrderByDescending(log => log.Timestamp);

        await Task.Delay(1, cancellationToken); // Simulate async processing
        
        return query.ToList();
    }

    private static string[] ParseQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<string>();

        // Simple query parsing - in real implementation, use proper Lucene parser
        return query.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                   .Where(term => !string.IsNullOrWhiteSpace(term))
                   .Select(term => term.Trim('"', '\''))
                   .ToArray();
    }

    private async Task<IEnumerable<SearchAggregation>> GenerateAggregationsAsync(IEnumerable<SearchLogEntry> logs, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        
        var logsList = logs.ToList();
        var aggregations = new List<SearchAggregation>();

        // Level aggregation
        var levelAgg = new SearchAggregation
        {
            Field = "level",
            Name = "Log Levels",
            Buckets = logsList
                .GroupBy(log => log.Level)
                .Select(g => new SearchBucket
                {
                    Key = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / logsList.Count * 100
                })
                .OrderByDescending(b => b.Count)
                .ToList()
        };
        aggregations.Add(levelAgg);

        // Application aggregation
        var appAgg = new SearchAggregation
        {
            Field = "application",
            Name = "Applications",
            Buckets = logsList
                .GroupBy(log => log.Application)
                .Select(g => new SearchBucket
                {
                    Key = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / logsList.Count * 100
                })
                .OrderByDescending(b => b.Count)
                .Take(10)
                .ToList()
        };
        aggregations.Add(appAgg);

        return aggregations;
    }

    private async Task<IEnumerable<string>> GetUniqueValuesForField(string tenantId, string fieldName, CancellationToken cancellationToken)
    {
        await Task.Delay(10, cancellationToken);
        
        var tenantLogs = _mockLogs.Where(log => string.IsNullOrEmpty(tenantId) || log.TenantId == tenantId);
        
        return fieldName.ToLower() switch
        {
            "level" => tenantLogs.Select(log => log.Level).Distinct().ToList(),
            "application" => tenantLogs.Select(log => log.Application).Distinct().ToList(),
            "environment" => tenantLogs.Select(log => log.Environment).Distinct().ToList(),
            "host" => tenantLogs.Select(log => log.Host).Distinct().ToList(),
            _ => new List<string>()
        };
    }

    private static void InitializeMockData()
    {
        var random = new Random();
        var levels = new[] { "TRACE", "DEBUG", "INFO", "WARN", "ERROR", "FATAL" };
        var applications = new[] { "WebApp", "API", "Database", "Cache", "Worker", "Gateway", "Auth", "Notification" };
        var environments = new[] { "Production", "Staging", "Development", "Test" };
        var hosts = new[] { "web-01", "web-02", "api-01", "api-02", "db-01", "cache-01", "worker-01", "worker-02" };
        var tenants = new[] { "tenant-1", "tenant-2", "tenant-3", "default-tenant" };
        
        var messages = new[]
        {
            "User authentication successful",
            "Database connection established",
            "Cache miss for key: user_{0}",
            "Processing request for endpoint /api/users/{0}",
            "Failed to connect to external service",
            "Memory usage: {0}%",
            "Request completed in {0}ms",
            "Validation error: Invalid email format",
            "File uploaded successfully: {0}",
            "Background job started: {0}",
            "Email sent to user: {0}",
            "Session expired for user: {0}",
            "Rate limit exceeded for IP: {0}",
            "SQL query executed in {0}ms",
            "Redis cache updated: {0}",
            "Webhook received from {0}",
            "Payment processed: ${0}",
            "Order created: {0}",
            "User registered: {0}",
            "Password reset requested for {0}"
        };

        var exceptions = new[]
        {
            "System.NullReferenceException: Object reference not set to an instance of an object.",
            "System.ArgumentException: Invalid argument provided.",
            "System.TimeoutException: The operation has timed out.",
            "System.InvalidOperationException: Operation is not valid due to the current state of the object.",
            "System.UnauthorizedAccessException: Access to the path is denied."
        };

        lock (_lockObject)
        {
            for (int i = 0; i < 10000; i++)
            {
                var timestamp = DateTime.UtcNow.AddHours(-random.Next(0, 24 * 7)); // Last 7 days
                var level = levels[random.Next(levels.Length)];
                var application = applications[random.Next(applications.Length)];
                var environment = environments[random.Next(environments.Length)];
                var host = hosts[random.Next(hosts.Length)];
                var tenant = tenants[random.Next(tenants.Length)];
                var message = string.Format(messages[random.Next(messages.Length)], 
                    random.Next(1000, 9999), 
                    random.Next(100, 999), 
                    Guid.NewGuid().ToString("N")[..8]);

                var logEntry = new SearchLogEntry
                {
                    Id = Guid.NewGuid(),
                    Timestamp = timestamp,
                    Level = level,
                    Message = message,
                    Application = application,
                    Environment = environment,
                    Host = host,
                    TenantId = tenant,
                    CorrelationId = random.Next(1, 100) <= 30 ? Guid.NewGuid().ToString() : null,
                    TraceId = Guid.NewGuid().ToString("N"),
                    SpanId = Guid.NewGuid().ToString("N")[..16],
                    UserId = random.Next(1, 100) <= 40 ? $"user_{random.Next(1, 1000)}" : null,
                    SessionId = Guid.NewGuid().ToString("N")[..16],
                    Exception = level == "ERROR" && random.Next(1, 100) <= 30 ? exceptions[random.Next(exceptions.Length)] : null,
                    Properties = new Dictionary<string, object>
                    {
                        ["RequestId"] = Guid.NewGuid().ToString(),
                        ["UserAgent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                        ["IpAddress"] = $"{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}",
                        ["ResponseTime"] = random.Next(50, 5000)
                    },
                    Tags = new Dictionary<string, string>
                    {
                        ["Version"] = "1.0.0",
                        ["Environment"] = environment,
                        ["Region"] = random.Next(1, 100) <= 50 ? "us-east-1" : "us-west-2"
                    },
                    Duration = random.Next(10, 1000),
                    RequestId = Guid.NewGuid().ToString(),
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                    IpAddress = $"{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}",
                    OriginalFormat = "JSON"
                };

                _mockLogs.Add(logEntry);
            }
        }
    }
}

// Extension class for SearchLogEntry to include TenantId
public static class SearchLogEntryExtensions
{
    private static readonly Dictionary<Guid, string> _tenantMapping = new();

    public static string TenantId(this SearchLogEntry entry)
    {
        if (_tenantMapping.TryGetValue(entry.Id, out var tenantId))
            return tenantId;

        // Generate tenant based on ID hash for consistency
        var hash = entry.Id.GetHashCode();
        var tenants = new[] { "tenant-1", "tenant-2", "tenant-3", "default-tenant" };
        var selectedTenant = tenants[Math.Abs(hash) % tenants.Length];
        
        _tenantMapping[entry.Id] = selectedTenant;
        return selectedTenant;
    }
}

// Add TenantId property to SearchLogEntry
public static class SearchLogEntryExtender
{
    public static string GetTenantId(this SearchLogEntry entry)
    {
        return entry.TenantId();
    }
}

// Temporary extension until we modify the model
namespace LogStream.Web.Models
{
    public partial class SearchLogEntry
    {
        public string TenantId { get; set; } = "default-tenant";
    }
}