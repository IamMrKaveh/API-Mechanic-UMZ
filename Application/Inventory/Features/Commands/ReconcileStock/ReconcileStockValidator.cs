namespace Application.Inventory.Features.Commands.ReconcileStock;

public class ReconcileStockValidator : AbstractValidator<ReconcileStockCommand>
{
    public ReconcileStockValidator()
    {
        RuleFor(x => x.VariantId)
            .GreaterThan(0)
            .WithMessage("شناسه واریانت باید بزرگتر از صفر باشد.");

        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("شناسه کاربر باید بزرگتر از صفر باشد.");
    }
}