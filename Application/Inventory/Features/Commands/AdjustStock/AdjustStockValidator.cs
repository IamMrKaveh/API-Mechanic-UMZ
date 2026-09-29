using Application.Common.Validation;

namespace Application.Inventory.Features.Commands.AdjustStock;

public class AdjustStockValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockValidator()
    {
        this.RuleForRequiredId(x => x.VariantId);
        RuleFor(x => x.QuantityChange).NotEqual(0).WithMessage("تغییر موجودی نمی‌تواند صفر باشد.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("دلیل تغییر موجودی الزامی است.");
    }
}