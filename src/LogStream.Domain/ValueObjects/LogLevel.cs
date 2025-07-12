namespace LogStream.Domain.ValueObjects;

public record LogLevel
{
    public string Value { get; }
    public int SeverityLevel { get; }

    public LogLevel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Log level cannot be null or empty", nameof(value));

        var normalizedValue = value.ToUpperInvariant();
        if (!Shared.Constants.LogLevel.AllLevels.Contains(normalizedValue))
            throw new ArgumentException($"Invalid log level: {value}", nameof(value));

        Value = normalizedValue;
        SeverityLevel = Shared.Constants.LogLevel.GetSeverityLevel(normalizedValue);
    }

    public static implicit operator string(LogLevel logLevel) => logLevel.Value;
    public static implicit operator LogLevel(string value) => new(value);

    public bool IsMoreSevereThan(LogLevel other) => SeverityLevel > other.SeverityLevel;
    public bool IsLessSevereThan(LogLevel other) => SeverityLevel < other.SeverityLevel;

    public override string ToString() => Value;
}