using Domain.Cart.ValueObjects;

namespace Infrastructure.Cart.Converters;

internal sealed class CartIdConverter : StronglyTypedIdConverter<CartId>
{
    public CartIdConverter() : base(CartId.From)
    {
    }
}