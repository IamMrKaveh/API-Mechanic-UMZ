namespace Domain.Brand.ValueObjects;

public sealed record BrandId : StronglyTypedId<BrandId>
{
    private BrandId(Guid value) : base(value) { }

    public static implicit operator Guid(BrandId id) => id.Value;

    public override string ToString() => Value.ToString();
}
