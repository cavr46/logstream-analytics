using LogStream.Contracts.DTOs;

namespace LogStream.Application.Commands.LogEntries;

public record BulkCreateLogEntriesCommand(string TenantId, BulkLogIngestRequest Request, string CreatedBy = "system") : IRequest<Result<BulkCreateLogEntriesResponse>>;

public record BulkCreateLogEntriesResponse
{
    public int TotalRequested { get; init; }
    public int Successful { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<Guid> CreatedIds { get; init; } = Array.Empty<Guid>();
}

public class BulkCreateLogEntriesCommandHandler : IRequestHandler<BulkCreateLogEntriesCommand, Result<BulkCreateLogEntriesResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BulkCreateLogEntriesCommandHandler> _logger;

    public BulkCreateLogEntriesCommandHandler(IUnitOfWork unitOfWork, ILogger<BulkCreateLogEntriesCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<BulkCreateLogEntriesResponse>> Handle(BulkCreateLogEntriesCommand request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var createdIds = new List<Guid>();
        var tenantId = new TenantId(request.TenantId);

        try
        {
            // Verify tenant exists and is active
            var tenant = await _unitOfWork.Tenants.GetByTenantIdAsync(tenantId, cancellationToken);
            if (tenant == null)
            {
                return Result<BulkCreateLogEntriesResponse>.Failure($"Tenant '{request.TenantId}' not found");
            }

            if (!tenant.CanIngestLogs())
            {
                return Result<BulkCreateLogEntriesResponse>.Failure($"Tenant '{request.TenantId}' cannot ingest logs");
            }

            var logEntries = new List<LogEntry>();

            foreach (var (logRequest, index) in request.Request.LogEntries.Select((item, index) => (item, index)))
            {
                try
                {
                    var timestamp = logRequest.Timestamp ?? DateTime.UtcNow;
                    var level = new LogLevel(logRequest.Level);
                    var message = new LogMessage(
                        logRequest.Message,
                        logRequest.MessageTemplate,
                        logRequest.Metadata);
                    var source = new LogSource(
                        logRequest.Source.Application,
                        logRequest.Source.Environment,
                        logRequest.Source.Server,
                        logRequest.Source.Component);

                    var logEntry = new LogEntry(
                        tenantId,
                        timestamp,
                        level,
                        message,
                        source,
                        logRequest.OriginalFormat,
                        logRequest.RawContent ?? logRequest.Message,
                        logRequest.TraceId,
                        logRequest.SpanId,
                        logRequest.UserId,
                        logRequest.SessionId,
                        logRequest.CorrelationId,
                        logRequest.Exception,
                        logRequest.Metadata,
                        logRequest.Tags,
                        logRequest.IpAddress,
                        logRequest.UserAgent);

                    logEntries.Add(logEntry);
                    createdIds.Add(logEntry.Id);
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing log entry at index {index}: {ex.Message}");
                    _logger.LogWarning(ex, "Error processing log entry at index {Index} for tenant {TenantId}", index, request.TenantId);
                }
            }

            if (logEntries.Any())
            {
                await _unitOfWork.LogEntries.BulkInsertAsync(logEntries, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Bulk created {Count} log entries for tenant {TenantId}", logEntries.Count, request.TenantId);
            }

            var response = new BulkCreateLogEntriesResponse
            {
                TotalRequested = request.Request.LogEntries.Count,
                Successful = logEntries.Count,
                Failed = errors.Count,
                Errors = errors,
                CreatedIds = createdIds
            };

            return Result<BulkCreateLogEntriesResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating log entries for tenant {TenantId}", request.TenantId);
            return Result<BulkCreateLogEntriesResponse>.Failure("Failed to bulk create log entries");
        }
    }
}