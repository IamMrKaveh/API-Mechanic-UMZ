namespace Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.VariantSku).HasMaxLength(100);
        builder.Property(e => e.VariantAttributes).HasColumnType("text");

        builder.Property(e => e.PurchasePriceAtOrder).HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR")).HasColumnType("decimal(18,2)");
        builder.Property(e => e.SellingPriceAtOrder).HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR")).HasColumnType("decimal(18,2)");
        builder.Property(e => e.OriginalPriceAtOrder).HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR")).HasColumnType("decimal(18,2)");
        builder.Property(e => e.DiscountAtOrder).HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR")).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Amount).HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR")).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Profit).HasConversion(v => v.Amount, v => Money.FromDecimal(v, "IRR")).HasColumnType("decimal(18,2)");

        builder.HasOne(e => e.Variant).WithMany(v => v.OrderItems).HasForeignKey(e => e.VariantId).OnDelete(DeleteBehavior.Restrict);
    }
}