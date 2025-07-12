using LogStream.Domain.Common;
using LogStream.Domain.ValueObjects;
using LogStream.Domain.Events;

namespace LogStream.Domain.Entities;

public class LogEntry : BaseEntity
{
    public TenantId TenantId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public LogLevel Level { get; private set; }
    public LogMessage Message { get; private set; }
    public LogSource Source { get; private set; }
    public string? TraceId { get; private set; }
    public string? SpanId { get; private set; }
    public string? UserId { get; private set; }
    public string? SessionId { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? Exception { get; private set; }
    public IReadOnlyDictionary<string, object> Metadata { get; private set; }
    public string? Tags { get; private set; }
    public string OriginalFormat { get; private set; }
    public string RawContent { get; private set; }
    public long SizeBytes { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public bool IsProcessed { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTime? ArchivedAt { get; private set; }

    protected LogEntry() { } // EF Core

    public LogEntry(
        TenantId tenantId,
        DateTime timestamp,
        LogLevel level,
        LogMessage message,
        LogSource source,
        string originalFormat,
        string rawContent,
        string? traceId = null,
        string? spanId = null,
        string? userId = null,
        string? sessionId = null,
        string? correlationId = null,
        string? exception = null,
        IReadOnlyDictionary<string, object>? metadata = null,
        string? tags = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        Timestamp = timestamp;
        Level = level ?? throw new ArgumentNullException(nameof(level));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Source = source ?? throw new ArgumentNullException(nameof(source));
        OriginalFormat = !string.IsNullOrWhiteSpace(originalFormat) ? originalFormat : throw new ArgumentException("Original format cannot be empty", nameof(originalFormat));
        RawContent = !string.IsNullOrEmpty(rawContent) ? rawContent : throw new ArgumentException("Raw content cannot be empty", nameof(rawContent));
        TraceId = traceId;
        SpanId = spanId;
        UserId = userId;
        SessionId = sessionId;
        CorrelationId = correlationId;
        Exception = exception;
        Metadata = metadata ?? new Dictionary<string, object>();
        Tags = tags;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        SizeBytes = System.Text.Encoding.UTF8.GetByteCount(rawContent);
        IsProcessed = false;
        IsArchived = false;

        AddDomainEvent(new LogEntryCreatedEvent(Id, TenantId, Level, timestamp));
    }

    public void MarkAsProcessed(string processedBy)
    {
        if (IsProcessed) return;

        IsProcessed = true;
        MarkAsUpdated(processedBy);

        AddDomainEvent(new LogEntryProcessedEvent(Id, TenantId, Level));
    }

    public void Archive(string archivedBy)
    {
        if (IsArchived) return;

        IsArchived = true;
        ArchivedAt = DateTime.UtcNow;
        MarkAsUpdated(archivedBy);

        AddDomainEvent(new LogEntryArchivedEvent(Id, TenantId));
    }

    public void UpdateTracing(string? traceId, string? spanId, string? correlationId, string updatedBy)
    {
        TraceId = traceId;
        SpanId = spanId;
        CorrelationId = correlationId;
        MarkAsUpdated(updatedBy);
    }

    public void UpdateException(string? exception, string updatedBy)
    {
        Exception = exception;
        MarkAsUpdated(updatedBy);
    }

    public void UpdateTags(string? tags, string updatedBy)
    {
        Tags = tags;
        MarkAsUpdated(updatedBy);
    }

    public bool ContainsKeyword(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return false;

        return Message.ContainsKeyword(keyword) ||
               Exception?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true ||
               Tags?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true ||
               RawContent.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
               Metadata.Values.Any(v => v?.ToString()?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true);
    }

    public bool MatchesLevel(LogLevel targetLevel) => Level.Value == targetLevel.Value;

    public bool IsMoreSevereThan(LogLevel targetLevel) => Level.IsMoreSevereThan(targetLevel);

    public bool IsFromDateRange(DateTime startDate, DateTime endDate) => 
        Timestamp >= startDate && Timestamp <= endDate;

    public bool IsFromSource(string application, string? environment = null, string? server = null)
    {
        if (!Source.Application.Equals(application, StringComparison.OrdinalIgnoreCase))
            return false;

        if (environment != null && !Source.Environment.Equals(environment, StringComparison.OrdinalIgnoreCase))
            return false;

        if (server != null && !string.Equals(Source.Server, server, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}