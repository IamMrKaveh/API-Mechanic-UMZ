namespace Domain.Brand.ValueObjects;

public sealed record BrandId
{
    public Guid Value { get; }

    private BrandId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("BrandId cannot be empty.", nameof(value));

        Value = value;
    }

    public static BrandId NewId() => new(Guid.NewGuid());

    public static BrandId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}