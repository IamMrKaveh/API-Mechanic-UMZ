using Domain.Variant.Aggregates;

namespace Domain.Product.Specifications;

public class DiscountedProductSpecification : Specification<ProductVariant>
{
    public override Expression<Func<ProductVariant, bool>> ToExpression()
    {
        return v => v.IsActive && v.CompareAtPrice != null && v.CompareAtPrice.Amount > v.Price.Amount;
    }
}