using LogStream.Domain.Common;
using LogStream.Domain.ValueObjects;
using LogStream.Domain.Events;

namespace LogStream.Domain.Entities;

public class Tenant : BaseEntity
{
    public TenantId TenantId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? SubscriptionStartDate { get; private set; }
    public DateTime? SubscriptionEndDate { get; private set; }
    public long MaxLogSizeBytes { get; private set; }
    public int MaxRetentionDays { get; private set; }
    public int MaxUsersCount { get; private set; }
    public int DailyLogIngestLimitMB { get; private set; }

    private readonly List<string> _allowedApiKeys = new();
    public IReadOnlyCollection<string> AllowedApiKeys => _allowedApiKeys.AsReadOnly();

    protected Tenant() { } // EF Core

    public Tenant(TenantId tenantId, string name, string? description = null, string createdBy = "system")
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        Name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : throw new ArgumentException("Name cannot be empty", nameof(name));
        Description = description?.Trim();
        IsActive = true;
        SubscriptionStartDate = DateTime.UtcNow;
        MaxLogSizeBytes = 10_000_000_000; // 10GB default
        MaxRetentionDays = 90; // 3 months default
        MaxUsersCount = 10;
        DailyLogIngestLimitMB = 1000; // 1GB per day default
        CreatedBy = createdBy;

        AddDomainEvent(new TenantCreatedEvent(Id, TenantId));
    }

    public void UpdateName(string name, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        var oldName = Name;
        Name = name.Trim();
        MarkAsUpdated(updatedBy);

        AddDomainEvent(new TenantUpdatedEvent(Id, TenantId, oldName, Name));
    }

    public void UpdateDescription(string? description, string updatedBy)
    {
        Description = description?.Trim();
        MarkAsUpdated(updatedBy);
    }

    public void Activate(string updatedBy)
    {
        if (IsActive) return;

        IsActive = true;
        MarkAsUpdated(updatedBy);

        AddDomainEvent(new TenantActivatedEvent(Id, TenantId));
    }

    public void Deactivate(string updatedBy)
    {
        if (!IsActive) return;

        IsActive = false;
        MarkAsUpdated(updatedBy);

        AddDomainEvent(new TenantDeactivatedEvent(Id, TenantId));
    }

    public void UpdateSubscription(DateTime startDate, DateTime endDate, string updatedBy)
    {
        if (startDate >= endDate)
            throw new ArgumentException("Start date must be before end date");

        SubscriptionStartDate = startDate;
        SubscriptionEndDate = endDate;
        MarkAsUpdated(updatedBy);
    }

    public void UpdateLimits(long maxLogSizeBytes, int maxRetentionDays, int maxUsersCount, int dailyLogIngestLimitMB, string updatedBy)
    {
        if (maxLogSizeBytes <= 0)
            throw new ArgumentException("Max log size must be positive", nameof(maxLogSizeBytes));
        
        if (maxRetentionDays <= 0)
            throw new ArgumentException("Max retention days must be positive", nameof(maxRetentionDays));
        
        if (maxUsersCount <= 0)
            throw new ArgumentException("Max users count must be positive", nameof(maxUsersCount));
        
        if (dailyLogIngestLimitMB <= 0)
            throw new ArgumentException("Daily log ingest limit must be positive", nameof(dailyLogIngestLimitMB));

        MaxLogSizeBytes = maxLogSizeBytes;
        MaxRetentionDays = maxRetentionDays;
        MaxUsersCount = maxUsersCount;
        DailyLogIngestLimitMB = dailyLogIngestLimitMB;
        MarkAsUpdated(updatedBy);
    }

    public void AddApiKey(string apiKey, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be empty", nameof(apiKey));

        if (!_allowedApiKeys.Contains(apiKey))
        {
            _allowedApiKeys.Add(apiKey);
            MarkAsUpdated(updatedBy);
        }
    }

    public void RemoveApiKey(string apiKey, string updatedBy)
    {
        if (_allowedApiKeys.Remove(apiKey))
        {
            MarkAsUpdated(updatedBy);
        }
    }

    public bool IsValidApiKey(string apiKey) => _allowedApiKeys.Contains(apiKey);

    public bool IsSubscriptionActive() => 
        SubscriptionStartDate.HasValue && 
        SubscriptionEndDate.HasValue && 
        DateTime.UtcNow >= SubscriptionStartDate.Value && 
        DateTime.UtcNow <= SubscriptionEndDate.Value;

    public bool CanIngestLogs() => IsActive && IsSubscriptionActive();
}