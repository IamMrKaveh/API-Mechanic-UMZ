using Domain.Audit.Entities;
using Domain.Audit.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Audit.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => AuditLogId.From(v));

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

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.EventType);
    }
}