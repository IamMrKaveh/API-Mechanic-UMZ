namespace Infrastructure.Audit.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Action).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Details).IsRequired().HasColumnType("text");
        builder.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.UserAgent).HasMaxLength(500);

        builder.Property(x => x.IntegrityHash).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsArchived).HasDefaultValue(false);

        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.EventType);
        builder.HasIndex(x => x.IsArchived);
        builder.HasIndex(x => new { x.UserId, x.Timestamp });
    }
}