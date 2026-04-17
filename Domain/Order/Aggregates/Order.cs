using Domain.Discount.Aggregates;
using Domain.Discount.ValueObjects;
using Domain.Order.Entities;
using Domain.Order.Events;
using Domain.Order.Exceptions;
using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Aggregates;

public sealed class Order : AggregateRoot<OrderId>
{
    public OrderNumber OrderNumber { get; private init; } = null!;
    public OrderStatusValue Status { get; private set; } = null!;
    public ReceiverInfo ReceiverInfo { get; private set; } = null!;
    public DeliveryAddress DeliveryAddress { get; private set; } = null!;
    public Money SubTotal { get; private set; } = null!;
    public Money ShippingCost { get; private set; } = null!;
    public Money DiscountAmount { get; private set; } = null!;
    public Money FinalAmount { get; private set; } = null!;
    public Guid IdempotencyKey { get; private init; }
    public string? CancellationReason { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? UpdatedAt { get; private set; }

    public UserId UserId { get; private init; } = default!;
    public User.Aggregates.User User { get; private init; } = default!;
    public DiscountCodeId? AppliedDiscountCodeId { get; private set; }
    public DiscountCode? AppliedDiscountCode { get; private set; }
    public PaymentTransactionId? PaymentTransactionId { get; private set; }
    public PaymentTransaction? PaymentTransaction { get; private set; }

    private readonly List<OrderItem> _orderItems = [];
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    public bool IsPaid => Status.IsPaid;
    public bool IsCancelled => Status == OrderStatusValue.Cancelled;
    public bool IsDelivered => Status == OrderStatusValue.Delivered;
    public bool IsShipped => Status == OrderStatusValue.Shipped;

    private Order()
    { }

    private Order(
        OrderId id,
        UserId userId,
        OrderNumber orderNumber,
        ReceiverInfo receiverInfo,
        DeliveryAddress deliveryAddress,
        Money shippingCost,
        Money discountAmount,
        DiscountCodeId? appliedDiscountCodeId,
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
            _orderItems.Add(OrderItem.FromSnapshot(id, snapshot));

        RecalculateTotals();

        RaiseDomainEvent(new OrderCreatedEvent(
            id,
            userId,
            orderNumber,
            FinalAmount.Amount,
            FinalAmount.Currency,
            _orderItems.Count,
            idempotencyKey));
    }

    public static Order Place(
        OrderId orderId,
        UserId userId,
        ReceiverInfo receiverInfo,
        DeliveryAddress deliveryAddress,
        Money shippingCost,
        Money discountAmount,
        DiscountCodeId? appliedDiscountCodeId,
        IEnumerable<OrderItemSnapshot> itemSnapshots,
        Guid idempotencyKey)
    {
        var snapshots = itemSnapshots.ToList();

        if (!snapshots.Any())
            throw new EmptyOrderException();

        ArgumentNullException.ThrowIfNull(userId);

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

    public bool HasItems() => _orderItems.Any();

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

    public void MarkAsPaid(PaymentTransactionId paymentTransactionId)
    {
        ArgumentNullException.ThrowIfNull(paymentTransactionId);

        TransitionTo(OrderStatusValue.Paid);
        PaymentTransactionId = paymentTransactionId;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new OrderPaidEvent(
            Id,
            OrderNumber,
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
            throw new OrderCancellationNotAllowedException(Status);

        var wasPaid = IsPaid;
        TransitionTo(OrderStatusValue.Cancelled);
        CancellationReason = reason;

        RaiseDomainEvent(new OrderCancelledEvent(
            Id,
            OrderNumber,
            UserId,
            reason,
            wasPaid));
    }

    public void Expire(OrderStatusValue orderStatusValue)
    {
        if (IsPaid)
            throw new InvalidOrderTransitionException(Status, orderStatusValue);

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

    public void MarkAsDeleted()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private void TransitionTo(OrderStatusValue next)
    {
        if (!Status.CanTransitionTo(next))
            throw new InvalidOrderTransitionException(Status, next);

        var previous = Status;
        Status = next;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new OrderStatusChangedEvent(
            Id,
            OrderNumber,
            UserId,
            previous,
            next));
    }

    private void RecalculateTotals()
    {
        SubTotal = _orderItems.Aggregate(
            Money.Zero(),
            (acc, item) => acc.Add(item.TotalPrice));

        var beforeDiscount = SubTotal.Add(ShippingCost);
        FinalAmount = beforeDiscount.IsGreaterThan(DiscountAmount)
            ? beforeDiscount.Subtract(DiscountAmount)
            : Money.Zero(SubTotal.Currency);
    }
}