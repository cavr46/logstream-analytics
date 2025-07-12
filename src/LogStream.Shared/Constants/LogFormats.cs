namespace LogStream.Shared.Constants;

public static class LogFormats
{
    public const string Json = "JSON";
    public const string Xml = "XML";
    public const string Csv = "CSV";
    public const string Apache = "APACHE";
    public const string Nginx = "NGINX";
    public const string Iis = "IIS";
    public const string Syslog = "SYSLOG";
    public const string WindowsEventLog = "WINDOWS_EVENT";
    public const string CustomRegex = "CUSTOM_REGEX";
    public const string PlainText = "PLAIN_TEXT";
    
    public static readonly IReadOnlySet<string> SupportedFormats = new HashSet<string>
    {
        Json, Xml, Csv, Apache, Nginx, Iis, Syslog, WindowsEventLog, CustomRegex, PlainText
    };
}