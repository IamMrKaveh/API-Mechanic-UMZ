using Application.Common.Validation;

namespace Application.Inventory.Features.Commands.CommitStockForOrder;

public class CommitStockForOrderValidator : AbstractValidator<CommitStockForOrderCommand>
{

    public CommitStockForOrderValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.VariantId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
        RuleFor(x => x.OrderNumber).NotEmpty().MaximumLength(50);
    }
}
