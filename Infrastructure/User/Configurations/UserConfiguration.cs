using Domain.User.ValueObjects;

namespace Infrastructure.User.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<Domain.User.Aggregates.User>
{
    public void Configure(EntityTypeBuilder<Domain.User.Aggregates.User> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => UserId.From(value));

        builder.OwnsOne(e => e.FullName, fn =>
        {
            fn.Property(f => f.FirstName).HasColumnName("FirstName").IsRequired().HasMaxLength(100);
            fn.Property(f => f.LastName).HasColumnName("LastName").IsRequired().HasMaxLength(100);
        });

        builder.OwnsOne(e => e.Email, em =>
        {
            em.Property(e => e.Value).HasColumnName("Email").IsRequired().HasMaxLength(256);
        });

        builder.OwnsOne(e => e.PhoneNumber, pn =>
        {
            pn.Property(p => p.Value).HasColumnName("PhoneNumber").HasMaxLength(20);
        });

        builder.Property(e => e.PasswordHash).IsRequired(false).HasMaxLength(500);
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsAdmin).IsRequired();
        builder.Property(e => e.IsEmailVerified).IsRequired();
        builder.Property(e => e.FailedLoginAttempts).IsRequired();
        builder.Property(e => e.LockoutEnd);
        builder.Property(e => e.LastLoginAt);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.Property(e => e.DefaultAddressId)
            .HasConversion(
                id => id != null ? id.Value : (Guid?)null,
                value => value.HasValue ? UserAddressId.From(value.Value) : null);

        builder.HasMany(e => e.Addresses)
            .WithOne()
            .HasForeignKey("UserId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex("Email_Value").IsUnique().HasDatabaseName("IX_Users_Email");

        builder.HasIndex("PhoneNumber_Value")
            .IsUnique()
            .HasFilter("\"PhoneNumber_Value\" IS NOT NULL")
            .HasDatabaseName("IX_Users_PhoneNumber");

        builder.HasQueryFilter(e => e.IsActive);
    }
}