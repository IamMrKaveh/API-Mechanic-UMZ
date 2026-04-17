using Domain.User.Entities;
using Domain.User.ValueObjects;

namespace Infrastructure.User.Configurations;

internal sealed class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => UserAddressId.From(value));

        builder.Property(e => e.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(e => e.Title)
            .HasMaxLength(100);

        builder.Property(e => e.ReceiverName)
            .IsRequired()
            .HasMaxLength(100);

        builder.OwnsOne(e => e.PhoneNumber, pn =>
        {
            pn.Property(p => p.Value)
                .HasColumnName("ReceiverPhoneNumber")
                .IsRequired()
                .HasMaxLength(20);
        });

        builder.Property(e => e.Province)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.PostalCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Latitude)
            .HasColumnType("decimal(9,6)");

        builder.Property(e => e.Longitude)
            .HasColumnType("decimal(9,6)");

        builder.Property(e => e.IsDefault).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => e.UserId);
    }
}