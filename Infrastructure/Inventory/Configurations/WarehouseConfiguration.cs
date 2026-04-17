using Domain.Inventory.Aggregates;
using Domain.Inventory.ValueObjects;

namespace Infrastructure.Inventory.Configurations;

public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => WarehouseId.From(v));

        builder.Property(e => e.Code)
            .HasConversion(v => v.Value, v => WarehouseCode.Create(v))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.Property(e => e.City).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.IsActive).IsRequired();
        builder.Property(e => e.IsDefault).IsRequired();
        builder.Property(e => e.Priority).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.HasIndex(e => e.Code).IsUnique();

        builder.ToTable("Warehouses");
    }
}