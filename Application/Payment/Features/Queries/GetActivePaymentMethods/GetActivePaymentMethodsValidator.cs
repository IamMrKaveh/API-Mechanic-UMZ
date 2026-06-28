namespace Application.Payment.Features.Queries.GetActivePaymentMethods;

public sealed class GetActivePaymentMethodsValidator : AbstractValidator<GetActivePaymentMethodsQuery>
{
    public GetActivePaymentMethodsValidator()
    {
        RuleFor(x => x.OrderAmount).GreaterThanOrEqualTo(0);
    }
}