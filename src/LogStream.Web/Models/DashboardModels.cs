namespace LogStream.Web.Models;

public class DashboardViewModel
{
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public DashboardMetrics Metrics { get; set; } = new();
    public IList<LogLevel> TopLogLevels { get; set; } = new List<LogLevel>();
    public IList<Application> TopApplications { get; set; } = new List<Application>();
    public IList<RecentLogEntry> RecentLogs { get; set; } = new List<RecentLogEntry>();
    public IList<Alert> ActiveAlerts { get; set; } = new List<Alert>();
}

public class DashboardMetrics
{
    public long TotalLogsToday { get; set; }
    public long TotalLogsThisWeek { get; set; }
    public long TotalLogsThisMonth { get; set; }
    public long ErrorLogsToday { get; set; }
    public long WarningLogsToday { get; set; }
    public double ErrorRate { get; set; }
    public long StorageUsedMB { get; set; }
    public double StorageUsagePercentage { get; set; }
    public int ActiveApplications { get; set; }
    public int ActiveEnvironments { get; set; }
    public DateTime LastLogReceived { get; set; }
}

public class LogLevel
{
    public string Name { get; set; } = string.Empty;
    public long Count { get; set; }
    public double Percentage { get; set; }
    public string Color { get; set; } = string.Empty;
}

public class Application
{
    public string Name { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public long LogCount { get; set; }
    public long ErrorCount { get; set; }
    public DateTime LastSeen { get; set; }
    public string Status { get; set; } = "Active";
}

public class RecentLogEntry
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Application { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public bool HasException => !string.IsNullOrEmpty(Exception);
}

public class Alert
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsAcknowledged { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
}

public class SearchFilters
{
    public string? Query { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Level { get; set; }
    public string? Application { get; set; }
    public string? Environment { get; set; }
    public string? Server { get; set; }
    public string[]? Tags { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 100;
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Color { get; set; } = string.Empty;
}

public class TimeSeriesDataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public string Series { get; set; } = string.Empty;
}