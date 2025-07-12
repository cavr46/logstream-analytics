using Grpc.Core;
using LogStream.Grpc.Protos;
using LogStream.Application.Commands.LogEntries;
using LogStream.Contracts.DTOs;
using MediatR;

namespace LogStream.Grpc.Services;

public class LogIngestionService : Protos.LogIngestion.LogIngestionBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LogIngestionService> _logger;

    public LogIngestionService(IMediator mediator, ILogger<LogIngestionService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task<IngestLogResponse> IngestLog(IngestLogRequest request, ServerCallContext context)
    {
        try
        {
            var createRequest = MapToCreateLogEntryRequest(request);
            var command = new CreateLogEntryCommand(request.TenantId, createRequest, "grpc");
            var result = await _mediator.Send(command, context.CancellationToken);

            if (result.IsSuccess)
            {
                return new IngestLogResponse
                {
                    Success = true,
                    Message = "Log entry created successfully",
                    LogEntryId = result.Data!.Id.ToString()
                };
            }
            else
            {
                return new IngestLogResponse
                {
                    Success = false,
                    Message = result.ErrorMessage ?? "Failed to create log entry"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting log via gRPC for tenant {TenantId}", request.TenantId);
            return new IngestLogResponse
            {
                Success = false,
                Message = "Internal server error"
            };
        }
    }

    public override async Task<IngestLogsBatchResponse> IngestLogsBatch(IngestLogsBatchRequest request, ServerCallContext context)
    {
        try
        {
            if (!request.LogEntries.Any())
            {
                return new IngestLogsBatchResponse
                {
                    Success = false,
                    Message = "No log entries provided"
                };
            }

            var tenantId = request.LogEntries.First().TenantId;
            var createRequests = request.LogEntries.Select(MapToCreateLogEntryRequest).ToList();
            var bulkRequest = new BulkLogIngestRequest { LogEntries = createRequests };
            
            var command = new BulkCreateLogEntriesCommand(tenantId, bulkRequest, "grpc");
            var result = await _mediator.Send(command, context.CancellationToken);

            if (result.IsSuccess)
            {
                var response = new IngestLogsBatchResponse
                {
                    Success = true,
                    Message = "Batch processing completed",
                    TotalRequested = result.Data!.TotalRequested,
                    Successful = result.Data!.Successful,
                    Failed = result.Data!.Failed
                };

                response.Errors.AddRange(result.Data!.Errors);
                response.CreatedIds.AddRange(result.Data!.CreatedIds.Select(id => id.ToString()));

                return response;
            }
            else
            {
                return new IngestLogsBatchResponse
                {
                    Success = false,
                    Message = result.ErrorMessage ?? "Failed to process batch"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting batch logs via gRPC");
            return new IngestLogsBatchResponse
            {
                Success = false,
                Message = "Internal server error"
            };
        }
    }

    public override async Task<IngestLogsStreamResponse> IngestLogsStream(IAsyncStreamReader<IngestLogRequest> requestStream, ServerCallContext context)
    {
        var successful = 0;
        var failed = 0;
        var totalProcessed = 0;
        var errors = new List<string>();

        try
        {
            await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
            {
                totalProcessed++;

                try
                {
                    var createRequest = MapToCreateLogEntryRequest(request);
                    var command = new CreateLogEntryCommand(request.TenantId, createRequest, "grpc-stream");
                    var result = await _mediator.Send(command, context.CancellationToken);

                    if (result.IsSuccess)
                    {
                        successful++;
                    }
                    else
                    {
                        failed++;
                        errors.Add($"Entry {totalProcessed}: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"Entry {totalProcessed}: {ex.Message}");
                    _logger.LogWarning(ex, "Error processing stream entry {Index}", totalProcessed);
                }
            }

            return new IngestLogsStreamResponse
            {
                Success = true,
                Message = "Stream processing completed",
                TotalProcessed = totalProcessed,
                Successful = successful,
                Failed = failed,
                Errors = { errors }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing log stream via gRPC");
            return new IngestLogsStreamResponse
            {
                Success = false,
                Message = "Internal server error",
                TotalProcessed = totalProcessed,
                Successful = successful,
                Failed = failed,
                Errors = { errors }
            };
        }
    }

    private static CreateLogEntryRequest MapToCreateLogEntryRequest(IngestLogRequest request)
    {
        DateTime? timestamp = null;
        if (!string.IsNullOrEmpty(request.Timestamp) && DateTime.TryParse(request.Timestamp, out var parsedTimestamp))
        {
            timestamp = parsedTimestamp;
        }

        return new CreateLogEntryRequest
        {
            Timestamp = timestamp,
            Level = request.Level,
            Message = request.Message,
            MessageTemplate = string.IsNullOrEmpty(request.MessageTemplate) ? null : request.MessageTemplate,
            Source = new LogSourceDto
            {
                Application = request.Source?.Application ?? "unknown",
                Environment = request.Source?.Environment ?? "unknown",
                Server = request.Source?.Server,
                Component = request.Source?.Component
            },
            TraceId = string.IsNullOrEmpty(request.TraceId) ? null : request.TraceId,
            SpanId = string.IsNullOrEmpty(request.SpanId) ? null : request.SpanId,
            UserId = string.IsNullOrEmpty(request.UserId) ? null : request.UserId,
            SessionId = string.IsNullOrEmpty(request.SessionId) ? null : request.SessionId,
            CorrelationId = string.IsNullOrEmpty(request.CorrelationId) ? null : request.CorrelationId,
            Exception = string.IsNullOrEmpty(request.Exception) ? null : request.Exception,
            Metadata = request.Metadata?.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value),
            Tags = string.IsNullOrEmpty(request.Tags) ? null : request.Tags,
            OriginalFormat = string.IsNullOrEmpty(request.OriginalFormat) ? "GRPC" : request.OriginalFormat,
            RawContent = string.IsNullOrEmpty(request.RawContent) ? request.Message : request.RawContent,
            IpAddress = string.IsNullOrEmpty(request.IpAddress) ? null : request.IpAddress,
            UserAgent = string.IsNullOrEmpty(request.UserAgent) ? null : request.UserAgent
        };
    }
}