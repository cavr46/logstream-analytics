namespace LogStream.Web.Models;

public class SearchRequest
{
    public string TenantId { get; set; } = string.Empty;
    public string? Query { get; set; }
    public string? Level { get; set; }
    public string? Application { get; set; }
    public string? Environment { get; set; }
    public string? Host { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int MaxResults { get; set; } = 1000;
    public bool IncludeStackTrace { get; set; } = true;
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 100;
    public string SortBy { get; set; } = "timestamp";
    public string SortOrder { get; set; } = "desc";
}

public class SearchResponse
{
    public IEnumerable<SearchLogEntry> Results { get; set; } = new List<SearchLogEntry>();
    public long TotalCount { get; set; }
    public int Page { get; set; }
    public int Size { get; set; }
    public TimeSpan Duration { get; set; }
    public IEnumerable<SearchAggregation> Aggregations { get; set; } = new List<SearchAggregation>();
}

public class SearchLogEntry
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Application { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public string? Exception { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new();
    public string? StackTrace { get; set; }
    public double? Duration { get; set; }
    public string? RequestId { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string OriginalFormat { get; set; } = string.Empty;
}

public class SearchAggregation
{
    public string Field { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public IEnumerable<SearchBucket> Buckets { get; set; } = new List<SearchBucket>();
}

public class SearchBucket
{
    public string Key { get; set; } = string.Empty;
    public long Count { get; set; }
    public double Percentage { get; set; }
}

public class SearchHistogram
{
    public DateTime Timestamp { get; set; }
    public long Count { get; set; }
    public long ErrorCount { get; set; }
    public long WarningCount { get; set; }
}

public class SavedSearch
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsed { get; set; }
    public int UseCount { get; set; }
    public SearchRequest Request { get; set; } = new();
    public bool IsPublic { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}

public class QuerySuggestion
{
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // field, value, operator
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class FieldSuggestion
{
    public string FieldName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public long DocumentCount { get; set; }
    public IEnumerable<string> SampleValues { get; set; } = new List<string>();
    public bool IsSearchable { get; set; } = true;
    public bool IsAggregatable { get; set; } = true;
}

public class SearchAlert
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public SearchRequest SearchQuery { get; set; } = new();
    public SearchAlertCondition Condition { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastTriggered { get; set; }
    public int TriggerCount { get; set; }
    public string[] NotificationChannels { get; set; } = Array.Empty<string>();
}

public class SearchAlertCondition
{
    public string Type { get; set; } = string.Empty; // count, rate, anomaly
    public string Operator { get; set; } = string.Empty; // gt, lt, eq, ne
    public double Threshold { get; set; }
    public TimeSpan TimeWindow { get; set; }
    public string? GroupBy { get; set; }
}

public class LogCorrelation
{
    public string CorrelationId { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int LogCount { get; set; }
    public int ErrorCount { get; set; }
    public IEnumerable<string> Applications { get; set; } = new List<string>();
    public IEnumerable<string> Hosts { get; set; } = new List<string>();
    public IEnumerable<SearchLogEntry> Logs { get; set; } = new List<SearchLogEntry>();
}

public class SearchExportRequest
{
    public SearchRequest SearchRequest { get; set; } = new();
    public string Format { get; set; } = "csv"; // csv, json, excel, pdf
    public bool IncludeHeaders { get; set; } = true;
    public string[] Fields { get; set; } = Array.Empty<string>();
    public int MaxRecords { get; set; } = 10000;
    public bool CompressOutput { get; set; } = false;
}

public class SearchStats
{
    public long TotalDocuments { get; set; }
    public long TotalSizeBytes { get; set; }
    public DateTime OldestLog { get; set; }
    public DateTime NewestLog { get; set; }
    public IEnumerable<SearchBucket> LevelDistribution { get; set; } = new List<SearchBucket>();
    public IEnumerable<SearchBucket> ApplicationDistribution { get; set; } = new List<SearchBucket>();
    public IEnumerable<SearchBucket> EnvironmentDistribution { get; set; } = new List<SearchBucket>();
    public double AverageLogsPerDay { get; set; }
    public double AverageLogSizeBytes { get; set; }
}