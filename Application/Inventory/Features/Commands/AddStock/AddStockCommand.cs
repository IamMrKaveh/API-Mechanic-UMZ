namespace Application.Inventory.Features.Commands.AddStock;

public record AddStockCommand(
    Guid VariantId,
    int Quantity,
    string Notes) : ICommand;