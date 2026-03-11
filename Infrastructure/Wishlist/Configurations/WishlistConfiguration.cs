namespace Infrastructure.Wishlist.Configurations;

public sealed class WishlistConfiguration : IEntityTypeConfiguration<Domain.Wishlist.Aggregates.Wishlist>
{
    public void Configure(EntityTypeBuilder<Domain.Wishlist.Aggregates.Wishlist> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.RowVersion).IsRowVersion();

        builder.HasOne(e => e.User).WithMany(u => u.Wishlists).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
    }
}