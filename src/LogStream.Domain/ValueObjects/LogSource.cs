namespace LogStream.Domain.ValueObjects;

public record LogSource
{
    public string Application { get; }
    public string Environment { get; }
    public string? Server { get; }
    public string? Component { get; }

    public LogSource(string application, string environment, string? server = null, string? component = null)
    {
        if (string.IsNullOrWhiteSpace(application))
            throw new ArgumentException("Application cannot be null or empty", nameof(application));
        
        if (string.IsNullOrWhiteSpace(environment))
            throw new ArgumentException("Environment cannot be null or empty", nameof(environment));

        Application = application.Trim();
        Environment = environment.Trim();
        Server = server?.Trim();
        Component = component?.Trim();
    }

    public string GetIdentifier() => $"{Application}:{Environment}:{Server ?? "unknown"}:{Component ?? "default"}";

    public override string ToString() => GetIdentifier();
}