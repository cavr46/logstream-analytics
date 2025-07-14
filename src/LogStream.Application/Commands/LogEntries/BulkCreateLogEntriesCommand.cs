using LogStream.Contracts.DTOs;
using MediatR;

namespace LogStream.Application.Commands.LogEntries;

public class BulkCreateLogEntriesCommand : IRequest<BulkCreateLogEntriesResult>
{
    public string TenantId { get; set; } = string.Empty;
    public List<LogEntryDto> LogEntries { get; set; } = new();
    public DateTime IngestionTimestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public Guid BatchId { get; set; }
}

public class BulkCreateLogEntriesResult
{
    public Guid BatchId { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime IngestionTimestamp { get; set; }
    public long ProcessingTimeMs { get; set; }
    public List<BulkLogEntryFailure> FailedEntries { get; set; } = new();
}

public class BulkLogEntryFailure
{
    public int Index { get; set; }
    public string Error { get; set; } = string.Empty;
    public LogEntryDto LogEntry { get; set; } = new();
}

public class BulkCreateLogEntriesCommandHandler : IRequestHandler<BulkCreateLogEntriesCommand, BulkCreateLogEntriesResult>
{
    private readonly ILogger<BulkCreateLogEntriesCommandHandler> _logger;
    private readonly IMediator _mediator;

    public BulkCreateLogEntriesCommandHandler(
        ILogger<BulkCreateLogEntriesCommandHandler> logger,
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<BulkCreateLogEntriesResult> Handle(BulkCreateLogEntriesCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Processing bulk log entries for tenant {TenantId}, count: {Count}", 
                request.TenantId, request.LogEntries.Count);
            
            var processedCount = 0;
            var failedEntries = new List<BulkLogEntryFailure>();
            
            // Process each log entry
            for (int i = 0; i < request.LogEntries.Count; i++)
            {
                try
                {
                    var logEntry = request.LogEntries[i];
                    
                    // Simulate processing - in real implementation this would:
                    // 1. Validate each entry
                    // 2. Save to database in batches
                    // 3. Index in Elasticsearch
                    // 4. Handle failures gracefully
                    
                    // Simulate some failures (5% failure rate)
                    if (Random.Shared.Next(1, 100) <= 5)
                    {
                        failedEntries.Add(new BulkLogEntryFailure
                        {
                            Index = i,
                            Error = "Simulated validation error",
                            LogEntry = logEntry
                        });
                        continue;
                    }
                    
                    processedCount++;
                    
                    // Add small delay to simulate processing
                    if (i % 100 == 0)
                    {
                        await Task.Delay(1, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process log entry at index {Index}", i);
                    failedEntries.Add(new BulkLogEntryFailure
                    {
                        Index = i,
                        Error = ex.Message,
                        LogEntry = request.LogEntries[i]
                    });
                }
            }
            
            stopwatch.Stop();
            
            return new BulkCreateLogEntriesResult
            {
                BatchId = request.BatchId,
                ProcessedCount = processedCount,
                FailedCount = failedEntries.Count,
                IngestionTimestamp = request.IngestionTimestamp,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                FailedEntries = failedEntries
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk log entries for tenant {TenantId}", request.TenantId);
            throw;
        }
    }
}