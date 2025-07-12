using LogStream.Domain.Common;
using LogStream.Domain.ValueObjects;

namespace LogStream.Domain.Events;

public record TenantCreatedEvent(Guid TenantEntityId, TenantId TenantId) : BaseDomainEvent;

public record TenantUpdatedEvent(Guid TenantEntityId, TenantId TenantId, string OldName, string NewName) : BaseDomainEvent;

public record TenantActivatedEvent(Guid TenantEntityId, TenantId TenantId) : BaseDomainEvent;

public record TenantDeactivatedEvent(Guid TenantEntityId, TenantId TenantId) : BaseDomainEvent;