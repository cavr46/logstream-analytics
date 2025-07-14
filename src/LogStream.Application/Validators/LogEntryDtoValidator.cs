using FluentValidation;
using LogStream.Contracts.DTOs;

namespace LogStream.Application.Validators;

public class LogEntryDtoValidator : AbstractValidator<LogEntryDto>
{
    public LogEntryDtoValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required")
            .MaximumLength(10000).WithMessage("Message cannot exceed 10,000 characters");

        RuleFor(x => x.Level)
            .NotEmpty().WithMessage("Level is required")
            .Must(BeValidLogLevel).WithMessage("Level must be one of: TRACE, DEBUG, INFO, WARN, ERROR, FATAL");

        RuleFor(x => x.Timestamp)
            .NotEmpty().WithMessage("Timestamp is required")
            .Must(BeValidTimestamp).WithMessage("Timestamp cannot be in the future");

        RuleFor(x => x.Source.Application)
            .NotEmpty().WithMessage("Application is required")
            .MaximumLength(100).WithMessage("Application cannot exceed 100 characters");

        RuleFor(x => x.Source.Environment)
            .NotEmpty().WithMessage("Environment is required")
            .MaximumLength(50).WithMessage("Environment cannot exceed 50 characters");

        RuleFor(x => x.TraceId)
            .MaximumLength(100).WithMessage("TraceId cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.TraceId));

        RuleFor(x => x.SpanId)
            .MaximumLength(100).WithMessage("SpanId cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.SpanId));

        RuleFor(x => x.UserId)
            .MaximumLength(100).WithMessage("UserId cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.UserId));

        RuleFor(x => x.SessionId)
            .MaximumLength(100).WithMessage("SessionId cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.SessionId));

        RuleFor(x => x.CorrelationId)
            .MaximumLength(100).WithMessage("CorrelationId cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.CorrelationId));

        RuleFor(x => x.Exception)
            .MaximumLength(50000).WithMessage("Exception cannot exceed 50,000 characters")
            .When(x => !string.IsNullOrEmpty(x.Exception));

        RuleFor(x => x.IpAddress)
            .Matches(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")
            .WithMessage("Invalid IP address format")
            .When(x => !string.IsNullOrEmpty(x.IpAddress));

        RuleFor(x => x.UserAgent)
            .MaximumLength(500).WithMessage("UserAgent cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.UserAgent));

        RuleFor(x => x.OriginalFormat)
            .NotEmpty().WithMessage("OriginalFormat is required")
            .Must(BeValidFormat).WithMessage("OriginalFormat must be one of: JSON, XML, CSV, TEXT, SYSLOG");
    }

    private static bool BeValidLogLevel(string level)
    {
        if (string.IsNullOrEmpty(level))
            return false;

        var validLevels = new[] { "TRACE", "DEBUG", "INFO", "WARN", "ERROR", "FATAL" };
        return validLevels.Contains(level.ToUpperInvariant());
    }

    private static bool BeValidTimestamp(DateTime timestamp)
    {
        return timestamp <= DateTime.UtcNow.AddMinutes(5); // Allow 5 minutes clock skew
    }

    private static bool BeValidFormat(string format)
    {
        if (string.IsNullOrEmpty(format))
            return false;

        var validFormats = new[] { "JSON", "XML", "CSV", "TEXT", "SYSLOG" };
        return validFormats.Contains(format.ToUpperInvariant());
    }
}