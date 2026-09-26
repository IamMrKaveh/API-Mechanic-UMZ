namespace Domain.Variant.ValueObjects;

public sealed record VariantId : StronglyTypedId<VariantId>
{
    private VariantId(Guid value) : base(value) { }

    public static implicit operator Guid(VariantId id) => id.Value;

    public override string ToString() => Value.ToString();
}
