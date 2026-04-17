using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Cart.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Domain.Cart.Aggregates.Cart>
{
    public void Configure(EntityTypeBuilder<Domain.Cart.Aggregates.Cart> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(v => v.Value, v => CartId.From(v));

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Property(e => e.UserId)
            .HasConversion(v => v == null ? (Guid?)null : v.Value, v => v.HasValue ? UserId.From(v.Value) : null);

        builder.Property(e => e.GuestToken)
            .HasConversion(v => v == null ? null : v.Value, v => v == null ? null : GuestToken.Create(v))
            .HasMaxLength(256);

        builder.Property(e => e.IsCheckedOut).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt);

        builder.HasMany(e => e.CartItems)
            .WithOne()
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}