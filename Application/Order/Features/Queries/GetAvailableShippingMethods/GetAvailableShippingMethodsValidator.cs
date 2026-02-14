namespace Application.Order.Features.Queries.GetAvailableShippingMethods;

public class GetAvailableShippingMethodsValidator : AbstractValidator<GetAvailableShippingMethodsQuery>
{
    public GetAvailableShippingMethodsValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("UserId is required.");
    }
}