using Application.Common.Validation;

namespace Application.Variant.Features.Commands.UpdateVariant;

public class UpdateVariantValidator : AbstractValidator<UpdateVariantCommand>
{
    public UpdateVariantValidator()
    {
        this.RuleForRequiredId(x => x.ProductId);
        this.RuleForRequiredId(x => x.VariantId);
        RuleFor(x => x.SellingPrice)
            .GreaterThan(0)
            .WithMessage("قیمت فروش واریانت باید بزرگتر از صفر باشد.");
        RuleFor(x => x.OriginalPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0).When(x => !x.IsUnlimited);
        RuleFor(x => x.ShippingMultiplier).InclusiveBetween(0.1m, 10m);
    }
}