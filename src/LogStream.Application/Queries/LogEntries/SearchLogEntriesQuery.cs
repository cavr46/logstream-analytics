using LogStream.Contracts.DTOs;

namespace LogStream.Application.Queries.LogEntries;

public record SearchLogEntriesQuery(string TenantId, LogSearchRequest Request) : IRequest<Result<PagedResult<LogEntryDto>>>;

public class SearchLogEntriesQueryHandler : IRequestHandler<SearchLogEntriesQuery, Result<PagedResult<LogEntryDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SearchLogEntriesQueryHandler> _logger;

    public SearchLogEntriesQueryHandler(IUnitOfWork unitOfWork, ILogger<SearchLogEntriesQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PagedResult<LogEntryDto>>> Handle(SearchLogEntriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = new TenantId(request.TenantId);

            // Verify tenant exists
            var tenant = await _unitOfWork.Tenants.GetByTenantIdAsync(tenantId, cancellationToken);
            if (tenant == null)
            {
                return Result<PagedResult<LogEntryDto>>.Failure($"Tenant '{request.TenantId}' not found");
            }

            LogLevel? level = null;
            if (!string.IsNullOrWhiteSpace(request.Request.Level))
            {
                level = new LogLevel(request.Request.Level);
            }

            var pagedResult = await _unitOfWork.LogEntries.GetLogsByTenantAsync(
                tenantId,
                request.Request.StartDate,
                request.Request.EndDate,
                level,
                request.Request.Query,
                request.Request.Page,
                request.Request.Size,
                cancellationToken);

            var logDtos = pagedResult.Items.Select(log => log.Adapt<LogEntryDto>()).ToList();
            var result = PagedResult<LogEntryDto>.Create(logDtos, pagedResult.TotalCount, pagedResult.PageNumber, pagedResult.PageSize);

            return Result<PagedResult<LogEntryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching log entries for tenant {TenantId}", request.TenantId);
            return Result<PagedResult<LogEntryDto>>.Failure("Failed to search log entries");
        }
    }
}