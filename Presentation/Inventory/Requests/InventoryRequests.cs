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
    IReadOnlyList<BulkAdjustStockItemRequest> Items,
    string Reason
);

public record BulkAdjustStockItemRequest(Guid VariantId, int QuantityChange);

public record RecordDamageRequest(
    Guid VariantId,
    int Quantity,
    string Reason
);

public record BulkStockInRequest(
    IReadOnlyList<BulkStockInItemRequest> Items,
    string Reason
);

public record BulkStockInItemRequest(Guid VariantId, int Quantity, string? Notes);

public record BatchAvailabilityRequest(ICollection<Guid> VariantIds);

public record ApproveReturnRequest(string? Reason = null);