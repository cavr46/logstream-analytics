using LogStream.Contracts.DTOs;

namespace LogStream.Application.Commands.Tenants;

public record CreateTenantCommand(CreateTenantRequest Request, string CreatedBy) : IRequest<Result<TenantDto>>;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<TenantDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTenantCommandHandler> _logger;

    public CreateTenantCommandHandler(IUnitOfWork unitOfWork, ILogger<CreateTenantCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<TenantDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = new TenantId(request.Request.TenantId);

            // Check if tenant already exists
            var existingTenant = await _unitOfWork.Tenants.GetByTenantIdAsync(tenantId, cancellationToken);
            if (existingTenant != null)
            {
                return Result<TenantDto>.Failure($"Tenant with ID '{request.Request.TenantId}' already exists");
            }

            // Create new tenant
            var tenant = new Tenant(
                tenantId,
                request.Request.Name,
                request.Request.Description,
                request.CreatedBy);

            _unitOfWork.Tenants.Add(tenant);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created tenant {TenantId} by {CreatedBy}", tenantId, request.CreatedBy);

            var tenantDto = tenant.Adapt<TenantDto>();
            return Result<TenantDto>.Success(tenantDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tenant {TenantId}", request.Request.TenantId);
            return Result<TenantDto>.Failure("Failed to create tenant");
        }
    }
}