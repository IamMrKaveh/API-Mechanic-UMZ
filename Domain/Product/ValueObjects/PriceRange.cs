namespace Domain.Product.ValueObjects;

public sealed class PriceRange : ValueObject
{
    public Money MinPrice { get; }
    public Money MaxPrice { get; }

    private PriceRange(Money minPrice, Money maxPrice)
    {
        MinPrice = minPrice;
        MaxPrice = maxPrice;
    }

    public static PriceRange Create(Money minPrice, Money maxPrice)
    {
        Guard.Against.Null(minPrice, nameof(minPrice));
        Guard.Against.Null(maxPrice, nameof(maxPrice));

        if (minPrice.Amount > maxPrice.Amount)
            throw new DomainException("حداقل قیمت نمی‌تواند بیشتر از حداکثر قیمت باشد.");

        return new PriceRange(minPrice, maxPrice);
    }

    public static PriceRange FromVariants(IEnumerable<ProductVariant> variants)
    {
        var activeVariants = variants.Where(v => !v.IsDeleted && v.IsActive).ToList();

        if (!activeVariants.Any())
            return new PriceRange(Money.Zero(), Money.Zero());

        var min = activeVariants.Min(v => v.SellingPrice);
        var max = activeVariants.Max(v => v.SellingPrice);

        return new PriceRange(Money.FromDecimal(min), Money.FromDecimal(max));
    }

    public bool IsSinglePrice => MinPrice.Amount == MaxPrice.Amount;

    public Money GetDifference() => MaxPrice.Subtract(MinPrice);

    public bool Contains(Money price)
    {
        return price.Amount >= MinPrice.Amount && price.Amount <= MaxPrice.Amount;
    }

    public string ToDisplayString()
    {
        if (IsSinglePrice)
            return MinPrice.ToTomanString();

        return $"{MinPrice.ToTomanString()} تا {MaxPrice.ToTomanString()}";
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return MinPrice;
        yield return MaxPrice;
    }

    public override string ToString() => ToDisplayString();
}