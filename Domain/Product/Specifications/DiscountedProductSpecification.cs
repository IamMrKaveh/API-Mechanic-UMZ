namespace Domain.Product.Specifications;

public class DiscountedProductSpecification : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.IsActive &&
                    !p.IsDeleted &&
                    p.Variants.Any(v => !v.IsDeleted && v.OriginalPrice > v.SellingPrice);
    }
}