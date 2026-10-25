using Domain.Inventory.Entities;

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
        builder.HasIndex(e => e.VariantId);
        builder.HasIndex(e => new { e.VariantId, e.CreatedAt }).IsDescending(false, true);

        // Reservation-expiry job filters EventType + CreatedAt; admin report filters
        // a CreatedAt range; reference lookups resolve by ReferenceNumber.
        builder.HasIndex(e => new { e.EventType, e.CreatedAt })
            .HasDatabaseName("IX_StockLedgerEntries_EventType_CreatedAt");
        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_StockLedgerEntries_CreatedAt");
        builder.HasIndex(e => e.ReferenceNumber)
            .HasDatabaseName("IX_StockLedgerEntries_ReferenceNumber");

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