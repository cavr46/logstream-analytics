using LogStream.Contracts.DTOs;

namespace LogStream.Application.Queries.Tenants;

public record GetTenantQuery(string TenantId) : IRequest<Result<TenantDto>>;

public class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, Result<TenantDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetTenantQueryHandler> _logger;

    public GetTenantQueryHandler(IUnitOfWork unitOfWork, ILogger<GetTenantQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<TenantDto>> Handle(GetTenantQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = new TenantId(request.TenantId);
            var tenant = await _unitOfWork.Tenants.GetByTenantIdAsync(tenantId, cancellationToken);

            if (tenant == null)
            {
                return Result<TenantDto>.Failure($"Tenant '{request.TenantId}' not found");
            }

            var tenantDto = tenant.Adapt<TenantDto>();
            return Result<TenantDto>.Success(tenantDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tenant {TenantId}", request.TenantId);
            return Result<TenantDto>.Failure("Failed to get tenant");
        }
    }
}