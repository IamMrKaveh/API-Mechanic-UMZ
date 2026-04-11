using Domain.Inventory.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Inventory.Services;

public sealed class InventoryDomainService
{
    public static Result Reserve(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string referenceNumber,
        OrderItemId? orderItemId = null,
        UserId? userId = null,
        string? correlationId = null)
    {
        return inventory.ReserveStock(quantity, referenceNumber, orderItemId, userId, correlationId);
    }

    public static Result ConfirmReservation(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string referenceNumber,
        OrderItemId? orderItemId = null)
    {
        return inventory.ConfirmReservation(quantity, referenceNumber, orderItemId);
    }

    public static Result RollbackReservation(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string referenceNumber,
        string? reason = null)
    {
        return inventory.ReleaseReservation(quantity, referenceNumber, reason);
    }

    public static Result ReturnStock(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string reason,
        OrderItemId? orderItemId = null,
        UserId? userId = null)
    {
        return inventory.ReturnStock(quantity, reason, orderItemId, userId);
    }

    public static Result AdjustStock(
        Aggregates.Inventory inventory,
        StockQuantity quantityChange,
        UserId userId,
        string reason)
    {
        return inventory.AdjustStock(quantityChange, userId, reason);
    }

    public static Result RecordDamage(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        UserId userId,
        string reason)
    {
        return inventory.RecordDamage(quantity, userId, reason);
    }

    public static Result Reconcile(
        Aggregates.Inventory inventory,
        StockQuantity calculatedStockFromTransactions,
        UserId userId)
    {
        return inventory.Reconcile(calculatedStockFromTransactions, userId);
    }

    public static Result IncreaseStock(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string reason,
        UserId userId)
    {
        return inventory.IncreaseStock(quantity, reason, userId);
    }

    public static Result DecreaseStock(
        Aggregates.Inventory inventory,
        StockQuantity quantity,
        string reason,
        UserId userId)
    {
        return inventory.DecreaseStock(quantity, reason, userId);
    }
}