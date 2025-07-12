namespace LogStream.Domain.ValueObjects;

public record TenantId
{
    public string Value { get; }

    public TenantId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Tenant ID cannot be null or empty", nameof(value));
        
        if (value.Length > 100)
            throw new ArgumentException("Tenant ID cannot exceed 100 characters", nameof(value));

        if (!IsValidTenantId(value))
            throw new ArgumentException("Invalid tenant ID format", nameof(value));

        Value = value.ToLowerInvariant();
    }

    public static implicit operator string(TenantId tenantId) => tenantId.Value;
    public static implicit operator TenantId(string value) => new(value);

    private static bool IsValidTenantId(string value)
    {
        return value.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
    }

    public override string ToString() => Value;
}