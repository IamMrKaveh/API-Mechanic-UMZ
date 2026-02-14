namespace Application.Inventory.Features.Commands.BulkAdjustStock;

public class BulkAdjustStockValidator : AbstractValidator<BulkAdjustStockCommand>
{
    public BulkAdjustStockValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("لیست آیتم‌ها نمی‌تواند خالی باشد.");

        RuleFor(x => x.Items.Count)
            .LessThanOrEqualTo(100)
            .WithMessage("حداکثر 100 آیتم در هر درخواست مجاز است.");

        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("شناسه کاربر باید بزرگتر از صفر باشد.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.VariantId)
                .GreaterThan(0)
                .WithMessage("شناسه واریانت باید بزرگتر از صفر باشد.");

            item.RuleFor(i => i.QuantityChange)
                .NotEqual(0)
                .WithMessage("مقدار تغییر نمی‌تواند صفر باشد.");

            item.RuleFor(i => i.Notes)
                .NotEmpty()
                .WithMessage("توضیحات الزامی است.")
                .MaximumLength(500)
                .WithMessage("توضیحات نمی‌تواند بیش از 500 کاراکتر باشد.");
        });
    }
}