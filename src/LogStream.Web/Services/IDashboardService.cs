using LogStream.Web.Models;

namespace LogStream.Web.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardDataAsync(string tenantId, string timeRange = "24h", CancellationToken cancellationToken = default);
    Task<DashboardMetrics> GetMetricsAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<IList<RecentLogEntry>> GetRecentLogsAsync(string tenantId, int count = 50, CancellationToken cancellationToken = default);
    Task<IList<Alert>> GetActiveAlertsAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<IList<ChartDataPoint>> GetLogLevelDistributionAsync(string tenantId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
    Task<IList<TimeSeriesDataPoint>> GetLogVolumeTimeSeriesAsync(string tenantId, DateTime startDate, DateTime endDate, string granularity = "hour", CancellationToken cancellationToken = default);
    Task<IList<Application>> GetTopApplicationsAsync(string tenantId, int count = 10, CancellationToken cancellationToken = default);
    Task<IList<SystemHealthStatus>> GetSystemHealthAsync(CancellationToken cancellationToken = default);
    Task<IList<TopErrorMessage>> GetTopErrorsAsync(string tenantId, int count = 10, CancellationToken cancellationToken = default);
}