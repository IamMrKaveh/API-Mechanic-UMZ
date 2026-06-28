namespace Application.Shipping.Features.Queries.GetShippingQuotes;

public class GetShippingQuotesValidator : AbstractValidator<GetShippingQuotesQuery>
{
    public GetShippingQuotesValidator()
    {
        RuleFor(x => x.OrderAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Items).NotNull();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.VariantId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}