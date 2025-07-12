namespace LogStream.Contracts.DTOs;

public record LogEntryDto
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? MessageTemplate { get; init; }
    public LogSourceDto Source { get; init; } = new();
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
    public string? UserId { get; init; }
    public string? SessionId { get; init; }
    public string? CorrelationId { get; init; }
    public string? Exception { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public string? Tags { get; init; }
    public string OriginalFormat { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public bool IsProcessed { get; init; }
    public bool IsArchived { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record LogSourceDto
{
    public string Application { get; init; } = string.Empty;
    public string Environment { get; init; } = string.Empty;
    public string? Server { get; init; }
    public string? Component { get; init; }
}

public record CreateLogEntryRequest
{
    public DateTime? Timestamp { get; init; }
    public string Level { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? MessageTemplate { get; init; }
    public LogSourceDto Source { get; init; } = new();
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
    public string? UserId { get; init; }
    public string? SessionId { get; init; }
    public string? CorrelationId { get; init; }
    public string? Exception { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public string? Tags { get; init; }
    public string OriginalFormat { get; init; } = "JSON";
    public string? RawContent { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

public record LogSearchRequest
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
}

public record BulkLogIngestRequest
{
    public IReadOnlyList<CreateLogEntryRequest> LogEntries { get; init; } = Array.Empty<CreateLogEntryRequest>();
}