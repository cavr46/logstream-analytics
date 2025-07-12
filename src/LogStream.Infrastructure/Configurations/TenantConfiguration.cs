using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogStream.Infrastructure.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.TenantId)
            .HasConversion(
                tenantId => tenantId.Value,
                value => new TenantId(value))
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(t => t.SubscriptionStartDate)
            .HasColumnType("datetime2");

        builder.Property(t => t.SubscriptionEndDate)
            .HasColumnType("datetime2");

        builder.Property(t => t.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(t => t.UpdatedAt)
            .HasColumnType("datetime2");

        // Configure API Keys as a JSON column for flexibility
        builder.OwnsMany(t => t.AllowedApiKeys, apiKeys =>
        {
            apiKeys.ToJson("AllowedApiKeys");
        });

        // Ignore domain events (they're not persisted)
        builder.Ignore(t => t.DomainEvents);
    }
}