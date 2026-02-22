namespace Infrastructure.Persistence.Configurations;

public sealed class ShippingConfiguration : IEntityTypeConfiguration<Domain.Shipping.Shipping>
{
    public void Configure(EntityTypeBuilder<Domain.Shipping.Shipping> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.BaseCost).HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR")).HasColumnType("decimal(18,2)");
        builder.Property(e => e.EstimatedDeliveryTime).HasMaxLength(100);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}