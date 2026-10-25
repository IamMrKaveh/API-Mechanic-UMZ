using Domain.Audit.Entities;
using Domain.Audit.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Audit.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);

        builder.Property(e => e.UserId)
            .HasConversion(
                v => v == null ? (Guid?)null : v.Value,
                v => v == null ? null : UserId.From(v.Value));

        builder.Property(e => e.EventType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Action).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Details).HasColumnType("text");
        builder.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.Property(e => e.EntityType).HasMaxLength(100);
        builder.Property(e => e.EntityId).HasMaxLength(100);
        builder.Property(e => e.IntegrityHash).IsRequired().HasMaxLength(200);

        builder.Property(e => e.HashVersion)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.CreatedAt);

        builder.HasIndex(e => e.EntityType)
            .HasDatabaseName("IX_AuditLogs_EntityType");

        // Only unarchived rows are ever pending work; a partial index stays small
        // while the archived history grows unbounded.
        builder.HasIndex(e => e.IsArchived)
            .HasFilter("\"IsArchived\" = false")
            .HasDatabaseName("IX_AuditLogs_Unarchived");

        builder.HasIndex(e => new { e.CreatedAt, e.IsArchived, e.EventType })
            .HasDatabaseName("IX_AuditLogs_CreatedAt_IsArchived_EventType");

        builder.HasIndex(e => new { e.EntityType, e.EntityId })
            .HasDatabaseName("IX_AuditLogs_EntityType_EntityId");

        // Trigram index for admin "%term%" search over Details; without it every
        // search is a sequential scan over the largest table in the system.
        // Requires the pg_trgm extension.
        builder.HasIndex(e => e.Details)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops")
            .HasDatabaseName("IX_AuditLogs_Details_trgm");
    }
}
