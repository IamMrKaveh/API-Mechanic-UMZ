namespace Application.Shipping.Features.Queries.GetAvailableShippings;

public class GetAvailableShippingsValidator : AbstractValidator<GetAvailableShippingsQuery>
{
    public GetAvailableShippingsValidator()
    {
        RuleFor(x => x.OrderAmount).GreaterThanOrEqualTo(0);
    }
}