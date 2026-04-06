namespace Application.Inventory.Features.Commands.BulkAdjustStock;

public class BulkAdjustStockValidator : AbstractValidator<BulkAdjustStockCommand>
{
    public BulkAdjustStockValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("لیست آیتم‌ها نمی‌تواند خالی باشد.");
        RuleFor(x => x.Reason).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.VariantId).NotEmpty();
            item.RuleFor(x => x.QuantityChange).NotEqual(0);
        });
    }
}