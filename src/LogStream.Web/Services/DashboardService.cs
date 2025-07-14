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

    public async Task<DashboardViewModel> GetDashboardDataAsync(string tenantId, string timeRange = "24h", CancellationToken cancellationToken = default)
    {
        try
        {
            var tenant = await _unitOfWork.Tenants.GetByTenantIdAsync(new TenantId(tenantId), cancellationToken);
            if (tenant == null)
            {
                throw new ArgumentException($"Tenant '{tenantId}' not found");
            }

            // Parse time range to get start/end dates
            var (startDate, endDate) = ParseTimeRange(timeRange);

            var metricsTask = GetMetricsAsync(tenantId, cancellationToken);
            var recentLogsTask = GetRecentLogsAsync(tenantId, 20, cancellationToken);
            var activeAlertsTask = GetActiveAlertsAsync(tenantId, cancellationToken);
            var logLevelDistributionTask = GetLogLevelDistributionDataAsync(tenantId, startDate, endDate, cancellationToken);
            var topApplicationsTask = GetTopApplicationsAsync(tenantId, 5, cancellationToken);
            var logVolumeTrendsTask = GetLogVolumeTrendsAsync(tenantId, startDate, endDate, cancellationToken);
            var systemHealthTask = GetSystemHealthAsync(cancellationToken);
            var topErrorsTask = GetTopErrorsAsync(tenantId, 8, cancellationToken);

            await Task.WhenAll(metricsTask, recentLogsTask, activeAlertsTask, logLevelDistributionTask, 
                              topApplicationsTask, logVolumeTrendsTask, systemHealthTask, topErrorsTask);

            return new DashboardViewModel
            {
                TenantId = tenantId,
                TenantName = tenant.Name,
                Metrics = await metricsTask,
                RecentLogs = await recentLogsTask,
                ActiveAlerts = await activeAlertsTask,
                LogLevelDistribution = await logLevelDistributionTask,
                TopApplications = await topApplicationsTask,
                LogVolumeTrends = await logVolumeTrendsTask,
                SystemHealth = await systemHealthTask,
                TopErrors = await topErrorsTask
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
        
        // Calculate trend percentages (compare with yesterday)
        var yesterday = today.AddDays(-1);
        var logsYesterday = await _unitOfWork.LogEntries.CountAsync(
            l => l.TenantId == tenant && l.Timestamp >= yesterday && l.Timestamp < today, cancellationToken);
        var errorsYesterday = await _unitOfWork.LogEntries.CountAsync(
            l => l.TenantId == tenant && l.Timestamp >= yesterday && l.Timestamp < today &&
                 (l.Level.Value == "ERROR" || l.Level.Value == "FATAL"), cancellationToken);

        var logsTrend = logsYesterday > 0 ? ((double)(totalLogsToday - logsYesterday) / logsYesterday) * 100 : 0;
        var errorRate = totalLogsToday > 0 ? (double)errorLogsToday / totalLogsToday * 100 : 0;
        var errorRateYesterday = logsYesterday > 0 ? (double)errorsYesterday / logsYesterday * 100 : 0;
        var errorTrend = errorRateYesterday > 0 ? ((errorRate - errorRateYesterday) / errorRateYesterday) * 100 : 0;

        // Calculate logs per second (last 5 minutes)
        var last5Minutes = DateTime.UtcNow.AddMinutes(-5);
        var logsLast5Min = await _unitOfWork.LogEntries.CountAsync(
            l => l.TenantId == tenant && l.Timestamp >= last5Minutes, cancellationToken);
        var logsPerSecond = logsLast5Min / 300; // 5 minutes = 300 seconds

        return new DashboardMetrics
        {
            TotalLogsToday = totalLogsToday,
            TotalLogsThisWeek = totalLogsThisWeek,
            TotalLogsThisMonth = totalLogsThisMonth,
            ErrorLogsToday = errorLogsToday,
            WarningLogsToday = warningLogsToday,
            ErrorRate = errorRate,
            StorageUsedMB = storageUsedBytes / 1024 / 1024,
            StorageUsagePercentage = tenantEntity?.MaxLogSizeBytes > 0 ? (double)storageUsedBytes / tenantEntity.MaxLogSizeBytes * 100 : 0,
            LastLogReceived = recentLogs.FirstOrDefault()?.Timestamp ?? DateTime.MinValue,
            LogsTrendPercentage = logsTrend,
            ErrorTrendPercentage = errorTrend,
            LogsPerSecond = logsPerSecond
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
                Status = g.Any(l => l.Timestamp >= DateTime.UtcNow.AddMinutes(-5)) ? "Healthy" : "Inactive",
                AverageResponseTime = 100 + new Random().Next(0, 500) // Simulated response time
            })
            .OrderByDescending(a => a.LogCount)
            .Take(count)
            .ToList();

        return grouped;
    }

    public async Task<IList<SystemHealthStatus>> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        // In a real implementation, you would check actual system health
        // For now, return simulated health status
        var services = new[]
        {
            new SystemHealthStatus { ServiceName = "API", Status = "Healthy", ResponseTime = 45, LastCheck = DateTime.UtcNow },
            new SystemHealthStatus { ServiceName = "Database", Status = "Healthy", ResponseTime = 12, LastCheck = DateTime.UtcNow },
            new SystemHealthStatus { ServiceName = "Redis", Status = "Healthy", ResponseTime = 2, LastCheck = DateTime.UtcNow },
            new SystemHealthStatus { ServiceName = "Elasticsearch", Status = "Healthy", ResponseTime = 25, LastCheck = DateTime.UtcNow },
            new SystemHealthStatus { ServiceName = "Functions", Status = "Degraded", ResponseTime = 150, LastCheck = DateTime.UtcNow },
            new SystemHealthStatus { ServiceName = "Storage", Status = "Healthy", ResponseTime = 35, LastCheck = DateTime.UtcNow }
        };

        return await Task.FromResult(services.ToList());
    }

    public async Task<IList<TopErrorMessage>> GetTopErrorsAsync(string tenantId, int count = 10, CancellationToken cancellationToken = default)
    {
        var tenant = new TenantId(tenantId);
        var yesterday = DateTime.UtcNow.AddDays(-1);
        
        var errorLogs = await _unitOfWork.LogEntries.FindAsync(
            l => l.TenantId == tenant && 
                 l.Timestamp >= yesterday &&
                 (l.Level.Value == "ERROR" || l.Level.Value == "FATAL"), cancellationToken);

        var grouped = errorLogs
            .GroupBy(l => l.Message.Content.Length > 100 ? l.Message.Content[..100] : l.Message.Content)
            .Select(g => new TopErrorMessage
            {
                Message = g.Key,
                Count = g.Count(),
                LastOccurrence = g.Max(l => l.Timestamp),
                Application = g.First().Source.Application,
                Environment = g.First().Source.Environment
            })
            .OrderByDescending(e => e.Count)
            .Take(count)
            .ToList();

        return grouped;
    }

    private async Task<IList<LogLevelDistribution>> GetLogLevelDistributionDataAsync(string tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var tenant = new TenantId(tenantId);
        var logs = await _unitOfWork.LogEntries.FindAsync(
            l => l.TenantId == tenant && l.Timestamp >= startDate && l.Timestamp <= endDate, cancellationToken);

        var grouped = logs.GroupBy(l => l.Level.Value)
            .Select(g => new LogLevelDistribution
            {
                Level = g.Key,
                Count = g.Count(),
                Percentage = 0 // Will be calculated below
            })
            .ToList();

        var total = grouped.Sum(x => x.Count);
        foreach (var item in grouped)
        {
            item.Percentage = total > 0 ? (double)item.Count / total * 100 : 0;
        }

        return grouped.OrderByDescending(x => x.Count).ToList();
    }

    private async Task<IList<LogVolumeTrend>> GetLogVolumeTrendsAsync(string tenantId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var tenant = new TenantId(tenantId);
        var logs = await _unitOfWork.LogEntries.FindAsync(
            l => l.TenantId == tenant && l.Timestamp >= startDate && l.Timestamp <= endDate, cancellationToken);

        // Group by hour
        var grouped = logs.GroupBy(l => new DateTime(l.Timestamp.Year, l.Timestamp.Month, l.Timestamp.Day, l.Timestamp.Hour, 0, 0))
            .Select(g => new LogVolumeTrend
            {
                Timestamp = g.Key,
                TotalLogs = g.Count(),
                ErrorLogs = g.Count(l => l.Level.Value == "ERROR" || l.Level.Value == "FATAL"),
                WarningLogs = g.Count(l => l.Level.Value == "WARN"),
                InfoLogs = g.Count(l => l.Level.Value == "INFO")
            })
            .OrderBy(t => t.Timestamp)
            .ToList();

        return grouped;
    }

    private static (DateTime startDate, DateTime endDate) ParseTimeRange(string timeRange)
    {
        var now = DateTime.UtcNow;
        return timeRange.ToLower() switch
        {
            "1h" => (now.AddHours(-1), now),
            "6h" => (now.AddHours(-6), now),
            "24h" => (now.AddHours(-24), now),
            "7d" => (now.AddDays(-7), now),
            "30d" => (now.AddDays(-30), now),
            _ => (now.AddHours(-24), now)
        };
    }
}