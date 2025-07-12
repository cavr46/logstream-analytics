namespace LogStream.Domain.ValueObjects;

public record LogMessage
{
    public string Content { get; }
    public string? Template { get; }
    public IReadOnlyDictionary<string, object> Properties { get; }

    public LogMessage(string content, string? template = null, IReadOnlyDictionary<string, object>? properties = null)
    {
        if (string.IsNullOrEmpty(content))
            throw new ArgumentException("Log message content cannot be null or empty", nameof(content));

        Content = content;
        Template = template;
        Properties = properties ?? new Dictionary<string, object>();
    }

    public string GetFullMessage()
    {
        if (Properties.Count == 0)
            return Content;

        var message = Content;
        foreach (var (key, value) in Properties)
        {
            message = message.Replace($"{{{key}}}", value?.ToString() ?? "null");
        }
        return message;
    }

    public bool ContainsKeyword(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return false;

        return Content.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
               Template?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true ||
               Properties.Values.Any(v => v?.ToString()?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true);
    }

    public override string ToString() => Content;
}