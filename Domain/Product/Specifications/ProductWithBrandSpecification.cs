namespace Domain.Product.Specifications;

public class ProductWithBrandSpecification : Specification<Aggregates.Product>
{
    private readonly BrandId _brandId;

    public ProductWithBrandSpecification(BrandId brandId)
    {
        _brandId = brandId;
    }

    public override Expression<Func<Aggregates.Product, bool>> ToExpression()
    {
        return p => p.IsActive && p.BrandId == _brandId;
    }
}