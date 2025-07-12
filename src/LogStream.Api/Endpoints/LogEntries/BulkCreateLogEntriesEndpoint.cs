namespace LogStream.Api.Endpoints.LogEntries;

public class BulkCreateLogEntriesEndpoint : Endpoint<BulkLogIngestRequest, Result<BulkCreateLogEntriesResponse>>
{
    private readonly IMediator _mediator;

    public BulkCreateLogEntriesEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/api/logs/{tenantId}/bulk");
        AllowAnonymous(); // In production, add API key authentication
        Summary(s =>
        {
            s.Summary = "Bulk create log entries";
            s.Description = "Creates multiple log entries for the specified tenant in a single request";
            s.Responses[200] = "Bulk operation completed";
            s.Responses[400] = "Invalid request";
            s.Responses[404] = "Tenant not found";
        });
    }

    public override async Task HandleAsync(BulkLogIngestRequest req, CancellationToken ct)
    {
        var tenantId = Route<string>("tenantId");
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        // Enrich log entries with IP and User Agent if not provided
        var enrichedLogEntries = req.LogEntries.Select(logEntry =>
        {
            var enriched = logEntry;
            
            if (string.IsNullOrEmpty(logEntry.IpAddress))
            {
                enriched = enriched with { IpAddress = clientIp };
            }

            if (string.IsNullOrEmpty(logEntry.UserAgent))
            {
                enriched = enriched with { UserAgent = userAgent };
            }

            return enriched;
        }).ToList();

        var enrichedRequest = req with { LogEntries = enrichedLogEntries };
        
        var command = new BulkCreateLogEntriesCommand(tenantId!, enrichedRequest, User?.Identity?.Name ?? "api");
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