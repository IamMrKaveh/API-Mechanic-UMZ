namespace Domain.Variant.ValueObjects;

public sealed record VariantShippingId : IStronglyTypedId
{
    public Guid Value { get; }

    private VariantShippingId(Guid value) => Value = value;

    public static VariantShippingId NewId() => new(Guid.NewGuid());

    public static VariantShippingId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("VariantShippingId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(VariantShippingId id) => id.Value;
}