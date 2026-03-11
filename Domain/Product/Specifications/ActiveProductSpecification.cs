namespace Domain.Product.Specifications;

public class ActiveProductSpecification : Specification<Aggregates.Product>
{
    public override Expression<Func<Aggregates.Product, bool>> ToExpression()
    {
        return p => p.IsActive;
    }
}