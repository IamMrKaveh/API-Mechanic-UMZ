namespace Application.Product.Features.Commands.UpdateVariant;

public class UpdateVariantValidator : AbstractValidator<UpdateVariantCommand>
{
    public UpdateVariantValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.VariantId).GreaterThan(0);
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OriginalPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).When(x => !x.IsUnlimited);
        RuleFor(x => x.ShippingMultiplier).InclusiveBetween(0.1m, 10m);
    }
}