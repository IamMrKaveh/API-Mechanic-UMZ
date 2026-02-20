namespace Application.Variant.Features.Commands.AddVariant;

public class AddVariantValidator : AbstractValidator<AddVariantCommand>
{
    public AddVariantValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OriginalPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PurchasePrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).When(x => !x.IsUnlimited);
        RuleFor(x => x.ShippingMultiplier).InclusiveBetween(0.1m, 10m);
    }
}