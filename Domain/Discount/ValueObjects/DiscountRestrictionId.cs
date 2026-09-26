namespace Domain.Discount.ValueObjects;

public sealed record DiscountRestrictionId : StronglyTypedId<DiscountRestrictionId>
{
    private DiscountRestrictionId(Guid value) : base(value) { }

    public static implicit operator Guid(DiscountRestrictionId id) => id.Value;

    public override string ToString() => Value.ToString();
}
