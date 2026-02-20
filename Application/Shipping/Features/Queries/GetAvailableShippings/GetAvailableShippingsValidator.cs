namespace Application.Shipping.Features.Queries.GetAvailableShippings;

public class GetAvailableShippingsValidator : AbstractValidator<GetAvailableShippingsQuery>
{
    public GetAvailableShippingsValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("UserId is required.");
    }
}