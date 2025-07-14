using MediatR;

namespace LogStream.Application.Queries.LogEntries;

public class GetIngestionJobStatusQuery : IRequest<IngestionJobStatus?>
{
    public Guid JobId { get; set; }
}

public class IngestionJobStatus
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Processing, Completed, Failed
    public int ProcessedCount { get; set; }
    public int TotalCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? CompletedTime { get; set; }
    public string? Error { get; set; }
    public double ProgressPercentage => TotalCount > 0 ? (double)ProcessedCount / TotalCount * 100 : 0;
}

public class GetIngestionJobStatusQueryHandler : IRequestHandler<GetIngestionJobStatusQuery, IngestionJobStatus?>
{
    private readonly ILogger<GetIngestionJobStatusQueryHandler> _logger;
    
    // In-memory storage for demo purposes - in real implementation this would be:
    // - Database table for job status
    // - Redis cache for fast lookup
    // - External job queue service (Azure Service Bus, etc.)
    private static readonly Dictionary<Guid, IngestionJobStatus> _jobStatuses = new();
    private static readonly object _lock = new();
    
    public GetIngestionJobStatusQueryHandler(ILogger<GetIngestionJobStatusQueryHandler> logger)
    {
        _logger = logger;
    }

    public async Task<IngestionJobStatus?> Handle(GetIngestionJobStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(10, cancellationToken); // Simulate database lookup
            
            lock (_lock)
            {
                if (_jobStatuses.TryGetValue(request.JobId, out var status))
                {
                    return status;
                }
                
                // Create a mock job status for demo purposes
                var mockStatus = new IngestionJobStatus
                {
                    JobId = request.JobId,
                    Status = GetRandomStatus(),
                    ProcessedCount = Random.Shared.Next(0, 1000),
                    TotalCount = Random.Shared.Next(100, 1000),
                    FailedCount = Random.Shared.Next(0, 50),
                    StartTime = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 30)),
                    CompletedTime = Random.Shared.Next(0, 100) > 50 ? DateTime.UtcNow.AddMinutes(-Random.Shared.Next(0, 10)) : null,
                    Error = Random.Shared.Next(0, 100) > 90 ? "Simulated processing error" : null
                };
                
                _jobStatuses[request.JobId] = mockStatus;
                return mockStatus;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ingestion job status for job {JobId}", request.JobId);
            throw;
        }
    }
    
    private static string GetRandomStatus()
    {
        var statuses = new[] { "Pending", "Processing", "Completed", "Failed" };
        return statuses[Random.Shared.Next(statuses.Length)];
    }
    
    // Helper method to simulate job progression (called by background service)
    public static void UpdateJobStatus(Guid jobId, IngestionJobStatus status)
    {
        lock (_lock)
        {
            _jobStatuses[jobId] = status;
        }
    }
}