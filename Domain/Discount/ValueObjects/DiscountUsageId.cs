namespace Domain.Discount.ValueObjects;

public sealed record DiscountUsageId : IStronglyTypedId
{
    public Guid Value { get; }

    private DiscountUsageId(Guid value) => Value = value;

    public static DiscountUsageId NewId() => new(Guid.NewGuid());

    public static DiscountUsageId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("DiscountUsageId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(DiscountUsageId id) => id.Value;
}