namespace Domain.Variant.Specifications;

public class ActiveProductVariantSpecification : Specification<ProductVariant>
{
    public override Expression<Func<ProductVariant, bool>> ToExpression()
    {
        return v => v.IsActive && !v.IsDeleted && v.Product.IsActive && !v.Product.IsDeleted;
    }
}