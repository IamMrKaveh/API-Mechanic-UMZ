using Application.Common.Validation;

namespace Application.Inventory.Features.Commands.ReconcileStock;

public class ReconcileStockValidator : AbstractValidator<ReconcileStockCommand>
{
    public ReconcileStockValidator()
    {
        this.RuleForRequiredId(x => x.VariantId);
        RuleFor(x => x.CalculatedStock).GreaterThanOrEqualTo(0).WithMessage("موجودی محاسبه‌شده نمی‌تواند منفی باشد.");
    }
}