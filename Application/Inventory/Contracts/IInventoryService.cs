namespace Application.Inventory.Contracts;

public interface IInventoryService
{
    Task<ServiceResult> ReserveStockAsync(
        int variantId,
        int quantity,
        int orderItemId,
        int? userId = null,
        string? referenceNumber = null,
        string? correlationId = null,
        string? cartId = null,
        DateTime? expiresAt = null,
        CancellationToken ct = default
        );

    Task<ServiceResult> ConfirmReservationAsync(
        int variantId,
        int quantity,
        int orderItemId,
        int? userId = null,
        string? referenceNumber = null,
        string? correlationId = null,
        CancellationToken ct = default
        );

    Task<ServiceResult> CommitStockForOrderAsync(
        int orderId,
        CancellationToken ct = default
        );

    Task<ServiceResult> RollbackReservationAsync(
        int variantId,
        int quantity,
        int? userId = null,
        string? reason = null,
        CancellationToken ct = default
        );

    Task<ServiceResult> RollbackReservationsAsync(
        string referenceNumber,
        CancellationToken ct = default
        );

    Task<ServiceResult> ReturnStockAsync(
        int variantId,
        int quantity,
        int orderId,
        int orderItemId,
        int userId,
        string reason,
        CancellationToken ct = default
        );

    Task<ServiceResult> ReturnStockForOrderAsync(
        int orderId,
        int userId,
        string reason,
        CancellationToken ct = default
        );

    Task<ServiceResult> AdjustStockAsync(
        int variantId,
        int quantityChange,
        int userId,
        string notes,
        CancellationToken ct = default
        );

    Task<ServiceResult> RecordDamageAsync(
        int variantId,
        int quantity,
        int userId,
        string notes,
        CancellationToken ct = default
        );

    Task<ServiceResult<(int VariantId, int FinalStock, int Difference, bool HasDiscrepancy, string Message)>> ReconcileStockAsync(
        int variantId,
        int userId,
        CancellationToken ct = default
        );

    Task<ServiceResult<(int Total, int Success, int Failed, IEnumerable<(int VariantId, bool IsSuccess, string? Error, int? NewStock)> Results)>> BulkAdjustStockAsync(
        IEnumerable<(int VariantId, int QuantityChange, string Notes)> items,
        int userId,
        CancellationToken ct = default
        );

    Task<ServiceResult<(int Total, int Success, int Failed, IEnumerable<(int VariantId, bool IsSuccess, string? Error, int? NewStock)> Results)>> BulkStockInAsync(
        IEnumerable<(int VariantId, int Quantity, string? Notes)> items,
        int userId,
        string? supplierReference = null,
        CancellationToken ct = default
        );

    Task LogTransactionAsync(
        int variantId,
        string transactionType,
        int quantityChange,
        int? orderItemId,
        int? userId,
        string? notes = null,
        string? referenceNumber = null,
        int? stockBefore = null,
        bool saveChanges = true,
        CancellationToken ct = default
        );

    Task ReconcileAsync(
        int variantId,
        int physicalCount,
        string reason,
        int userId,
        int? warehouseId = null,
        CancellationToken ct = default
        );
}