namespace LogStream.Shared.Constants;

public static class LogLevel
{
    public const string Trace = "TRACE";
    public const string Debug = "DEBUG";
    public const string Information = "INFO";
    public const string Warning = "WARN";
    public const string Error = "ERROR";
    public const string Critical = "FATAL";
    
    public static readonly IReadOnlySet<string> AllLevels = new HashSet<string>
    {
        Trace, Debug, Information, Warning, Error, Critical
    };

    public static int GetSeverityLevel(string level) => level?.ToUpperInvariant() switch
    {
        Trace => 1,
        Debug => 2,
        Information => 3,
        Warning => 4,
        Error => 5,
        Critical => 6,
        _ => 0
    };
}