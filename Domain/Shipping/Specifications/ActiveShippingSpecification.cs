namespace Domain.Shipping.Specifications;

public class ActiveShippingSpecification : Specification<Aggregates.Shipping>
{
    public override Expression<Func<Aggregates.Shipping, bool>> ToExpression()
    {
        return s => s.IsActive;
    }
}