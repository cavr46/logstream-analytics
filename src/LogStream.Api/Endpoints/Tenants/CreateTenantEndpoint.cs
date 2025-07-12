namespace LogStream.Api.Endpoints.Tenants;

public class CreateTenantEndpoint : Endpoint<CreateTenantRequest, Result<TenantDto>>
{
    private readonly IMediator _mediator;

    public CreateTenantEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/api/tenants");
        AllowAnonymous(); // In production, add proper authentication
        Summary(s =>
        {
            s.Summary = "Create a new tenant";
            s.Description = "Creates a new tenant in the system with the specified details";
            s.Responses[200] = "Tenant created successfully";
            s.Responses[400] = "Invalid request";
            s.Responses[409] = "Tenant already exists";
        });
    }

    public override async Task HandleAsync(CreateTenantRequest req, CancellationToken ct)
    {
        var command = new CreateTenantCommand(req, User?.Identity?.Name ?? "system");
        var result = await _mediator.Send(command, ct);

        if (result.IsSuccess)
        {
            await SendOkAsync(result, ct);
        }
        else
        {
            await SendResultAsync(Results.BadRequest(result), ct);
        }
    }
}