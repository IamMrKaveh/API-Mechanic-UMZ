namespace Domain.Variant.ValueObjects;

public sealed record VariantShippingId : StronglyTypedId<VariantShippingId>
{
    private VariantShippingId(Guid value) : base(value) { }

    public static implicit operator Guid(VariantShippingId id) => id.Value;

    public override string ToString() => Value.ToString();
}
