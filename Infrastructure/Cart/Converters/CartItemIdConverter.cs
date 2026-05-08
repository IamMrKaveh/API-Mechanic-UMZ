using Domain.Cart.ValueObjects;

namespace Infrastructure.Cart.Converters;

internal sealed class CartItemIdConverter : StronglyTypedIdConverter<CartItemId>
{
    public CartItemIdConverter() : base(CartItemId.From)
    {
    }
}