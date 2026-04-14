using Domain.Shipping.ValueObjects;

namespace Infrastructure.Shipping.Configurations;

internal sealed class ShippingConfiguration : IEntityTypeConfiguration<Domain.Shipping.Aggregates.Shipping>
{
    public void Configure(EntityTypeBuilder<Domain.Shipping.Aggregates.Shipping> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => ShippingId.From(value));

        builder.Property(e => e.Name)
            .HasConversion(n => n.Value, v => ShippingName.Create(v))
            .IsRequired()
            .HasMaxLength(200);

        builder.OwnsOne(e => e.Cost, cost =>
        {
            cost.Property(c => c.Amount).HasColumnName("Cost").HasColumnType("decimal(18,2)").IsRequired();
            cost.Property(c => c.Currency).HasColumnName("CostCurrency").HasMaxLength(10).IsRequired();
        });

        builder.OwnsOne(e => e.FreeShipping, fs =>
        {
            fs.Property(f => f.IsEnabled).HasColumnName("FreeShippingEnabled").IsRequired();
            fs.Property(f => f.ThresholdAmount)
                .HasConversion(
                    m => m != null ? m.Amount : (decimal?)null,
                    v => v.HasValue ? Money.Create(v.Value) : null)
                .HasColumnName("FreeShippingThreshold")
                .HasColumnType("decimal(18,2)");
        });

        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsDefault).IsRequired();
        builder.Property(e => e.SortOrder).IsRequired();
        builder.Property(e => e.MinDeliveryDays);
        builder.Property(e => e.MaxDeliveryDays);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.IsDefault);
        builder.HasIndex(e => e.Name).IsUnique();
    }
}