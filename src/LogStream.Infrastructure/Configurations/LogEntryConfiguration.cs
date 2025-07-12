using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogStream.Infrastructure.Configurations;

public class LogEntryConfiguration : IEntityTypeConfiguration<LogEntry>
{
    public void Configure(EntityTypeBuilder<LogEntry> builder)
    {
        builder.ToTable("LogEntries");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.TenantId)
            .HasConversion(
                tenantId => tenantId.Value,
                value => new TenantId(value))
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.Timestamp)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(l => l.Level)
            .HasConversion(
                level => level.Value,
                value => new LogLevel(value))
            .HasMaxLength(20)
            .IsRequired();

        // Configure LogMessage as owned entity
        builder.OwnsOne(l => l.Message, message =>
        {
            message.Property(m => m.Content)
                .HasColumnName("MessageContent")
                .HasMaxLength(10000)
                .IsRequired();

            message.Property(m => m.Template)
                .HasColumnName("MessageTemplate")
                .HasMaxLength(2000);

            message.OwnsMany(m => m.Properties, properties =>
            {
                properties.ToJson("MessageProperties");
            });
        });

        // Configure LogSource as owned entity
        builder.OwnsOne(l => l.Source, source =>
        {
            source.Property(s => s.Application)
                .HasColumnName("SourceApplication")
                .HasMaxLength(200)
                .IsRequired();

            source.Property(s => s.Environment)
                .HasColumnName("SourceEnvironment")
                .HasMaxLength(100)
                .IsRequired();

            source.Property(s => s.Server)
                .HasColumnName("SourceServer")
                .HasMaxLength(200);

            source.Property(s => s.Component)
                .HasColumnName("SourceComponent")
                .HasMaxLength(200);
        });

        builder.Property(l => l.TraceId)
            .HasMaxLength(100);

        builder.Property(l => l.SpanId)
            .HasMaxLength(100);

        builder.Property(l => l.UserId)
            .HasMaxLength(100);

        builder.Property(l => l.SessionId)
            .HasMaxLength(200);

        builder.Property(l => l.CorrelationId)
            .HasMaxLength(200);

        builder.Property(l => l.Exception)
            .HasMaxLength(10000);

        builder.Property(l => l.Tags)
            .HasMaxLength(2000);

        builder.Property(l => l.OriginalFormat)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.RawContent)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(l => l.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(l => l.UserAgent)
            .HasMaxLength(2000);

        builder.Property(l => l.CreatedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(l => l.CreatedAt)
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(l => l.UpdatedAt)
            .HasColumnType("datetime2");

        builder.Property(l => l.ArchivedAt)
            .HasColumnType("datetime2");

        // Configure Metadata as JSON column
        builder.OwnsMany(l => l.Metadata, metadata =>
        {
            metadata.ToJson("Metadata");
        });

        // Ignore domain events (they're not persisted)
        builder.Ignore(l => l.DomainEvents);
    }
}