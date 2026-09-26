namespace Domain.Cart.ValueObjects;

public sealed record CartId : StronglyTypedId<CartId>
{
    private CartId(Guid value) : base(value) { }

    public static implicit operator Guid(CartId id) => id.Value;

    public override string ToString() => Value.ToString();
}
