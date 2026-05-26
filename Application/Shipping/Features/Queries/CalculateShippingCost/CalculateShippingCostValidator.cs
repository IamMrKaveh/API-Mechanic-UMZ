namespace Application.Shipping.Features.Queries.CalculateShippingCost;

public class CalculateShippingCostValidator : AbstractValidator<CalculateShippingCostQuery>
{
    public CalculateShippingCostValidator()
    {
        RuleFor(x => x.ShippingId).NotEmpty();
        RuleFor(x => x.OrderAmount).GreaterThanOrEqualTo(0);
    }
}