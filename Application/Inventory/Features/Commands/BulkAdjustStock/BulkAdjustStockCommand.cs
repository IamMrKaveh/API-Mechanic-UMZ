namespace Application.Inventory.Features.Commands.BulkAdjustStock;

public record BulkAdjustStockCommand(
    IReadOnlyList<BulkAdjustStockItem> Items,
    string Reason)
    : ICommand, IManualTransactionRequest;

public record BulkAdjustStockItem(Guid VariantId, int QuantityChange);