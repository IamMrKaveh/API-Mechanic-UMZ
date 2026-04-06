using Application.Common.Results;

namespace Application.Inventory.Features.Commands.BulkAdjustStock;

public record BulkAdjustStockCommand(
    List<StockAdjustmentItem> Items,
    Guid UserId,
    string Reason) : IRequest<ServiceResult>;

public record StockAdjustmentItem(Guid VariantId, int QuantityChange);