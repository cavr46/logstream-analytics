using LogStream.Application.Commands.Tenants;

namespace LogStream.Application.Validators;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Request.TenantId)
            .NotEmpty()
            .WithMessage("Tenant ID is required")
            .MaximumLength(100)
            .WithMessage("Tenant ID cannot exceed 100 characters")
            .Matches("^[a-zA-Z0-9_-]+$")
            .WithMessage("Tenant ID can only contain letters, numbers, hyphens, and underscores");

        RuleFor(x => x.Request.Name)
            .NotEmpty()
            .WithMessage("Tenant name is required")
            .MaximumLength(200)
            .WithMessage("Tenant name cannot exceed 200 characters");

        RuleFor(x => x.Request.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Request.Description));

        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .WithMessage("CreatedBy is required")
            .MaximumLength(100)
            .WithMessage("CreatedBy cannot exceed 100 characters");
    }
}