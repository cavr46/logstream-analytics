namespace LogStream.Api.Endpoints.Tenants;

public class GetTenantRequest
{
    public string TenantId { get; set; } = string.Empty;
}

public class GetTenantEndpoint : Endpoint<GetTenantRequest, Result<TenantDto>>
{
    private readonly IMediator _mediator;

    public GetTenantEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/api/tenants/{tenantId}");
        AllowAnonymous(); // In production, add proper authentication
        Summary(s =>
        {
            s.Summary = "Get tenant by ID";
            s.Description = "Retrieves a tenant by its ID";
            s.Responses[200] = "Tenant found";
            s.Responses[404] = "Tenant not found";
        });
    }

    public override async Task HandleAsync(GetTenantRequest req, CancellationToken ct)
    {
        var query = new GetTenantQuery(req.TenantId);
        var result = await _mediator.Send(query, ct);

        if (result.IsSuccess)
        {
            await SendOkAsync(result, ct);
        }
        else
        {
            await SendResultAsync(Results.NotFound(result), ct);
        }
    }
}