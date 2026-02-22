namespace Application.Inventory.Contracts;

public interface IStockLedgerService
{
    Task RecordStockInAsync(int variantId, int quantity, decimal unitCost,
        string? referenceNumber = null, string? note = null,
        int? warehouseId = null, int? userId = null, CancellationToken ct = default);

    Task RecordReservationAsync(int variantId, int quantity, string referenceNumber,
        string? correlationId = null, int? warehouseId = null, int? userId = null,
        int? orderItemId = null, CancellationToken ct = default);

    Task RecordReservationReleaseAsync(int variantId, int quantity, string referenceNumber,
        string? reason = null, int? warehouseId = null, CancellationToken ct = default);

    Task RecordReservationCommitAsync(int variantId, int quantity, string referenceNumber,
        int? orderItemId = null, int? warehouseId = null, CancellationToken ct = default);

    Task RecordAdjustmentAsync(int variantId, int delta, string reason,
        int? userId = null, int? warehouseId = null, CancellationToken ct = default);

    Task ReconcileAsync(int variantId, int physicalCount, string reason,
        int userId, int? warehouseId = null, CancellationToken ct = default);
}