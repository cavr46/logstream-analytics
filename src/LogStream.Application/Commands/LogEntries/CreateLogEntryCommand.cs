using LogStream.Contracts.DTOs;

namespace LogStream.Application.Commands.LogEntries;

public record CreateLogEntryCommand(string TenantId, CreateLogEntryRequest Request, string CreatedBy = "system") : IRequest<Result<LogEntryDto>>;

public class CreateLogEntryCommandHandler : IRequestHandler<CreateLogEntryCommand, Result<LogEntryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateLogEntryCommandHandler> _logger;

    public CreateLogEntryCommandHandler(IUnitOfWork unitOfWork, ILogger<CreateLogEntryCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<LogEntryDto>> Handle(CreateLogEntryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = new TenantId(request.TenantId);

            // Verify tenant exists and is active
            var tenant = await _unitOfWork.Tenants.GetByTenantIdAsync(tenantId, cancellationToken);
            if (tenant == null)
            {
                return Result<LogEntryDto>.Failure($"Tenant '{request.TenantId}' not found");
            }

            if (!tenant.CanIngestLogs())
            {
                return Result<LogEntryDto>.Failure($"Tenant '{request.TenantId}' cannot ingest logs");
            }

            // Create log entry
            var timestamp = request.Request.Timestamp ?? DateTime.UtcNow;
            var level = new LogLevel(request.Request.Level);
            var message = new LogMessage(
                request.Request.Message,
                request.Request.MessageTemplate,
                request.Request.Metadata);
            var source = new LogSource(
                request.Request.Source.Application,
                request.Request.Source.Environment,
                request.Request.Source.Server,
                request.Request.Source.Component);

            var logEntry = new LogEntry(
                tenantId,
                timestamp,
                level,
                message,
                source,
                request.Request.OriginalFormat,
                request.Request.RawContent ?? request.Request.Message,
                request.Request.TraceId,
                request.Request.SpanId,
                request.Request.UserId,
                request.Request.SessionId,
                request.Request.CorrelationId,
                request.Request.Exception,
                request.Request.Metadata,
                request.Request.Tags,
                request.Request.IpAddress,
                request.Request.UserAgent);

            _unitOfWork.LogEntries.Add(logEntry);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var logEntryDto = logEntry.Adapt<LogEntryDto>();
            return Result<LogEntryDto>.Success(logEntryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating log entry for tenant {TenantId}", request.TenantId);
            return Result<LogEntryDto>.Failure("Failed to create log entry");
        }
    }
}