namespace Application.Inventory.Features.Commands.ReconcileStock;

public class ReconcileStockValidator : AbstractValidator<ReconcileStockCommand>
{
    public ReconcileStockValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty();
        RuleFor(x => x.CalculatedStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UserId).NotEmpty();
    }
}