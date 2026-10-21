using Application.Common.Validation;

namespace Application.Inventory.Features.Commands.RemoveStock;

public class RemoveStockValidator : AbstractValidator<RemoveStockCommand>
{

    public RemoveStockValidator()
    {
        this.RuleForRequiredId(x => x.VariantId);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
