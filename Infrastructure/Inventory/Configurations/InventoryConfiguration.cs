using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Inventory.Configurations;

internal sealed class InventoryConfiguration : IEntityTypeConfiguration<Domain.Inventory.Aggregates.Inventory>
{
    public void Configure(EntityTypeBuilder<Domain.Inventory.Aggregates.Inventory> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => InventoryId.From(value));

        builder.Property(e => e.VariantId)
            .HasConversion(id => id.Value, value => VariantId.From(value))
            .IsRequired();

        builder.OwnsOne(e => e.StockQuantity, sq =>
        {
            sq.Property(v => v.Value)
                .HasColumnName("StockQuantity")
                .IsRequired();
        });

        builder.OwnsOne(e => e.ReservedQuantity, rq =>
        {
            rq.Property(v => v.Value)
                .HasColumnName("ReservedQuantity")
                .IsRequired();
        });

        builder.Property(e => e.IsUnlimited).IsRequired();
        builder.Property(e => e.LowStockThreshold).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasIndex(e => e.VariantId).IsUnique();

        builder.HasOne(e => e.Variant)
            .WithOne()
            .HasForeignKey<Domain.Inventory.Aggregates.Inventory>(e => e.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.LedgerEntries)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Domain.Inventory.Aggregates.Inventory.LedgerEntries))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.ToTable("Inventories");
    }
}