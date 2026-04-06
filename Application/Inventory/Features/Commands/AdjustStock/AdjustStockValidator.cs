namespace Application.Inventory.Features.Commands.AdjustStock;

public class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.QuantityChange).NotEqual(0).WithMessage("تغییر موجودی نمی‌تواند صفر باشد.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("دلیل تغییر موجودی الزامی است.");
        RuleFor(x => x.UserId).NotEmpty();
    }
}