using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using LogStream.Domain.Interfaces;
using LogStream.Functions.Services;

namespace LogStream.Functions.Functions;

public class ArchivalFunction
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IArchivalService _archivalService;
    private readonly ILogger<ArchivalFunction> _logger;

    public ArchivalFunction(
        IUnitOfWork unitOfWork,
        IArchivalService archivalService,
        ILogger<ArchivalFunction> logger)
    {
        _unitOfWork = unitOfWork;
        _archivalService = archivalService;
        _logger = logger;
    }

    [Function("ArchiveOldLogs")]
    public async Task ArchiveOldLogs([TimerTrigger("0 0 2 * * *")] TimerInfo timer) // Daily at 2 AM
    {
        _logger.LogInformation("Starting log archival process at {Time}", DateTime.UtcNow);

        try
        {
            var activeTenants = await _unitOfWork.Tenants.GetActiveTenants();
            
            _logger.LogInformation("Starting archival for {Count} active tenants", activeTenants.Count);

            foreach (var tenant in activeTenants)
            {
                try
                {
                    await _archivalService.ArchiveOldLogsAsync(tenant.TenantId);
                    _logger.LogInformation("Completed archival for tenant {TenantId}", tenant.TenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error archiving logs for tenant {TenantId}", tenant.TenantId);
                    // Continue with other tenants
                }
            }

            _logger.LogInformation("Completed log archival process for all tenants");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during log archival process");
            throw;
        }
    }

    [Function("CleanupArchivedLogs")]
    public async Task CleanupArchivedLogs([TimerTrigger("0 0 3 * * 0")] TimerInfo timer) // Weekly on Sunday at 3 AM
    {
        _logger.LogInformation("Starting archived logs cleanup at {Time}", DateTime.UtcNow);

        try
        {
            var activeTenants = await _unitOfWork.Tenants.GetActiveTenants();
            var cleanupDate = DateTime.UtcNow.AddDays(-365); // Keep archived logs for 1 year
            
            _logger.LogInformation("Starting cleanup for {Count} active tenants", activeTenants.Count);

            foreach (var tenant in activeTenants)
            {
                try
                {
                    await _archivalService.DeleteArchivedLogsAsync(tenant.TenantId, cleanupDate);
                    _logger.LogInformation("Completed cleanup for tenant {TenantId}", tenant.TenantId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up archived logs for tenant {TenantId}", tenant.TenantId);
                    // Continue with other tenants
                }
            }

            _logger.LogInformation("Completed archived logs cleanup for all tenants");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during archived logs cleanup");
            throw;
        }
    }

    [Function("CheckStorageQuotas")]
    public async Task CheckStorageQuotas([TimerTrigger("0 0 */6 * * *")] TimerInfo timer) // Every 6 hours
    {
        _logger.LogInformation("Starting storage quota check at {Time}", DateTime.UtcNow);

        try
        {
            var activeTenants = await _unitOfWork.Tenants.GetActiveTenants();
            
            foreach (var tenant in activeTenants)
            {
                try
                {
                    var totalSize = await _unitOfWork.LogEntries.GetTotalSizeBytesAsync(tenant.TenantId);
                    var usagePercentage = (double)totalSize / tenant.MaxLogSizeBytes * 100;

                    if (usagePercentage > 90) // 90% quota usage
                    {
                        _logger.LogWarning("Tenant {TenantId} is using {Percentage:F1}% of storage quota ({UsedSize}/{MaxSize} bytes)",
                            tenant.TenantId, usagePercentage, totalSize, tenant.MaxLogSizeBytes);

                        // In a real implementation, send notification to tenant
                    }
                    else if (usagePercentage > 80) // 80% quota usage
                    {
                        _logger.LogInformation("Tenant {TenantId} is using {Percentage:F1}% of storage quota",
                            tenant.TenantId, usagePercentage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking storage quota for tenant {TenantId}", tenant.TenantId);
                }
            }

            _logger.LogInformation("Completed storage quota check for all tenants");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during storage quota check");
            throw;
        }
    }

    [Function("GenerateRetentionReport")]
    public async Task GenerateRetentionReport([TimerTrigger("0 0 4 1 * *")] TimerInfo timer) // Monthly on 1st at 4 AM
    {
        _logger.LogInformation("Starting monthly retention report generation at {Time}", DateTime.UtcNow);

        try
        {
            var activeTenants = await _unitOfWork.Tenants.GetActiveTenants();
            var reportDate = DateTime.UtcNow;
            var previousMonth = reportDate.AddMonths(-1);

            foreach (var tenant in activeTenants)
            {
                try
                {
                    var totalLogs = await _unitOfWork.LogEntries.CountAsync(
                        l => l.TenantId == tenant.TenantId);

                    var archivedLogs = await _unitOfWork.LogEntries.CountAsync(
                        l => l.TenantId == tenant.TenantId && l.IsArchived);

                    var monthlyLogs = await _unitOfWork.LogEntries.CountAsync(
                        l => l.TenantId == tenant.TenantId && 
                             l.Timestamp >= previousMonth.Date && 
                             l.Timestamp < reportDate.Date);

                    var totalSize = await _unitOfWork.LogEntries.GetTotalSizeBytesAsync(tenant.TenantId);

                    _logger.LogInformation(
                        "Retention Report for {TenantId}: Total: {Total}, Archived: {Archived}, " +
                        "Last Month: {Monthly}, Size: {Size} MB, Retention: {Retention} days",
                        tenant.TenantId, totalLogs, archivedLogs, monthlyLogs, 
                        totalSize / 1024 / 1024, tenant.MaxRetentionDays);

                    // In a real implementation, you would:
                    // 1. Generate detailed report with charts
                    // 2. Send report to tenant administrators
                    // 3. Store report in blob storage
                    // 4. Update tenant usage metrics
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating retention report for tenant {TenantId}", tenant.TenantId);
                }
            }

            _logger.LogInformation("Completed monthly retention report generation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during retention report generation");
            throw;
        }
    }
}