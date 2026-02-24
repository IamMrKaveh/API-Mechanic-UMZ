namespace Domain.Product.Specifications;

public class FeaturedProductSpecification : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.IsActive && !p.IsDeleted && p.IsFeatured;
    }
}