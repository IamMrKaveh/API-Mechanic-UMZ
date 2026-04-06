using Application.Common.Results;

namespace Application.Inventory.Contracts;

public interface IInventoryService
{
    Task<ServiceResult> ReserveStockAsync(
        Guid variantId,
        int quantity,
        string referenceNumber,
        Guid? orderItemId = null,
        CancellationToken ct = default);

    Task<ServiceResult> ReleaseReservationAsync(
        Guid variantId,
        int quantity,
        string referenceNumber,
        string? reason = null,
        CancellationToken ct = default);

    Task<ServiceResult> CommitReservationAsync(
        Guid variantId,
        int quantity,
        string referenceNumber,
        Guid? orderItemId = null,
        CancellationToken ct = default);

    Task<ServiceResult> IncreaseStockAsync(
        Guid variantId,
        int quantity,
        string reason,
        Guid? userId = null,
        CancellationToken ct = default);

    Task<ServiceResult> AdjustStockAsync(
        Guid variantId,
        int quantityChange,
        Guid userId,
        string reason,
        CancellationToken ct = default);

    Task<ServiceResult> ReturnStockAsync(
        Guid variantId,
        int quantity,
        string reason,
        Guid? orderItemId = null,
        Guid? userId = null,
        CancellationToken ct = default);
}