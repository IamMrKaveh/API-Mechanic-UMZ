using Domain.Inventory.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Inventory.Configurations;

public sealed class StockLedgerEntryConfiguration : IEntityTypeConfiguration<StockLedgerEntry>
{
    public void Configure(EntityTypeBuilder<StockLedgerEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(200);
        builder.Property(e => e.EventType).HasConversion<string>().HasMaxLength(50);
        builder.Property(e => e.ReferenceNumber).HasMaxLength(100);
        builder.Property(e => e.CorrelationId).HasMaxLength(200);
        builder.Property(e => e.Note).HasMaxLength(500);
        builder.Property(e => e.UnitCost).HasColumnType("decimal(18,4)");

        builder.HasIndex(e => e.IdempotencyKey).IsUnique();

        builder.HasOne(e => e.ProductVariant)
            .WithMany()
            .HasForeignKey(e => e.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}