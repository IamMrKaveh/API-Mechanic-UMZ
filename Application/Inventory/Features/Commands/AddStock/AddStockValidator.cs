using Application.Common.Validation;

namespace Application.Inventory.Features.Commands.AddStock;

public class AddStockValidator : AbstractValidator<AddStockCommand>
{

    public AddStockValidator()
    {
        this.RuleForRequiredId(x => x.VariantId);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
