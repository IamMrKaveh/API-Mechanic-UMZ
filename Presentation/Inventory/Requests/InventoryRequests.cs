using Application.Inventory.Features.Commands.BulkAdjustStock;
using Application.Inventory.Features.Commands.BulkStockIn;

namespace Presentation.Inventory.Requests;

public record ReverseInventoryTransactionRequest(
    Guid VariantId,
    string IdempotencyKey,
    string Reason
);

public record AdjustStockRequest(
    Guid VariantId,
    int QuantityChange,
    string Reason
);

public record BulkAdjustStockRequest(
    IReadOnlyList<BulkAdjustStockItem> Items,
    Guid UserId,
    string Reason
);

public record StockAdjustmentItemRequest(Guid VariantId, int QuantityChange);

public record RecordDamageRequest(
    Guid VariantId,
    int Quantity,
    string Reason
);

public record BulkStockInRequest(
    IReadOnlyList<BulkStockInItem> Items,
    Guid? UserId,
    string Reason
);

public record StockInItemRequest(Guid VariantId, int Quantity, string? Notes);

public record BatchAvailabilityRequest(IReadOnlyList<Guid> VariantIds);

public record BatchAvailabilityIntRequest(ICollection<Guid> VariantIds);

public record ApproveReturnRequest(string? Reason = null);