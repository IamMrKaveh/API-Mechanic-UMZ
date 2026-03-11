namespace Domain.Shipping.Specifications;

public class DefaultShippingSpecification : Specification<Aggregates.Shipping>
{
    public override Expression<Func<Aggregates.Shipping, bool>> ToExpression()
    {
        return s => s.IsActive && s.IsDefault;
    }
}