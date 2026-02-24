namespace Infrastructure.Persistence.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.HasOne(d => d.Cart)
            .WithMany(p => p.CartItems)
            .HasForeignKey(d => d.CartId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Variant)
               .WithMany(p => p.CartItems)
               .HasForeignKey(d => d.VariantId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasIndex(e => new { e.CartId, e.VariantId }).IsUnique();
    }
}