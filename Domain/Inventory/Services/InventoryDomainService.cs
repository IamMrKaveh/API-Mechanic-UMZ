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
        OrderItemId? orderItemId = null,
        UserId? userId = null,
        string? correlationId = null)
    {
        return inventory.ReserveStock(quantity, referenceNumber, orderItemId, userId, correlationId);
    }

    public static ServiceResult ConfirmReservation(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string referenceNumber,
        OrderItemId? orderItemId = null)
    {
        return inventory.ConfirmReservation(quantity, referenceNumber, orderItemId);
    }

    public static ServiceResult RollbackReservation(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string referenceNumber,
        string? reason = null)
    {
        return inventory.ReleaseReservation(quantity, referenceNumber, reason);
    }

    public static ServiceResult ReturnStock(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string reason,
        UserId? userId = null)
    {
        return inventory.ReturnStock(quantity, reason, userId);
    }

    public static ServiceResult AdjustStock(
        Aggregates.Inventory inventory,
        StockQuantity quantityChange,
        UserId userId,
        string reason)
    {
        return inventory.AdjustStock(quantityChange, userId, reason);
    }

    public static ServiceResult RecordDamage(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        UserId userId,
        string reason)
    {
        return inventory.RecordDamage(quantity, userId, reason);
    }

    public static ServiceResult Reconcile(
        Aggregates.Inventory inventory,
        StockQuantity calculatedStockFromTransactions,
        UserId userId)
    {
        return inventory.Reconcile(calculatedStockFromTransactions, userId);
    }

    public static ServiceResult IncreaseStock(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string reason,
        UserId userId)
    {
        return inventory.IncreaseStock(quantity, reason, userId);
    }

    public static ServiceResult DecreaseStock(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string reason,
        UserId userId)
    {
        return inventory.DecreaseStock(quantity, reason, userId);
    }
}