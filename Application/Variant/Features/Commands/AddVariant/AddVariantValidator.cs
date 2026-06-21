namespace Application.Variant.Features.Commands.AddVariant;

public class AddVariantValidator : AbstractValidator<AddVariantCommand>
{
    public AddVariantValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.SellingPrice)
            .GreaterThan(0)
            .WithMessage("قیمت فروش واریانت باید بزرگتر از صفر باشد.");
        RuleFor(x => x.OriginalPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).When(x => !x.IsUnlimited);
        RuleFor(x => x.ShippingMultiplier).InclusiveBetween(0.1m, 10m);
        RuleFor(x => x.OriginalPrice)
            .GreaterThanOrEqualTo(x => x.SellingPrice)
            .When(x => x.OriginalPrice > 0)
            .WithMessage("قیمت اصلی نمی‌تواند کمتر از قیمت فروش باشد.");
    }
}