namespace Domain.Order.Aggregates;

public sealed class Order : AggregateRoot<Guid>
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
    public bool IsDeleted { get; private set; }
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
        Guid id,
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
        IsDeleted = false;
        CreatedAt = DateTime.UtcNow;

        foreach (var snapshot in itemSnapshots)
            _items.Add(OrderItem.FromSnapshot(id, snapshot));

        RecalculateTotals();

        RaiseDomainEvent(new OrderCreatedEvent(
            id,
            userId,
            orderNumber.Value,
            FinalAmount.Amount,
            FinalAmount.Currency,
            _items.Count,
            idempotencyKey));
    }

    public static Order Place(
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
            Guid.NewGuid(),
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

    public bool CanBeCancelled()
    {
        if (IsDeleted) return false;
        return Status.CanBeCancelled();
    }

    public bool CanBeModified()
    {
        if (IsDeleted) return false;
        return Status.CanBeEdited();
    }

    public void MarkAsReserved()
    {
        EnsureNotDeleted();
        TransitionTo(OrderStatusValue.Reserved);
    }

    public void MarkAsPending()
    {
        EnsureNotDeleted();
        TransitionTo(OrderStatusValue.Pending);
    }

    public void MarkAsPaid(Guid paymentTransactionId)
    {
        EnsureNotDeleted();

        if (paymentTransactionId == Guid.Empty)
            throw new ArgumentException("Payment transaction ID cannot be empty.", nameof(paymentTransactionId));

        TransitionTo(OrderStatusValue.Paid);
        PaymentTransactionId = paymentTransactionId;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new OrderPaidEvent(
            Id,
            OrderNumber.Value,
            UserId,
            paymentTransactionId,
            FinalAmount.Amount,
            FinalAmount.Currency));
    }

    public void MarkAsFailed()
    {
        EnsureNotDeleted();
        TransitionTo(OrderStatusValue.Failed);
    }

    public void StartProcessing()
    {
        EnsureNotDeleted();
        TransitionTo(OrderStatusValue.Processing);
    }

    public void MarkAsShipped()
    {
        EnsureNotDeleted();
        TransitionTo(OrderStatusValue.Shipped);
    }

    public void MarkAsDelivered()
    {
        EnsureNotDeleted();
        TransitionTo(OrderStatusValue.Delivered);
    }

    public void Cancel(string reason)
    {
        EnsureNotDeleted();

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Cancellation reason cannot be empty.", nameof(reason));

        if (!CanBeCancelled())
            throw new OrderCancellationNotAllowedException(Status.Value);

        var wasPaid = IsPaid;
        TransitionTo(OrderStatusValue.Cancelled);
        CancellationReason = reason;

        RaiseDomainEvent(new OrderCancelledEvent(
            Id,
            OrderNumber.Value,
            UserId,
            reason,
            wasPaid));
    }

    public void Expire()
    {
        EnsureNotDeleted();

        if (IsPaid)
            throw new InvalidOrderTransitionException(Status.Value, OrderStatusValue.Expired.Value);

        TransitionTo(OrderStatusValue.Expired);
    }

    public void Refund()
    {
        EnsureNotDeleted();
        TransitionTo(OrderStatusValue.Refunded);
    }

    public void MarkAsReturned()
    {
        EnsureNotDeleted();
        TransitionTo(OrderStatusValue.Returned);
    }

    public void SoftDelete()
    {
        if (IsDeleted) return;

        if (IsPaid)
            throw new DomainException("سفارش پرداخت شده قابل حذف نیست.");

        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    private void TransitionTo(OrderStatusValue next)
    {
        if (!Status.CanTransitionTo(next))
            throw new InvalidOrderTransitionException(Status.Value, next.Value);

        var previous = Status;
        Status = next;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new OrderStatusChangedEvent(
            Id,
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

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("سفارش حذف شده است و قابل تغییر نیست.");
    }
}