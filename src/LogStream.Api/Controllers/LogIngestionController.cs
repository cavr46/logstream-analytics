using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using LogStream.Contracts.DTOs;
using LogStream.Application.Commands.LogEntries;
using MediatR;
using FluentValidation;
using System.Text.Json;

namespace LogStream.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[EnableRateLimiting("IngestionPolicy")]
public class LogIngestionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LogIngestionController> _logger;
    private readonly IValidator<LogEntryDto> _validator;

    public LogIngestionController(
        IMediator mediator,
        ILogger<LogIngestionController> logger,
        IValidator<LogEntryDto> validator)
    {
        _mediator = mediator;
        _logger = logger;
        _validator = validator;
    }

    /// <summary>
    /// Ingest a single log entry
    /// </summary>
    /// <param name="logEntry">The log entry to ingest</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>Result of the ingestion</returns>
    [HttpPost("single")]
    [ProducesResponseType(typeof(LogIngestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> IngestSingleLog(
        [FromBody] LogEntryDto logEntry,
        [FromHeader(Name = "X-Tenant-ID")] string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(new { Error = "X-Tenant-ID header is required" });
        }

        // Validate the log entry
        var validationResult = await _validator.ValidateAsync(logEntry, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.ToDictionary(e => e.PropertyName, e => e.ErrorMessage);
            return BadRequest(new ValidationProblemDetails(errors));
        }

        try
        {
            var command = new CreateLogEntryCommand
            {
                TenantId = tenantId,
                LogEntry = logEntry,
                IngestionTimestamp = DateTime.UtcNow,
                Source = GetIngestionSource()
            };

            var result = await _mediator.Send(command, cancellationToken);

            var response = new LogIngestionResponse
            {
                Success = true,
                LogId = result.LogId,
                IngestionTimestamp = result.IngestionTimestamp,
                ProcessingTimeMs = result.ProcessingTimeMs,
                Message = "Log entry ingested successfully"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting single log for tenant {TenantId}", tenantId);
            return StatusCode(500, new { Error = "Internal server error during log ingestion" });
        }
    }

    /// <summary>
    /// Ingest multiple log entries in a single request
    /// </summary>
    /// <param name="request">Bulk ingestion request</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <returns>Result of the bulk ingestion</returns>
    [HttpPost("bulk")]
    [RequestSizeLimit(100_000_000)] // 100MB limit
    [ProducesResponseType(typeof(BulkLogIngestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> IngestBulkLogs(
        [FromBody] BulkLogIngestionRequest request,
        [FromHeader(Name = "X-Tenant-ID")] string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(new { Error = "X-Tenant-ID header is required" });
        }

        if (request.LogEntries?.Any() != true)
        {
            return BadRequest(new { Error = "LogEntries array cannot be empty" });
        }

        if (request.LogEntries.Count > 10000)
        {
            return BadRequest(new { Error = "Maximum 10,000 log entries per bulk request" });
        }

        try
        {
            var command = new BulkCreateLogEntriesCommand
            {
                TenantId = tenantId,
                LogEntries = request.LogEntries.ToList(),
                IngestionTimestamp = DateTime.UtcNow,
                Source = GetIngestionSource(),
                BatchId = Guid.NewGuid()
            };

            var result = await _mediator.Send(command, cancellationToken);

            var response = new BulkLogIngestionResponse
            {
                Success = true,
                BatchId = result.BatchId,
                ProcessedCount = result.ProcessedCount,
                FailedCount = result.FailedCount,
                IngestionTimestamp = result.IngestionTimestamp,
                ProcessingTimeMs = result.ProcessingTimeMs,
                Message = $"Processed {result.ProcessedCount} logs successfully, {result.FailedCount} failed",
                FailedEntries = result.FailedEntries.Select(f => new FailedLogEntry
                {
                    Index = f.Index,
                    Error = f.Error,
                    LogEntry = f.LogEntry
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting bulk logs for tenant {TenantId}", tenantId);
            return StatusCode(500, new { Error = "Internal server error during bulk ingestion" });
        }
    }

    /// <summary>
    /// Ingest logs from file upload
    /// </summary>
    /// <param name="file">Log file to upload</param>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="format">File format (json, csv, txt, etc.)</param>
    /// <returns>Result of the file ingestion</returns>
    [HttpPost("file")]
    [RequestSizeLimit(1_000_000_000)] // 1GB limit
    [ProducesResponseType(typeof(FileIngestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> IngestLogFile(
        IFormFile file,
        [FromHeader(Name = "X-Tenant-ID")] string tenantId,
        [FromQuery] string format = "auto",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            return BadRequest(new { Error = "X-Tenant-ID header is required" });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { Error = "File is required" });
        }

        var allowedFormats = new[] { "json", "csv", "txt", "log", "auto" };
        if (!allowedFormats.Contains(format.ToLower()))
        {
            return BadRequest(new { Error = $"Unsupported format. Allowed formats: {string.Join(", ", allowedFormats)}" });
        }

        try
        {
            var command = new ProcessLogFileCommand
            {
                TenantId = tenantId,
                FileName = file.FileName,
                FileContent = file.OpenReadStream(),
                ContentType = file.ContentType,
                Format = format,
                IngestionTimestamp = DateTime.UtcNow,
                Source = GetIngestionSource()
            };

            var result = await _mediator.Send(command, cancellationToken);

            var response = new FileIngestionResponse
            {
                Success = true,
                JobId = result.JobId,
                FileName = file.FileName,
                FileSizeBytes = file.Length,
                EstimatedProcessingTimeMs = result.EstimatedProcessingTimeMs,
                Message = "File queued for processing",
                StatusUrl = Url.Action(nameof(GetIngestionStatus), new { jobId = result.JobId })
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ingesting log file for tenant {TenantId}", tenantId);
            return StatusCode(500, new { Error = "Internal server error during file ingestion" });
        }
    }

    /// <summary>
    /// Get the status of a file ingestion job
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <returns>Status of the ingestion job</returns>
    [HttpGet("status/{jobId}")]
    [ProducesResponseType(typeof(IngestionJobStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIngestionStatus(
        [FromRoute] Guid jobId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetIngestionJobStatusQuery { JobId = jobId };
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound(new { Error = "Job not found" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ingestion status for job {JobId}", jobId);
            return StatusCode(500, new { Error = "Internal server error" });
        }
    }

    /// <summary>
    /// Health check endpoint for monitoring
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    public IActionResult HealthCheck()
    {
        return Ok(new HealthCheckResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }

    private string GetIngestionSource()
    {
        return $"API-{Request.Headers.UserAgent.FirstOrDefault() ?? "Unknown"}";
    }
}

// DTOs for API responses
public class LogIngestionResponse
{
    public bool Success { get; set; }
    public Guid LogId { get; set; }
    public DateTime IngestionTimestamp { get; set; }
    public long ProcessingTimeMs { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BulkLogIngestionRequest
{
    [Required]
    public List<LogEntryDto> LogEntries { get; set; } = new();
}

public class BulkLogIngestionResponse
{
    public bool Success { get; set; }
    public Guid BatchId { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime IngestionTimestamp { get; set; }
    public long ProcessingTimeMs { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<FailedLogEntry> FailedEntries { get; set; } = new();
}

public class FailedLogEntry
{
    public int Index { get; set; }
    public string Error { get; set; } = string.Empty;
    public LogEntryDto LogEntry { get; set; } = new();
}

public class FileIngestionResponse
{
    public bool Success { get; set; }
    public Guid JobId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public long EstimatedProcessingTimeMs { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? StatusUrl { get; set; }
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

public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
}

// Commands and Queries (will be implemented in Application layer)
public class ProcessLogFileCommand : IRequest<ProcessLogFileResult>
{
    public string TenantId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public Stream FileContent { get; set; } = Stream.Null;
    public string ContentType { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public DateTime IngestionTimestamp { get; set; }
    public string Source { get; set; } = string.Empty;
}

public class ProcessLogFileResult
{
    public Guid JobId { get; set; }
    public long EstimatedProcessingTimeMs { get; set; }
}

public class GetIngestionJobStatusQuery : IRequest<IngestionJobStatus>
{
    public Guid JobId { get; set; }
}