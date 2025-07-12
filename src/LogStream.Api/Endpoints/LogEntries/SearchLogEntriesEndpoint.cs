namespace LogStream.Api.Endpoints.LogEntries;

public class SearchLogEntriesEndpoint : Endpoint<LogSearchRequest, Result<PagedResult<LogEntryDto>>>
{
    private readonly IMediator _mediator;

    public SearchLogEntriesEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Get("/api/logs/{tenantId}/search");
        AllowAnonymous(); // In production, add proper authentication
        Summary(s =>
        {
            s.Summary = "Search log entries";
            s.Description = "Searches log entries for the specified tenant with various filters";
            s.Responses[200] = "Search completed successfully";
            s.Responses[400] = "Invalid search parameters";
            s.Responses[404] = "Tenant not found";
        });
    }

    public override async Task HandleAsync(LogSearchRequest req, CancellationToken ct)
    {
        var tenantId = Route<string>("tenantId");
        
        var query = new SearchLogEntriesQuery(tenantId!, req);
        var result = await _mediator.Send(query, ct);

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