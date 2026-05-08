namespace Domain.Variant.ValueObjects;

public sealed record VariantId : IStronglyTypedId
{
    public Guid Value { get; }

    private VariantId(Guid value) => Value = value;

    public static VariantId NewId() => new(Guid.NewGuid());

    public static VariantId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("VariantId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(VariantId id) => id.Value;
}