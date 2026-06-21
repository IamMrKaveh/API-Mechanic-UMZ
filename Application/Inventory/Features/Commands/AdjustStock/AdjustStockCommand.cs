namespace Application.Inventory.Features.Commands.AdjustStock;

public record AdjustStockCommand(
    Guid VariantId,
    int QuantityChange,
    string Reason) : ICommand;