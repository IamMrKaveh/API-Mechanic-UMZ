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

        builder.OwnsOne(e => e.BaseCost, cost =>
        {
            cost.Property(c => c.Amount).HasColumnName("Cost").HasColumnType("decimal(18,2)").IsRequired();
            cost.Property(c => c.Currency).HasColumnName("CostCurrency").HasMaxLength(10).IsRequired();
        });

        builder.OwnsOne(e => e.FreeShipping, fs =>
        {
            fs.Property(f => f.IsEnabled).HasColumnName("FreeShippingEnabled").IsRequired();
            fs.OwnsOne(f => f.ThresholdAmount, ta =>
            {
                ta.Property(m => m.Amount)
                    .HasColumnName("FreeShippingThreshold")
                    .HasColumnType("decimal(18,2)");
                ta.Property(m => m.Currency)
                    .HasColumnName("FreeShippingThresholdCurrency")
                    .HasMaxLength(10);
            });
        });

        builder.OwnsOne(e => e.DeliveryTime, dt =>
        {
            dt.Property(d => d.MinDays).HasColumnName("MinDeliveryDays").IsRequired();
            dt.Property(d => d.MaxDays).HasColumnName("MaxDeliveryDays").IsRequired();
        });

        builder.OwnsOne(e => e.OrderRange, or =>
        {
            or.OwnsOne(r => r.MinOrderAmount, m =>
                m.Property(p => p.Amount).HasColumnName("MinOrderAmount").HasColumnType("decimal(18,2)"));
            or.OwnsOne(r => r.MaxOrderAmount, m =>
                m.Property(p => p.Amount).HasColumnName("MaxOrderAmount").HasColumnType("decimal(18,2)"));
        });

        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.EstimatedDeliveryTime).HasMaxLength(200);
        builder.Property(e => e.MaxWeight).HasColumnType("decimal(18,2)");
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsDefault).IsRequired();
        builder.Property(e => e.SortOrder).IsRequired();
        builder.Property(e => e.IsDeleted).IsRequired();
        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.DeletedBy);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.IsDefault);
        builder.HasIndex(e => e.Name).IsUnique();
    }
}