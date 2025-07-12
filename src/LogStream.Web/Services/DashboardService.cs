using LogStream.Domain.Interfaces;
using LogStream.Domain.ValueObjects;
using LogStream.Web.Models;
using Microsoft.Extensions.Logging;

namespace LogStream.Web.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IUnitOfWork unitOfWork, ILogger<DashboardService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenant = await _unitOfWork.Tenants.GetByTenantIdAsync(new TenantId(tenantId), cancellationToken);
            if (tenant == null)
            {
                throw new ArgumentException($"Tenant '{tenantId}' not found");
            }

            var tasks = new Task[]
            {
                GetMetricsAsync(tenantId, cancellationToken),
                GetRecentLogsAsync(tenantId, 20, cancellationToken),
                GetActiveAlertsAsync(tenantId, cancellationToken),
                GetLogLevelDistributionAsync(tenantId, cancellationToken: cancellationToken),
                GetTopApplicationsAsync(tenantId, 5, cancellationToken)
            };

            await Task.WhenAll(tasks);

            return new DashboardViewModel
            {
                TenantId = tenantId,
                TenantName = tenant.Name,
                Metrics = await (Task<DashboardMetrics>)tasks[0],
                RecentLogs = await (Task<IList<RecentLogEntry>>)tasks[1],
                ActiveAlerts = await (Task<IList<Alert>>)tasks[2],
                TopLogLevels = await (Task<IList<LogLevel>>)tasks[3],
                TopApplications = await (Task<IList<Application>>)tasks[4]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data for tenant {TenantId}", tenantId);
            throw;
        }
    }

    public async Task<DashboardMetrics> GetMetricsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = new TenantId(tenantId);
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var totalLogsToday = await _unitOfWork.LogEntries.CountAsync(
            l => l.TenantId == tenant && l.Timestamp >= today, cancellationToken);

        var totalLogsThisWeek = await _unitOfWork.LogEntries.CountAsync(
            l => l.TenantId == tenant && l.Timestamp >= weekStart, cancellationToken);

        var totalLogsThisMonth = await _unitOfWork.LogEntries.CountAsync(
            l => l.TenantId == tenant && l.Timestamp >= monthStart, cancellationToken);

        var errorLogsToday = await _unitOfWork.LogEntries.CountAsync(
            l => l.TenantId == tenant && l.Timestamp >= today && 
                 (l.Level.Value == "ERROR" || l.Level.Value == "FATAL"), cancellationToken);

        var warningLogsToday = await _unitOfWork.LogEntries.CountAsync(
            l => l.TenantId == tenant && l.Timestamp >= today && l.Level.Value == "WARN", cancellationToken);

        var storageUsedBytes = await _unitOfWork.LogEntries.GetTotalSizeBytesAsync(tenant, cancellationToken);
        var tenantEntity = await _unitOfWork.Tenants.GetByTenantIdAsync(tenant, cancellationToken);

        var recentLogs = await _unitOfWork.LogEntries.GetRecentLogsByTenantAsync(tenant, 1, cancellationToken);
        
        return new DashboardMetrics
        {
            TotalLogsToday = totalLogsToday,
            TotalLogsThisWeek = totalLogsThisWeek,
            TotalLogsThisMonth = totalLogsThisMonth,
            ErrorLogsToday = errorLogsToday,
            WarningLogsToday = warningLogsToday,
            ErrorRate = totalLogsToday > 0 ? (double)errorLogsToday / totalLogsToday * 100 : 0,
            StorageUsedMB = storageUsedBytes / 1024 / 1024,
            StorageUsagePercentage = tenantEntity != null ? (double)storageUsedBytes / tenantEntity.MaxLogSizeBytes * 100 : 0,
            LastLogReceived = recentLogs.FirstOrDefault()?.Timestamp ?? DateTime.MinValue
        };
    }

    public async Task<IList<RecentLogEntry>> GetRecentLogsAsync(string tenantId, int count = 50, CancellationToken cancellationToken = default)
    {
        var tenant = new TenantId(tenantId);
        var logs = await _unitOfWork.LogEntries.GetRecentLogsByTenantAsync(tenant, count, cancellationToken);

        return logs.Select(l => new RecentLogEntry
        {
            Id = l.Id,
            Timestamp = l.Timestamp,
            Level = l.Level,
            Message = l.Message.Content.Length > 200 ? l.Message.Content[..200] + "..." : l.Message.Content,
            Application = l.Source.Application,
            Environment = l.Source.Environment,
            Exception = l.Exception
        }).ToList();
    }

    public async Task<IList<Alert>> GetActiveAlertsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        // In a real implementation, you would have an Alerts table
        // For now, return simulated active alerts based on recent error logs
        var tenant = new TenantId(tenantId);
        var recentErrors = await _unitOfWork.LogEntries.FindAsync(
            l => l.TenantId == tenant && 
                 l.Timestamp >= DateTime.UtcNow.AddHours(-1) &&
                 l.Level.Value == "FATAL", cancellationToken);

        return recentErrors.Take(5).Select(l => new Alert
        {
            Id = Guid.NewGuid(),
            Title = "Critical Error Detected",
            Message = $"Critical error in {l.Source.Application}: {l.Message.Content[..100]}...",
            Severity = "CRITICAL",
            CreatedAt = l.Timestamp,
            IsAcknowledged = false
        }).ToList();
    }

    public async Task<IList<LogLevel>> GetLogLevelDistributionAsync(string tenantId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        var tenant = new TenantId(tenantId);
        startDate ??= DateTime.UtcNow.Date;
        endDate ??= DateTime.UtcNow;

        var logs = await _unitOfWork.LogEntries.FindAsync(
            l => l.TenantId == tenant && l.Timestamp >= startDate && l.Timestamp <= endDate, cancellationToken);

        var grouped = logs.GroupBy(l => l.Level.Value)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var total = grouped.Sum(x => x.Count);
        var colors = new Dictionary<string, string>
        {
            { "TRACE", "#9E9E9E" },
            { "DEBUG", "#2196F3" },
            { "INFO", "#4CAF50" },
            { "WARN", "#FF9800" },
            { "ERROR", "#F44336" },
            { "FATAL", "#9C27B0" }
        };

        return grouped.Select(g => new LogLevel
        {
            Name = g.Level,
            Count = g.Count,
            Percentage = total > 0 ? (double)g.Count / total * 100 : 0,
            Color = colors.GetValueOrDefault(g.Level, "#000000")
        }).ToList();
    }

    public async Task<IList<TimeSeriesDataPoint>> GetLogVolumeTimeSeriesAsync(string tenantId, DateTime startDate, DateTime endDate, string granularity = "hour", CancellationToken cancellationToken = default)
    {
        var tenant = new TenantId(tenantId);
        var logs = await _unitOfWork.LogEntries.FindAsync(
            l => l.TenantId == tenant && l.Timestamp >= startDate && l.Timestamp <= endDate, cancellationToken);

        var grouped = granularity.ToLower() switch
        {
            "minute" => logs.GroupBy(l => new DateTime(l.Timestamp.Year, l.Timestamp.Month, l.Timestamp.Day, l.Timestamp.Hour, l.Timestamp.Minute, 0)),
            "hour" => logs.GroupBy(l => new DateTime(l.Timestamp.Year, l.Timestamp.Month, l.Timestamp.Day, l.Timestamp.Hour, 0, 0)),
            "day" => logs.GroupBy(l => l.Timestamp.Date),
            _ => logs.GroupBy(l => new DateTime(l.Timestamp.Year, l.Timestamp.Month, l.Timestamp.Day, l.Timestamp.Hour, 0, 0))
        };

        return grouped.Select(g => new TimeSeriesDataPoint
        {
            Timestamp = g.Key,
            Value = g.Count(),
            Series = "Total Logs"
        }).OrderBy(d => d.Timestamp).ToList();
    }

    public async Task<IList<Application>> GetTopApplicationsAsync(string tenantId, int count = 10, CancellationToken cancellationToken = default)
    {
        var tenant = new TenantId(tenantId);
        var today = DateTime.UtcNow.Date;
        
        var logs = await _unitOfWork.LogEntries.FindAsync(
            l => l.TenantId == tenant && l.Timestamp >= today, cancellationToken);

        var grouped = logs.GroupBy(l => new { l.Source.Application, l.Source.Environment })
            .Select(g => new Application
            {
                Name = g.Key.Application,
                Environment = g.Key.Environment,
                LogCount = g.Count(),
                ErrorCount = g.Count(l => l.Level.Value == "ERROR" || l.Level.Value == "FATAL"),
                LastSeen = g.Max(l => l.Timestamp),
                Status = g.Any(l => l.Timestamp >= DateTime.UtcNow.AddMinutes(-5)) ? "Active" : "Inactive"
            })
            .OrderByDescending(a => a.LogCount)
            .Take(count)
            .ToList();

        return grouped;
    }
}