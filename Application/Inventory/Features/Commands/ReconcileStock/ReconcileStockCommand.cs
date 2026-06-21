namespace Application.Inventory.Features.Commands.ReconcileStock;

public record ReconcileStockCommand(
    Guid VariantId,
    int CalculatedStock) : ICommand;