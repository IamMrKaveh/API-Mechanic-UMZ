namespace Domain.Cart.ValueObjects;

public sealed record CartItemId : StronglyTypedId<CartItemId>
{
    private CartItemId(Guid value) : base(value) { }

    public static implicit operator Guid(CartItemId id) => id.Value;

    public override string ToString() => Value.ToString();
}
