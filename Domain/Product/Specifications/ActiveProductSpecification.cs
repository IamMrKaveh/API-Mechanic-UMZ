namespace Domain.Product.Specifications;

public class ActiveProductSpecification : Specification<Product>
{
    public override Expression<Func<Product, bool>> ToExpression()
    {
        return p => p.IsActive && !p.IsDeleted;
    }
}