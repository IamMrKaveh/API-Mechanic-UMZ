using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Contracts;

public interface IInventoryService
{
    Task<ServiceResult> ReserveStockAsync(
        VariantId variantId,
        int quantity,
        string referenceNumber,
        OrderItemId? orderItemId = null,
        CancellationToken ct = default);

    Task<ServiceResult> ReleaseReservationAsync(
        VariantId variantId,
        int quantity,
        string referenceNumber,
        string? reason = null,
        CancellationToken ct = default);

    Task<ServiceResult> CommitReservationAsync(
        VariantId variantId,
        int quantity,
        string referenceNumber,
        Guid? orderItemId = null,
        CancellationToken ct = default);

    Task<ServiceResult> IncreaseStockAsync(
        VariantId variantId,
        int quantity,
        string reason,
        UserId? userId = null,
        CancellationToken ct = default);

    Task<ServiceResult> AdjustStockAsync(
        VariantId variantId,
        int quantityChange,
        UserId userId,
        string reason,
        CancellationToken ct = default);

    Task<ServiceResult> ReturnStockAsync(
        VariantId variantId,
        int quantity,
        string reason,
        OrderItemId? orderItemId = null,
        UserId? userId = null,
        CancellationToken ct = default);
}