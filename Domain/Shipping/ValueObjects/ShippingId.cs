namespace Domain.Shipping.ValueObjects;

public sealed record ShippingId : IStronglyTypedId
{
    public Guid Value { get; }

    private ShippingId(Guid value) => Value = value;

    public static ShippingId NewId() => new(Guid.NewGuid());

    public static ShippingId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("ShippingId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(ShippingId id) => id.Value;
}