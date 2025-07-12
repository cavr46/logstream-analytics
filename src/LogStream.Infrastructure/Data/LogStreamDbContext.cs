using LogStream.Domain.Common;
using LogStream.Infrastructure.Configurations;
using MediatR;

namespace LogStream.Infrastructure.Data;

public class LogStreamDbContext : DbContext
{
    private readonly IMediator _mediator;

    public LogStreamDbContext(DbContextOptions<LogStreamDbContext> options, IMediator mediator) 
        : base(options)
    {
        _mediator = mediator;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new LogEntryConfiguration());

        // Configure database indexes for performance
        ConfigureIndexes(modelBuilder);

        // Configure multi-tenancy partitioning
        ConfigurePartitioning(modelBuilder);
    }

    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Tenant indexes
        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.TenantId)
            .IsUnique()
            .HasDatabaseName("IX_Tenants_TenantId");

        modelBuilder.Entity<Tenant>()
            .HasIndex(t => t.IsActive)
            .HasDatabaseName("IX_Tenants_IsActive");

        // LogEntry indexes for high-performance queries
        modelBuilder.Entity<LogEntry>()
            .HasIndex(l => new { l.TenantId, l.Timestamp })
            .HasDatabaseName("IX_LogEntries_TenantId_Timestamp");

        modelBuilder.Entity<LogEntry>()
            .HasIndex(l => new { l.TenantId, l.Level, l.Timestamp })
            .HasDatabaseName("IX_LogEntries_TenantId_Level_Timestamp");

        modelBuilder.Entity<LogEntry>()
            .HasIndex(l => new { l.TenantId, l.IsProcessed, l.CreatedAt })
            .HasDatabaseName("IX_LogEntries_TenantId_IsProcessed_CreatedAt");

        modelBuilder.Entity<LogEntry>()
            .HasIndex(l => new { l.TenantId, l.IsArchived, l.CreatedAt })
            .HasDatabaseName("IX_LogEntries_TenantId_IsArchived_CreatedAt");

        modelBuilder.Entity<LogEntry>()
            .HasIndex(l => l.TraceId)
            .HasDatabaseName("IX_LogEntries_TraceId");

        modelBuilder.Entity<LogEntry>()
            .HasIndex(l => l.CorrelationId)
            .HasDatabaseName("IX_LogEntries_CorrelationId");
    }

    private static void ConfigurePartitioning(ModelBuilder modelBuilder)
    {
        // Configure table partitioning by TenantId and Date for scalability
        // This would be implemented with SQL Server partitioning in production
        modelBuilder.Entity<LogEntry>()
            .ToTable("LogEntries", t => t.HasComment("Partitioned by TenantId and Timestamp for optimal query performance"));
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync()
    {
        var domainEntities = ChangeTracker
            .Entries<BaseEntity>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent);
        }
    }
}