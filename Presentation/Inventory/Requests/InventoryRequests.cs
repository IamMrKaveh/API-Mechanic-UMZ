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
    IReadOnlyList<StockInItemRequest> Items,
    string? SupplierReference = null
);

public record StockInItemRequest(Guid VariantId, int Quantity, string? Notes);

public record BatchAvailabilityRequest(IReadOnlyList<Guid> VariantIds);

public record BatchAvailabilityIntRequest(IReadOnlyList<int> VariantIds);

public record ApproveReturnRequest(string? Reason = null);