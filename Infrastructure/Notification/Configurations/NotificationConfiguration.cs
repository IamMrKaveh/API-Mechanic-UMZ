namespace Infrastructure.Notification.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Domain.Notification.Notification>
{
    public void Configure(EntityTypeBuilder<Domain.Notification.Notification> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Message).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.Type).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ActionUrl).HasMaxLength(500);
        builder.Property(e => e.RelatedEntityType).HasMaxLength(100);

        builder.HasOne(e => e.User).WithMany(u => u.Notifications).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}