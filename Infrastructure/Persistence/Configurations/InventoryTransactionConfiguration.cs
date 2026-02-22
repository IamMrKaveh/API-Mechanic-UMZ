namespace Infrastructure.Persistence.Configurations;

public sealed class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.TransactionType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.ReferenceNumber).HasMaxLength(100);
        builder.Property(e => e.CorrelationId).HasMaxLength(200);
        builder.Property(e => e.CartId).HasMaxLength(100);

        builder.HasOne(e => e.Variant).WithMany(v => v.InventoryTransactions).HasForeignKey(e => e.VariantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.User).WithMany(u => u.InventoryTransactions).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(e => e.ReversalTransactions).WithOne().HasForeignKey("_reversedByTransactionId").OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.VariantId, e.TransactionType, e.CorrelationId }).IsUnique().HasFilter("\"CorrelationId\" IS NOT NULL");
    }
}