namespace Application.Shipping.Features.Queries.CalculateShippingCost;

public class CalculateShippingCostValidator : AbstractValidator<CalculateShippingCostQuery>
{
    public CalculateShippingCostValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("UserId is required.");

        RuleFor(x => x.ShippingMethodId)
            .GreaterThan(0)
            .WithMessage("ShippingMethodId is required.");
    }
}