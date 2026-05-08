namespace Domain.Discount.ValueObjects;

public sealed record DiscountCodeId : IStronglyTypedId
{
    public Guid Value { get; }

    private DiscountCodeId(Guid value) => Value = value;

    public static DiscountCodeId NewId() => new(Guid.NewGuid());

    public static DiscountCodeId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("DiscountCodeId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(DiscountCodeId id) => id.Value;
}