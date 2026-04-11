namespace Application.Inventory.Features.Commands.BulkAdjustStock;

public record BulkAdjustStockCommand(
    IReadOnlyList<BulkAdjustStockItem> Items,
    Guid UserId,
    string Reason) : IRequest<ServiceResult>;

public record BulkAdjustStockItem(Guid VariantId, int QuantityChange);