namespace Domain.Discount.ValueObjects;

public sealed record DiscountRestrictionId : IStronglyTypedId
{
    public Guid Value { get; }

    private DiscountRestrictionId(Guid value) => Value = value;

    public static DiscountRestrictionId NewId() => new(Guid.NewGuid());

    public static DiscountRestrictionId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("DiscountRestrictionId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(DiscountRestrictionId id) => id.Value;
}