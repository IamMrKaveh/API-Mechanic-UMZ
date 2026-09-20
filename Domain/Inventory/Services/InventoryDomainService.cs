using Domain.Inventory.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Inventory.Services;

public sealed class InventoryDomainService
{
    public static ServiceResult Reserve(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string referenceNumber,
        DateTime now,
        OrderItemId? orderItemId = null,
        UserId? userId = null,
        string? correlationId = null)
    {
        return inventory.ReserveStock(quantity, referenceNumber, now, orderItemId, userId, correlationId);
    }

    public static ServiceResult ConfirmReservation(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string referenceNumber,
        DateTime now,
        OrderItemId? orderItemId = null)
    {
        return inventory.ConfirmReservation(quantity, referenceNumber, now, orderItemId);
    }

    public static ServiceResult RollbackReservation(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string referenceNumber,
        DateTime now,
        string? reason = null)
    {
        return inventory.ReleaseReservation(quantity, referenceNumber, now, reason);
    }

    public static ServiceResult ReturnStock(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string reason,
        DateTime now,
        UserId? userId = null)
    {
        return inventory.ReturnStock(quantity, reason, now, userId);
    }

    public static ServiceResult AdjustStock(
        Aggregates.Inventory inventory,
        StockQuantity quantityChange,
        UserId userId,
        string reason,
        DateTime now)
    {
        return inventory.AdjustStock(quantityChange, userId, reason, now);
    }

    public static ServiceResult RecordDamage(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        UserId userId,
        string reason,
        DateTime now)
    {
        return inventory.RecordDamage(quantity, userId, reason, now);
    }

    public static ServiceResult Reconcile(
        Aggregates.Inventory inventory,
        StockQuantity calculatedStockFromTransactions,
        UserId userId,
        DateTime now)
    {
        return inventory.Reconcile(calculatedStockFromTransactions, userId, now);
    }

    public static ServiceResult IncreaseStock(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string reason,
        DateTime now,
        UserId? userId = null,
        string? referenceNumber = null)
    {
        return inventory.IncreaseStock(quantity, reason, now, userId, referenceNumber);
    }

    public static ServiceResult DecreaseStock(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string reason,
        DateTime now,
        UserId? userId = null,
        string? referenceNumber = null)
    {
        return inventory.DecreaseStock(quantity, reason, now, userId, referenceNumber);
    }
}
