using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Notification.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Domain.Notification.Aggregates.Notification>
{
    public void Configure(EntityTypeBuilder<Domain.Notification.Aggregates.Notification> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
               .HasConversion(v => v.Value, v => NotificationId.From(v));

        builder.Property(e => e.UserId)
               .HasConversion(v => v.Value, v => UserId.From(v))
               .IsRequired();

        builder.Property(e => e.Title)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(e => e.Message)
               .IsRequired()
               .HasMaxLength(1000);

        builder.Property(e => e.Type)
               .HasConversion(v => v.Value, v => NotificationType.FromString(v))
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.ActionUrl)
               .HasMaxLength(500);

        builder.Property(e => e.RelatedEntityType)
               .HasMaxLength(100);

        builder.Property(e => e.RelatedEntityId);

        builder.Property(e => e.IsRead).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.IsRead });
    }
}