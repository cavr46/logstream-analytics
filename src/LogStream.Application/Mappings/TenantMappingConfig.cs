using LogStream.Contracts.DTOs;

namespace LogStream.Application.Mappings;

public class TenantMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Tenant, TenantDto>()
            .Map(dest => dest.TenantId, src => src.TenantId.Value);

        config.NewConfig<TenantDto, Tenant>()
            .ConstructUsing(src => new Tenant(
                new TenantId(src.TenantId),
                src.Name,
                src.Description,
                src.CreatedBy))
            .Ignore(dest => dest.DomainEvents);
    }
}