namespace Application.Inventory.Features.Commands.BulkAdjustStock;

public record BulkAdjustStockCommand(
    IReadOnlyList<BulkAdjustStockItem> Items,
    string Reason)
    : IRequest<ServiceResult>, IManualTransactionRequest;

public record BulkAdjustStockItem(Guid VariantId, int QuantityChange);