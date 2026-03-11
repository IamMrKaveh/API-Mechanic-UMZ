using Domain.Order.Results;

namespace Domain.Order.Services;

public sealed class OrderDomainService
{
    public Aggregates.Order PlaceOrder(
        Guid userId,
        ReceiverInfo receiverInfo,
        DeliveryAddress deliveryAddress,
        Money shippingCost,
        Money discountAmount,
        Guid? appliedDiscountCodeId,
        IEnumerable<OrderItemSnapshot> itemSnapshots,
        Guid idempotencyKey)
    {
        var order = Aggregates.Order.Place(
            userId,
            receiverInfo,
            deliveryAddress,
            shippingCost,
            discountAmount,
            appliedDiscountCodeId,
            itemSnapshots,
            idempotencyKey);

        return order;
    }

    public Money CalculateOrderTotals(
        IEnumerable<OrderItemSnapshot> items,
        Money shippingCost,
        Money discountAmount)
    {
        var snapshots = items.ToList();

        if (!snapshots.Any())
            return Money.Zero();

        var subTotal = snapshots
            .Skip(1)
            .Aggregate(
                snapshots.First().UnitPrice.Multiply(snapshots.First().Quantity),
                (acc, item) => acc.Add(item.UnitPrice.Multiply(item.Quantity)));

        var beforeDiscount = subTotal.Add(shippingCost);

        return beforeDiscount.IsGreaterThan(discountAmount)
            ? beforeDiscount.Subtract(discountAmount)
            : Money.Zero(subTotal.Currency);
    }

    public OrderCancellationValidation ValidateCancellation(Aggregates.Order order)
    {
        Guard.Against.Null(order, nameof(order));

        if (order.IsDeleted)
            return OrderCancellationValidation.Failed("سفارش حذف شده است.");

        if (!order.CanBeCancelled())
            return OrderCancellationValidation.Failed($"سفارش در وضعیت '{order.Status.DisplayName}' قابل لغو نیست.");

        return OrderCancellationValidation.Success(order.IsPaid);
    }
}