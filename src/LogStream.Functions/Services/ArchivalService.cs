using LogStream.Domain.Entities;
using LogStream.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace LogStream.Functions.Services;

public class ArchivalService : IArchivalService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ArchivalService> _logger;

    public ArchivalService(IUnitOfWork unitOfWork, ILogger<ArchivalService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ArchiveOldLogsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenant = await _unitOfWork.Tenants.GetByTenantIdAsync(tenantId, cancellationToken);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant {TenantId} not found for archival", tenantId);
                return;
            }

            var archivalDate = DateTime.UtcNow.AddDays(-tenant.MaxRetentionDays);
            var batchSize = 1000;

            while (true)
            {
                var logsToArchive = await _unitOfWork.LogEntries.GetLogsForArchivalAsync(
                    archivalDate, batchSize, cancellationToken);

                if (!logsToArchive.Any())
                    break;

                await ArchiveLogsBatchAsync(logsToArchive, cancellationToken);
                
                _logger.LogInformation("Archived batch of {Count} logs for tenant {TenantId}", 
                    logsToArchive.Count, tenantId);
            }

            _logger.LogInformation("Completed archival process for tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during archival process for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task ArchiveLogsBatchAsync(IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken = default)
    {
        var logEntryList = logEntries.ToList();
        
        try
        {
            // Compress and store logs to cold storage
            await CompressAndStoreAsync(logEntryList, cancellationToken);

            // Mark logs as archived
            foreach (var logEntry in logEntryList)
            {
                logEntry.Archive("archival-service");
                _unitOfWork.LogEntries.Update(logEntry);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully archived {Count} log entries", logEntryList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving log entries batch");
            throw;
        }
    }

    public async Task DeleteArchivedLogsAsync(string tenantId, DateTime olderThan, CancellationToken cancellationToken = default)
    {
        try
        {
            var batchSize = 1000;

            while (true)
            {
                var archivedLogs = await _unitOfWork.LogEntries.FindAsync(
                    l => l.TenantId == tenantId && 
                         l.IsArchived && 
                         l.ArchivedAt < olderThan,
                    cancellationToken);

                var batch = archivedLogs.Take(batchSize).ToList();
                if (!batch.Any())
                    break;

                _unitOfWork.LogEntries.RemoveRange(batch);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Deleted batch of {Count} archived logs for tenant {TenantId}", 
                    batch.Count, tenantId);
            }

            _logger.LogInformation("Completed deletion of archived logs for tenant {TenantId}", tenantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting archived logs for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task CompressAndStoreAsync(IEnumerable<LogEntry> logEntries, CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation, you would:
            // 1. Serialize log entries to JSON or other format
            // 2. Compress the data (gzip, lz4, etc.)
            // 3. Store to Azure Blob Storage, AWS S3, or other cold storage
            // 4. Optionally store metadata about the archived file

            var logEntryList = logEntries.ToList();
            var totalSize = logEntryList.Sum(l => l.SizeBytes);

            _logger.LogInformation("Compressing and storing {Count} log entries ({Size} bytes)", 
                logEntryList.Count, totalSize);

            // Simulate compression and storage
            await Task.Delay(100, cancellationToken);

            // In production:
            // var compressedData = await CompressLogsAsync(logEntryList);
            // var archiveFileName = $"logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.gz";
            // await _blobStorageService.UploadAsync(archiveFileName, compressedData);

            _logger.LogInformation("Successfully compressed and stored {Count} log entries", logEntryList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error compressing and storing log entries");
            throw;
        }
    }
}