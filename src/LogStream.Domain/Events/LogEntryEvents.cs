using LogStream.Domain.Common;
using LogStream.Domain.ValueObjects;

namespace LogStream.Domain.Events;

public record LogEntryCreatedEvent(Guid LogEntryId, TenantId TenantId, LogLevel Level, DateTime Timestamp) : BaseDomainEvent;

public record LogEntryProcessedEvent(Guid LogEntryId, TenantId TenantId, LogLevel Level) : BaseDomainEvent;

public record LogEntryArchivedEvent(Guid LogEntryId, TenantId TenantId) : BaseDomainEvent;