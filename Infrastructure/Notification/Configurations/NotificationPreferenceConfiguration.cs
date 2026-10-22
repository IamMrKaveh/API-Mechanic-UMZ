using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Notification.Configurations;

public sealed class NotificationPreferenceConfiguration
    : IEntityTypeConfiguration<Domain.Notification.Aggregates.NotificationPreference>
{
    public void Configure(
        EntityTypeBuilder<Domain.Notification.Aggregates.NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => e.UserId).IsUnique();

        // The preferences belong to the user and disappear with it.
        builder.HasOne<Domain.User.Aggregates.User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // The disabled types are stored by name; the catalogue rebuilds the value
        // object, so renaming a type can never break an existing row.
        builder.Property(e => e.DisabledTypeNames)
            .HasField("_disabledTypeNames")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasConversion(
                names => names.ToArray(),
                names => (IReadOnlyCollection<string>)names.ToList())
            .HasColumnType("text[]");
    }
}
