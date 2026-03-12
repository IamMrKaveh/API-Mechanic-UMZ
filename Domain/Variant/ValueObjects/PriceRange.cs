namespace Domain.Variant.ValueObjects;

public sealed class PriceRange : ValueObject
{
    public Money Min { get; }
    public Money Max { get; }

    private PriceRange(Money min, Money max)
    {
        Min = min;
        Max = max;
    }

    public static PriceRange Create(Money min, Money max)
    {
        Guard.Against.Null(min, nameof(min));
        Guard.Against.Null(max, nameof(max));

        if (min.Amount < 0)
            throw new DomainException("حداقل قیمت نمی‌تواند منفی باشد.");

        if (max.Amount < min.Amount)
            throw new DomainException("حداکثر قیمت نمی‌تواند کمتر از حداقل قیمت باشد.");

        return new PriceRange(min, max);
    }

    public static PriceRange Single(Money price)
    {
        Guard.Against.Null(price, nameof(price));
        return new PriceRange(price, price);
    }

    public bool IsSinglePrice => Min.Amount == Max.Amount;

    public bool Contains(Money price)
    {
        Guard.Against.Null(price, nameof(price));
        return price.Amount >= Min.Amount && price.Amount <= Max.Amount;
    }

    public string ToDisplayString()
    {
        if (IsSinglePrice)
            return Min.ToTomanString();

        return $"{Min.ToTomanString()} - {Max.ToTomanString()}";
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Min;
        yield return Max;
    }

    public override string ToString() => ToDisplayString();
}