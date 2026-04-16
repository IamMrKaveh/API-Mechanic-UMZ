using Domain.Notification.Aggregates;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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

        builder.Property(e => e.IsRead).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.Property<string>("Title").IsRequired().HasMaxLength(200);
        builder.Property<string>("Message").IsRequired().HasMaxLength(1000);
        builder.Property<string>("Type").IsRequired().HasMaxLength(100);
        builder.Property<string?>("ActionUrl").HasMaxLength(500);
        builder.Property<string?>("RelatedEntityType").HasMaxLength(100);
        builder.Property<Guid?>("RelatedEntityId");

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.IsRead });
    }
}