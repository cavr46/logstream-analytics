namespace LogStream.Api.Endpoints.LogEntries;

public class CreateLogEntryEndpoint : Endpoint<CreateLogEntryRequest, Result<LogEntryDto>>
{
    private readonly IMediator _mediator;

    public CreateLogEntryEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/api/logs/{tenantId}");
        AllowAnonymous(); // In production, add API key authentication
        Summary(s =>
        {
            s.Summary = "Create a new log entry";
            s.Description = "Creates a new log entry for the specified tenant";
            s.Responses[200] = "Log entry created successfully";
            s.Responses[400] = "Invalid request";
            s.Responses[404] = "Tenant not found";
        });
    }

    public override async Task HandleAsync(CreateLogEntryRequest req, CancellationToken ct)
    {
        var tenantId = Route<string>("tenantId");
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        // Set IP and User Agent if not provided
        if (string.IsNullOrEmpty(req.IpAddress))
        {
            req = req with { IpAddress = clientIp };
        }

        if (string.IsNullOrEmpty(req.UserAgent))
        {
            req = req with { UserAgent = userAgent };
        }

        var command = new CreateLogEntryCommand(tenantId!, req, User?.Identity?.Name ?? "api");
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