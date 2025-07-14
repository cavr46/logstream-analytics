using LogStream.Contracts.DTOs;
using MediatR;

namespace LogStream.Application.Commands.LogEntries;

public class CreateLogEntryCommand : IRequest<CreateLogEntryResult>
{
    public string TenantId { get; set; } = string.Empty;
    public LogEntryDto LogEntry { get; set; } = new();
    public DateTime IngestionTimestamp { get; set; }
    public string Source { get; set; } = string.Empty;
}

public class CreateLogEntryResult
{
    public Guid LogId { get; set; }
    public DateTime IngestionTimestamp { get; set; }
    public long ProcessingTimeMs { get; set; }
}

public class CreateLogEntryCommandHandler : IRequestHandler<CreateLogEntryCommand, CreateLogEntryResult>
{
    private readonly ILogger<CreateLogEntryCommandHandler> _logger;

    public CreateLogEntryCommandHandler(ILogger<CreateLogEntryCommandHandler> logger)
    {
        _logger = logger;
    }

    public async Task<CreateLogEntryResult> Handle(CreateLogEntryCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Processing log entry for tenant {TenantId}", request.TenantId);
            
            // Simulate processing time
            await Task.Delay(Random.Shared.Next(10, 100), cancellationToken);
            
            var logId = Guid.NewGuid();
            
            // In real implementation, this would:
            // 1. Validate tenant exists and is active
            // 2. Create domain entity
            // 3. Save to database
            // 4. Index in Elasticsearch
            // 5. Publish domain events
            
            stopwatch.Stop();
            
            return new CreateLogEntryResult
            {
                LogId = logId,
                IngestionTimestamp = request.IngestionTimestamp,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating log entry for tenant {TenantId}", request.TenantId);
            throw;
        }
    }
}