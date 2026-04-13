namespace Application.Product.Features.Commands.BulkUpdatePrices;

public sealed class BulkUpdatePricesValidator : AbstractValidator<BulkUpdatePricesCommand>
{
    public BulkUpdatePricesValidator()
    {
        RuleFor(x => x.Updates)
            .NotEmpty().WithMessage("لیست بروزرسانی قیمت‌ها خالی است.");

        RuleForEach(x => x.Updates).ChildRules(u =>
        {
            u.RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("شناسه محصول الزامی است.");

            u.RuleFor(x => x.VariantId)
                .NotEmpty().WithMessage("شناسه واریانت الزامی است.");

            u.RuleFor(x => x.SellingPrice)
                .GreaterThanOrEqualTo(0).WithMessage("قیمت فروش نمی‌تواند منفی باشد.");

            u.RuleFor(x => x.OriginalPrice)
                .GreaterThanOrEqualTo(0).WithMessage("قیمت اصلی نمی‌تواند منفی باشد.");

            u.RuleFor(x => x.PurchasePrice)
                .GreaterThanOrEqualTo(0).WithMessage("قیمت خرید نمی‌تواند منفی باشد.");
        });
    }
}