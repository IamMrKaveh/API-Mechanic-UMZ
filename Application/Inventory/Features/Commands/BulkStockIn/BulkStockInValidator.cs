using Application.Common.Validation;

namespace Application.Inventory.Features.Commands.BulkStockIn;

public class BulkStockInValidator : AbstractValidator<BulkStockInCommand>
{

    public BulkStockInValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.VariantId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
