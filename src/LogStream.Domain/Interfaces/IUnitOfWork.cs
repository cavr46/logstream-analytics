namespace LogStream.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ITenantRepository Tenants { get; }
    ILogEntryRepository LogEntries { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}