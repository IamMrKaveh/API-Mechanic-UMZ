using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.ValueObjects;

namespace Infrastructure.Wishlist.Configurations;

internal sealed class WishlistConfiguration : IEntityTypeConfiguration<Domain.Wishlist.Aggregates.Wishlist>
{
    public void Configure(EntityTypeBuilder<Domain.Wishlist.Aggregates.Wishlist> builder)
    {
        builder.ToTable("Wishlists");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => WishlistId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.UserId)
            .HasConversion(id => id.Value, value => UserId.From(value))
            .IsRequired();

        builder.Property(e => e.ProductId)
            .HasConversion(id => id.Value, value => ProductId.From(value))
            .IsRequired();

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .HasPrincipalKey(u => u.Id)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(e => e.Product)
            .WithMany()
            .HasForeignKey(e => e.ProductId)
            .HasPrincipalKey(p => p.Id)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.Navigation(e => e.User).AutoInclude(false);
        builder.Navigation(e => e.Product).AutoInclude(false);

        builder.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.ProductId);
    }
}