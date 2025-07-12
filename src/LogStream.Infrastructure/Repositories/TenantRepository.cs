using LogStream.Infrastructure.Data;

namespace LogStream.Infrastructure.Repositories;

public class TenantRepository : Repository<Tenant>, ITenantRepository
{
    public TenantRepository(LogStreamDbContext context) : base(context)
    {
    }

    public async Task<Tenant?> GetByTenantIdAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken);
    }

    public async Task<bool> TenantExistsAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(t => t.TenantId == tenantId, cancellationToken);
    }

    public async Task<IReadOnlyList<Tenant>> GetActiveTenants(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tenant?> GetTenantByApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        return await DbSet
            .Where(t => t.IsActive)
            .AsEnumerable() // Switch to client evaluation for complex JSON queries
            .FirstOrDefault(t => t.IsValidApiKey(apiKey));
    }
}