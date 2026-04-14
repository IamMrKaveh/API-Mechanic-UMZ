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

        builder.Property("UserId")
            .IsRequired();

        builder.OwnsOne(e => e.Address, addr =>
        {
            addr.Property(a => a.Province).HasColumnName("Province").IsRequired().HasMaxLength(100);
            addr.Property(a => a.City).HasColumnName("City").IsRequired().HasMaxLength(100);
            addr.Property(a => a.Street).HasColumnName("Street").IsRequired().HasMaxLength(500);
            addr.Property(a => a.PostalCode).HasColumnName("PostalCode").IsRequired().HasMaxLength(20);
            addr.Property(a => a.Latitude).HasColumnName("Latitude");
            addr.Property(a => a.Longitude).HasColumnName("Longitude");
        });

        builder.Property(e => e.Title).HasMaxLength(100);
        builder.Property(e => e.ReceiverName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ReceiverPhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(e => e.IsDefault).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);
    }
}