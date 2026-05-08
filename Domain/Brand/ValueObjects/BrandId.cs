namespace Domain.Brand.ValueObjects;

public sealed record BrandId : IStronglyTypedId
{
    public Guid Value { get; }

    private BrandId(Guid value) => Value = value;

    public static BrandId NewId() => new(Guid.NewGuid());

    public static BrandId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("BrandId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(BrandId id) => id.Value;
}