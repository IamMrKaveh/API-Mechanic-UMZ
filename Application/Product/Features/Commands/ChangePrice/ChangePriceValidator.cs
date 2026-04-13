namespace Application.Product.Features.Commands.ChangePrice;

public sealed class ChangePriceValidator : AbstractValidator<ChangePriceCommand>
{
    public ChangePriceValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("شناسه محصول الزامی است.");

        RuleFor(x => x.VariantId)
            .NotEmpty().WithMessage("شناسه واریانت الزامی است.");

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0).WithMessage("قیمت خرید نمی‌تواند منفی باشد.");

        RuleFor(x => x.SellingPrice)
            .GreaterThanOrEqualTo(0).WithMessage("قیمت فروش نمی‌تواند منفی باشد.");

        RuleFor(x => x.OriginalPrice)
            .GreaterThanOrEqualTo(0).WithMessage("قیمت اصلی نمی‌تواند منفی باشد.");
    }
}