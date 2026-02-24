namespace Domain.Variant.Specifications;

public class ActiveVariantSpecification : Specification<ProductVariant>
{
    public override Expression<Func<ProductVariant, bool>> ToExpression()
    {
        return v => v.IsActive && !v.IsDeleted;
    }
}