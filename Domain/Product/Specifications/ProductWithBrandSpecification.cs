using Domain.Brand.ValueObjects;

namespace Domain.Product.Specifications;

public class ProductWithBrandSpecification(BrandId brandId) : Specification<Aggregates.Product>
{
    private readonly BrandId _brandId = brandId;

    public override Expression<Func<Aggregates.Product, bool>> ToExpression()
    {
        return p => p.IsActive && p.BrandId == _brandId;
    }
}