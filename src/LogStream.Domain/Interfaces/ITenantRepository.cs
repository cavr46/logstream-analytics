using LogStream.Domain.Entities;
using LogStream.Domain.ValueObjects;

namespace LogStream.Domain.Interfaces;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetByTenantIdAsync(TenantId tenantId, CancellationToken cancellationToken = default);
    Task<bool> TenantExistsAsync(TenantId tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> GetActiveTenants(CancellationToken cancellationToken = default);
    Task<Tenant?> GetTenantByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}