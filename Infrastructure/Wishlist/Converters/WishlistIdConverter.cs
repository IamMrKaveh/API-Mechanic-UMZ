using Domain.Wishlist.ValueObjects;

namespace Infrastructure.Wishlist.Converters;

internal sealed class WishlistIdConverter : StronglyTypedIdConverter<WishlistId>
{
    public WishlistIdConverter() : base(WishlistId.From)
    {
    }
}