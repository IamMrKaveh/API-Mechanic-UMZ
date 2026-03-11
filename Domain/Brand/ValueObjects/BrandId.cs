namespace Domain.Brand.ValueObjects;

public sealed record BrandId(Guid Value)
{
    public static BrandId NewId() => new(Guid.NewGuid());
    public static BrandId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}