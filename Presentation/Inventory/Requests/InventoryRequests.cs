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
    ICollection<Guid> VariantId,
    ICollection<int> QuantityChange,
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
    ICollection<Guid> VariantIds,
    ICollection<int> Quantities,
    ICollection<string>? ReferenceNumbers,
    Guid? UserId,
    string Reason
);

public record StockInItemRequest(Guid VariantId, int Quantity, string? Notes);

public record BatchAvailabilityRequest(IReadOnlyList<Guid> VariantIds);

public record BatchAvailabilityIntRequest(ICollection<Guid> VariantIds);

public record ApproveReturnRequest(string? Reason = null);