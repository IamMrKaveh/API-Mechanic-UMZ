namespace Infrastructure.User.Configurations;

public sealed class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.Title).IsRequired().HasMaxLength(100);
        builder.Property(e => e.ReceiverName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Province).IsRequired().HasMaxLength(50);
        builder.Property(e => e.City).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Address).IsRequired().HasMaxLength(500);
        builder.Property(e => e.PostalCode).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Latitude).HasColumnType("decimal(10,7)");
        builder.Property(e => e.Longitude).HasColumnType("decimal(10,7)");

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(e => e.User).WithMany(u => u.UserAddresses).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}