namespace Domain.Shipping.ValueObjects;

public sealed record ShippingId
{
    public Guid Value { get; }

    private ShippingId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("ShippingId cannot be empty.", nameof(value));

        Value = value;
    }

    public static ShippingId NewId() => new(Guid.NewGuid());

    public static ShippingId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}