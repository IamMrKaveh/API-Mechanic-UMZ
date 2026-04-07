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
    IReadOnlyList<StockAdjustmentItemRequest> Items,
    string Reason
);

public record StockAdjustmentItemRequest(Guid VariantId, int QuantityChange);

public record RecordDamageRequest(
    Guid VariantId,
    int Quantity,
    string Reason
);

public record BulkStockInRequest(
    IReadOnlyList<StockInItemRequest> Items
);

public record StockInItemRequest(Guid VariantId, int Quantity, string Reason);

public record BatchAvailabilityRequest(IReadOnlyList<BatchAvailabilityItem> Items);

public record BatchAvailabilityItem(Guid VariantId, int RequestedQuantity);

public record ApproveReturnRequest(string? Reason = null);