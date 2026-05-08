namespace Domain.Variant.ValueObjects;

public sealed record VariantAttributeId : IStronglyTypedId
{
    public Guid Value { get; }

    private VariantAttributeId(Guid value) => Value = value;

    public static VariantAttributeId NewId() => new(Guid.NewGuid());

    public static VariantAttributeId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("VariantAttributeId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(VariantAttributeId id) => id.Value;
}