using Application.Common.Validation;

namespace Application.Product.Features.Commands.ChangePrice;

public sealed class ChangePriceValidator : AbstractValidator<ChangePriceCommand>
{
    public ChangePriceValidator()
    {
        this.RuleForRequiredId(x => x.ProductId, "شناسه محصول الزامی است.");

        this.RuleForRequiredId(x => x.VariantId, "شناسه واریانت الزامی است.");

        RuleFor(x => x.SellingPrice)
            .GreaterThan(0).WithMessage("قیمت فروش باید بزرگتر از صفر باشد.");

        RuleFor(x => x.OriginalPrice)
            .GreaterThanOrEqualTo(0).WithMessage("قیمت اصلی نمی‌تواند منفی باشد.");
    }
}