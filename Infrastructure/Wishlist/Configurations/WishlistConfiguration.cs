using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.ValueObjects;

namespace Infrastructure.Wishlist.Configurations;

internal sealed class WishlistConfiguration : IEntityTypeConfiguration<Domain.Wishlist.Aggregates.Wishlist>
{
    public void Configure(EntityTypeBuilder<Domain.Wishlist.Aggregates.Wishlist> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => WishlistId.From(value));

        builder.Property(e => e.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(e => e.ProductId)
            .HasConversion(id => id.Value, value => ProductId.From(value))
            .IsRequired();

        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
        builder.HasIndex(e => e.UserId);
    }
}