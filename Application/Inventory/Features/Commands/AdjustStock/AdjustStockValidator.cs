namespace Application.Inventory.Features.Commands.AdjustStock;

public class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.VariantId)
            .GreaterThan(0)
            .WithMessage("شناسه واریانت باید بزرگتر از صفر باشد.");

        RuleFor(x => x.QuantityChange)
            .NotEqual(0)
            .WithMessage("مقدار تغییر نمی‌تواند صفر باشد.");

        RuleFor(x => x.Notes)
            .NotEmpty()
            .WithMessage("توضیحات الزامی است.")
            .MaximumLength(500)
            .WithMessage("توضیحات نمی‌تواند بیش از 500 کاراکتر باشد.");

        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("شناسه کاربر باید بزرگتر از صفر باشد.");
    }
}