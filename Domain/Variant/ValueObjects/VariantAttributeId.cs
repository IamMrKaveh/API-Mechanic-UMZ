namespace Domain.Variant.ValueObjects;

public sealed record VariantAttributeId : StronglyTypedId<VariantAttributeId>
{
    private VariantAttributeId(Guid value) : base(value) { }

    public static implicit operator Guid(VariantAttributeId id) => id.Value;

    public override string ToString() => Value.ToString();
}
