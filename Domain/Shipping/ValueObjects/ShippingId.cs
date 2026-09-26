namespace Domain.Shipping.ValueObjects;

public sealed record ShippingId : StronglyTypedId<ShippingId>
{
    private ShippingId(Guid value) : base(value) { }

    public static implicit operator Guid(ShippingId id) => id.Value;

    public override string ToString() => Value.ToString();
}
