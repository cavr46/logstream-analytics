namespace LogStream.Contracts.DTOs;

public record TenantDto
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime? SubscriptionStartDate { get; init; }
    public DateTime? SubscriptionEndDate { get; init; }
    public long MaxLogSizeBytes { get; init; }
    public int MaxRetentionDays { get; init; }
    public int MaxUsersCount { get; init; }
    public int DailyLogIngestLimitMB { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public string? UpdatedBy { get; init; }
}

public record CreateTenantRequest
{
    public string TenantId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public record UpdateTenantRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public record UpdateTenantLimitsRequest
{
    public long MaxLogSizeBytes { get; init; }
    public int MaxRetentionDays { get; init; }
    public int MaxUsersCount { get; init; }
    public int DailyLogIngestLimitMB { get; init; }
}

public record UpdateTenantSubscriptionRequest
{
    public DateTime SubscriptionStartDate { get; init; }
    public DateTime SubscriptionEndDate { get; init; }
}