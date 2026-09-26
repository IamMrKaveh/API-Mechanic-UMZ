namespace Domain.Discount.ValueObjects;

public sealed record DiscountCodeId : StronglyTypedId<DiscountCodeId>
{
    private DiscountCodeId(Guid value) : base(value) { }

    public static implicit operator Guid(DiscountCodeId id) => id.Value;

    public override string ToString() => Value.ToString();
}
