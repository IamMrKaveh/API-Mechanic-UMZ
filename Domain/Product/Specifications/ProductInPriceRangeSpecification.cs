namespace Domain.Product.Specifications;

public class ProductInPriceRangeSpecification : Specification<Product>
{
    private readonly decimal _minPrice;
    private readonly decimal _maxPrice;

    public ProductInPriceRangeSpecification(decimal minPrice, decimal maxPrice)
    {
        _minPrice = minPrice;
        _maxPrice = maxPrice;
    }

    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.IsActive &&
                    !p.IsDeleted &&
                    p.MinPrice.Amount >= _minPrice &&
                    p.MaxPrice.Amount <= _maxPrice;
    }
}