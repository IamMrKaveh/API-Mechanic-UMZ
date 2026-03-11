namespace Domain.Product.Specifications;

public class ProductInPriceRangeSpecification : Specification<ProductVariant>
{
    private readonly decimal _minPrice;
    private readonly decimal _maxPrice;

    public ProductInPriceRangeSpecification(decimal minPrice, decimal maxPrice)
    {
        if (minPrice < 0)
            throw new DomainException("Minimum price cannot be negative.");

        if (maxPrice < minPrice)
            throw new DomainException("Maximum price cannot be less than minimum price.");

        _minPrice = minPrice;
        _maxPrice = maxPrice;
    }

    public override Expression<Func<ProductVariant, bool>> ToExpression()
    {
        return v => v.IsActive && v.Price.Amount >= _minPrice && v.Price.Amount <= _maxPrice;
    }
}