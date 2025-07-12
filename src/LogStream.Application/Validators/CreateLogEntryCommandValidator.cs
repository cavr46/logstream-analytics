using LogStream.Application.Commands.LogEntries;

namespace LogStream.Application.Validators;

public class CreateLogEntryCommandValidator : AbstractValidator<CreateLogEntryCommand>
{
    public CreateLogEntryCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required");

        RuleFor(x => x.Request.Level)
            .NotEmpty()
            .WithMessage("Log level is required")
            .Must(BeValidLogLevel)
            .WithMessage("Invalid log level. Valid levels are: TRACE, DEBUG, INFO, WARN, ERROR, FATAL");

        RuleFor(x => x.Request.Message)
            .NotEmpty()
            .WithMessage("Log message is required")
            .MaximumLength(10000)
            .WithMessage("Log message cannot exceed 10000 characters");

        RuleFor(x => x.Request.Source.Application)
            .NotEmpty()
            .WithMessage("Application name is required")
            .MaximumLength(200)
            .WithMessage("Application name cannot exceed 200 characters");

        RuleFor(x => x.Request.Source.Environment)
            .NotEmpty()
            .WithMessage("Environment is required")
            .MaximumLength(100)
            .WithMessage("Environment cannot exceed 100 characters");

        RuleFor(x => x.Request.Source.Server)
            .MaximumLength(200)
            .WithMessage("Server name cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Request.Source.Server));

        RuleFor(x => x.Request.Source.Component)
            .MaximumLength(200)
            .WithMessage("Component name cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Request.Source.Component));

        RuleFor(x => x.Request.TraceId)
            .MaximumLength(100)
            .WithMessage("Trace ID cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Request.TraceId));

        RuleFor(x => x.Request.CorrelationId)
            .MaximumLength(200)
            .WithMessage("Correlation ID cannot exceed 200 characters")
            .When(x => !string.IsNullOrEmpty(x.Request.CorrelationId));

        RuleFor(x => x.Request.Exception)
            .MaximumLength(10000)
            .WithMessage("Exception cannot exceed 10000 characters")
            .When(x => !string.IsNullOrEmpty(x.Request.Exception));
    }

    private static bool BeValidLogLevel(string level)
    {
        if (string.IsNullOrWhiteSpace(level))
            return false;

        try
        {
            _ = new LogLevel(level);
            return true;
        }
        catch
        {
            return false;
        }
    }
}