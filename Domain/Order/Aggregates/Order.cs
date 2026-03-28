using Domain.Order.Entities;
using Domain.Order.Events;
using Domain.Order.Exceptions;
using Domain.Order.ValueObjects;

namespace Domain.Order.Aggregates;

public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderItem> _items = [];

    public OrderNumber OrderNumber { get; private init; } = null!;
    public Guid UserId { get; private init; }
    public OrderStatusValue Status { get; private set; } = null!;
    public ReceiverInfo ReceiverInfo { get; private set; } = null!;
    public DeliveryAddress DeliveryAddress { get; private set; } = null!;
    public Money SubTotal { get; private set; } = null!;
    public Money ShippingCost { get; private set; } = null!;
    public Money DiscountAmount { get; private set; } = null!;
    public Money FinalAmount { get; private set; } = null!;
    public Guid? AppliedDiscountCodeId { get; private set; }
    public Guid? PaymentTransactionId { get; private set; }
    public Guid IdempotencyKey { get; private init; }
    public string? CancellationReason { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public bool IsPaid => Status.IsPaid;
    public bool IsCancelled => Status == OrderStatusValue.Cancelled;
    public bool IsDelivered => Status == OrderStatusValue.Delivered;
    public bool IsShipped => Status == OrderStatusValue.Shipped;

    private Order()
    { }

    private Order(
        OrderId id,
        Guid userId,
        OrderNumber orderNumber,
        ReceiverInfo receiverInfo,
        DeliveryAddress deliveryAddress,
        Money shippingCost,
        Money discountAmount,
        Guid? appliedDiscountCodeId,
        IEnumerable<OrderItemSnapshot> itemSnapshots,
        Guid idempotencyKey) : base(id)
    {
        UserId = userId;
        OrderNumber = orderNumber;
        ReceiverInfo = receiverInfo;
        DeliveryAddress = deliveryAddress;
        ShippingCost = shippingCost;
        DiscountAmount = discountAmount;
        AppliedDiscountCodeId = appliedDiscountCodeId;
        IdempotencyKey = idempotencyKey;
        Status = OrderStatusValue.Created;
        CreatedAt = DateTime.UtcNow;

        foreach (var snapshot in itemSnapshots)
            _items.Add(OrderItem.FromSnapshot(id.Value, snapshot));

        RecalculateTotals();

        RaiseDomainEvent(new OrderCreatedEvent(
            Guid.NewGuid(),
            userId,
            orderNumber.Value,
            FinalAmount.Amount,
            FinalAmount.Currency,
            _items.Count,
            idempotencyKey));
    }

    public static Order Place(
        OrderId orderId,
        Guid userId,
        ReceiverInfo receiverInfo,
        DeliveryAddress deliveryAddress,
        Money shippingCost,
        Money discountAmount,
        Guid? appliedDiscountCodeId,
        IEnumerable<OrderItemSnapshot> itemSnapshots,
        Guid idempotencyKey)
    {
        var snapshots = itemSnapshots.ToList();

        if (!snapshots.Any())
            throw new EmptyOrderException();

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));

        if (idempotencyKey == Guid.Empty)
            throw new ArgumentException("Idempotency key cannot be empty.", nameof(idempotencyKey));

        return new Order(
            orderId,
            userId,
            OrderNumber.Generate(),
            receiverInfo,
            deliveryAddress,
            shippingCost,
            discountAmount,
            appliedDiscountCodeId,
            snapshots,
            idempotencyKey);
    }

    public bool HasItems() => _items.Any();

    public bool CanBeCancelled() => Status.CanBeCancelled();

    public bool CanBeModified() => Status.CanBeEdited();

    public void MarkAsReserved()
    {
        TransitionTo(OrderStatusValue.Reserved);
    }

    public void MarkAsPending()
    {
        TransitionTo(OrderStatusValue.Pending);
    }

    public void MarkAsPaid(Guid paymentTransactionId)
    {
        if (paymentTransactionId == Guid.Empty)
            throw new ArgumentException("Payment transaction ID cannot be empty.", nameof(paymentTransactionId));

        TransitionTo(OrderStatusValue.Paid);
        PaymentTransactionId = paymentTransactionId;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new OrderPaidEvent(
            Guid.NewGuid(),
            OrderNumber.Value,
            UserId,
            paymentTransactionId,
            FinalAmount.Amount,
            FinalAmount.Currency));
    }

    public void MarkAsFailed()
    {
        TransitionTo(OrderStatusValue.Failed);
    }

    public void StartProcessing()
    {
        TransitionTo(OrderStatusValue.Processing);
    }

    public void MarkAsShipped()
    {
        TransitionTo(OrderStatusValue.Shipped);
    }

    public void MarkAsDelivered()
    {
        TransitionTo(OrderStatusValue.Delivered);
    }

    public void Cancel(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason cannot be empty.", nameof(reason));

        if (!CanBeCancelled())
            throw new OrderCancellationNotAllowedException(Status.Value);

        var wasPaid = IsPaid;
        TransitionTo(OrderStatusValue.Cancelled);
        CancellationReason = reason;

        RaiseDomainEvent(new OrderCancelledEvent(
            Guid.NewGuid(),
            OrderNumber.Value,
            UserId,
            reason,
            wasPaid));
    }

    public void Expire()
    {
        if (IsPaid)
            throw new InvalidOrderTransitionException(Status.Value, OrderStatusValue.Expired.Value);

        TransitionTo(OrderStatusValue.Expired);
    }

    public void Refund()
    {
        TransitionTo(OrderStatusValue.Refunded);
    }

    public void MarkAsReturned()
    {
        TransitionTo(OrderStatusValue.Returned);
    }

    private void TransitionTo(OrderStatusValue next)
    {
        if (!Status.CanTransitionTo(next))
            throw new InvalidOrderTransitionException(Status.Value, next.Value);

        var previous = Status;
        Status = next;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new OrderStatusChangedEvent(
            Guid.NewGuid(),
            OrderNumber.Value,
            UserId,
            previous.Value,
            next.Value));
    }

    private void RecalculateTotals()
    {
        SubTotal = _items.Aggregate(
            Money.Zero(),
            (acc, item) => acc.Add(item.TotalPrice));

        var beforeDiscount = SubTotal.Add(ShippingCost);
        FinalAmount = beforeDiscount.IsGreaterThan(DiscountAmount)
            ? beforeDiscount.Subtract(DiscountAmount)
            : Money.Zero(SubTotal.Currency);
    }
}