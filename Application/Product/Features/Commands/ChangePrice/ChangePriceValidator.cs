namespace Application.Product.Features.Commands.ChangePrice;

public sealed class ChangePriceValidator : AbstractValidator<ChangePriceCommand>
{
    public ChangePriceValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("شناسه محصول الزامی است.");

        RuleFor(x => x.VariantId)
            .NotEmpty().WithMessage("شناسه واریانت الزامی است.");

        RuleFor(x => x.SellingPrice)
            .GreaterThan(0).WithMessage("قیمت فروش باید بزرگتر از صفر باشد.");

        RuleFor(x => x.OriginalPrice)
            .GreaterThanOrEqualTo(0).WithMessage("قیمت اصلی نمی‌تواند منفی باشد.");
    }
}