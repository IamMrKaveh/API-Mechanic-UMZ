namespace Application.Product.Features.Commands.BulkUpdatePrices;

public class BulkUpdatePricesValidator : AbstractValidator<BulkUpdatePricesCommand>
{
    public BulkUpdatePricesValidator()
    {
        RuleFor(x => x.Updates).NotEmpty().WithMessage("No price updates provided.");
        RuleForEach(x => x.Updates).ChildRules(u =>
        {
            u.RuleFor(x => x.ProductId).GreaterThan(0);
            u.RuleFor(x => x.VariantId).GreaterThan(0);
            u.RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0);
            u.RuleFor(x => x.OriginalPrice).GreaterThanOrEqualTo(0);
            u.RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        });
    }
}