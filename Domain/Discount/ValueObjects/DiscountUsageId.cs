namespace Domain.Discount.ValueObjects;

public sealed record DiscountUsageId : StronglyTypedId<DiscountUsageId>
{
    private DiscountUsageId(Guid value) : base(value) { }

    public static implicit operator Guid(DiscountUsageId id) => id.Value;

    public override string ToString() => Value.ToString();
}
