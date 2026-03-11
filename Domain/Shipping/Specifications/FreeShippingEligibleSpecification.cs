namespace Domain.Shipping.Specifications;

public class FreeShippingEligibleSpecification(decimal orderAmount) : Specification<Aggregates.Shipping>
{
    private readonly decimal _orderAmount = orderAmount;

    public override Expression<Func<Aggregates.Shipping, bool>> ToExpression()
    {
        return s => s.IsActive
                    && s.FreeShipping.IsEnabled
                    && s.FreeShipping.ThresholdAmount != null;
    }
}