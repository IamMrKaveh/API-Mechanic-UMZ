using Domain.Inventory.Aggregates;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;

namespace Domain.Inventory.Services;

public sealed class InventoryDomainService
{
    public Result Reserve(
        Aggregates.Inventory inventory,
        int quantity,
        string referenceNumber,
        OrderItemId? orderItemId = null,
        UserId? userId = null,
        string? correlationId = null)
    {
        return inventory.ReserveStock(quantity, referenceNumber, orderItemId, userId, correlationId);
    }

    public Result ConfirmReservation(
        Aggregates.Inventory inventory,
        int quantity,
        string referenceNumber,
        OrderItemId? orderItemId = null)
    {
        return inventory.ConfirmReservation(quantity, referenceNumber, orderItemId);
    }

    public Result RollbackReservation(
        Aggregates.Inventory inventory,
        int quantity,
        string referenceNumber,
        string? reason = null)
    {
        return inventory.ReleaseReservation(quantity, referenceNumber, reason);
    }

    public Result ReturnStock(
        Aggregates.Inventory inventory,
        int quantity,
        string reason,
        OrderItemId? orderItemId = null,
        UserId? userId = null)
    {
        return inventory.ReturnStock(quantity, reason, orderItemId, userId);
    }

    public Result AdjustStock(
        Aggregates.Inventory inventory,
        int quantityChange,
        UserId userId,
        string reason)
    {
        return inventory.AdjustStock(quantityChange, userId, reason);
    }

    public Result RecordDamage(
        Aggregates.Inventory inventory,
        int quantity,
        UserId userId,
        string reason)
    {
        return inventory.RecordDamage(quantity, userId, reason);
    }

    public Result Reconcile(
        Aggregates.Inventory inventory,
        int calculatedStockFromTransactions,
        UserId userId)
    {
        return inventory.Reconcile(calculatedStockFromTransactions, userId);
    }

    public Result IncreaseStock(
        Aggregates.Inventory inventory,
        int quantity,
        string reason,
        UserId userId)
    {
        return inventory.IncreaseStock(quantity, reason, userId);
    }

    public Result DecreaseStock(
        Aggregates.Inventory inventory,
        int quantity,
        string reason,
        UserId userId)
    {
        return inventory.DecreaseStock(quantity, reason, userId);
    }
}