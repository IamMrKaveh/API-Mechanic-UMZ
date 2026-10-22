using Domain.Inventory.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Contracts;

public interface IInventoryService
{
    Task<ServiceResult> ReserveStockAsync(
        VariantId variantId,
        StockQuantity quantity,
        string referenceNumber,
        OrderItemId? orderItemId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Reserves the stock of every line of an order in one go. When a line cannot
    /// be reserved, the lines that were already reserved are released again, so a
    /// partial reservation never survives.
    /// </summary>
    Task<ServiceResult> ReserveStockForOrderAsync(
        IReadOnlyCollection<StockReservationRequest> items,
        string referenceNumber,
        CancellationToken ct = default);

    Task<ServiceResult> ReleaseReservationAsync(
        VariantId variantId,
        StockQuantity quantity,
        string referenceNumber,
        string? reason = null,
        CancellationToken ct = default);

    Task<ServiceResult> AdjustStockAsync(
        VariantId variantId,
        StockQuantity quantityChange,
        UserId userId,
        string reason,
        CancellationToken ct = default);

    Task<ServiceResult> ReturnStockForOrderAsync(
        OrderId orderId,
        Guid adminUserId,
        string reason,
        CancellationToken ct = default);

    Task<ServiceResult> RollbackReservationsAsync(
        string referenceNumber,
        CancellationToken ct = default);
}