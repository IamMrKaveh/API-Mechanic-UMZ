namespace Domain.Order;

public class Order : AggregateRoot, ISoftDeletable, IAuditable
{
    private readonly List<OrderItem> _orderItems = new();

    public int UserId { get; private set; }
    public int? UserAddressId { get; private set; }
    public string ReceiverName { get; private set; } = null!;
    public AddressSnapshot AddressSnapshot { get; private set; } = null!;
    public Money TotalAmount { get; private set; } = null!;
    public Money TotalProfit { get; private set; } = null!;
    public Money ShippingCost { get; private set; } = null!;
    public Money DiscountAmount { get; private set; } = null!;
    public Money FinalAmount { get; private set; } = null!;
    public OrderStatusValue Status { get; private set; }
    public int ShippingMethodId { get; private set; }
    public int? DiscountCodeId { get; private set; }
    public DateTime? PaymentDate { get; private set; }
    public DateTime? ShippedDate { get; private set; }
    public DateTime? DeliveryDate { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public OrderNumber OrderNumber { get; private set; } = null!;
    public string? CancellationReason { get; private set; }
    public int? CancelledBy { get; private set; }

    // Soft Delete
    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAt { get; private set; }
    public int? DeletedBy { get; private set; }

    // Audit
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    // Navigation for EF Core
    public User.User? User { get; private set; }

    public ShippingMethod? ShippingMethod { get; private set; }
    public ICollection<Payment.PaymentTransaction> PaymentTransactions { get; private set; } = new List<Payment.PaymentTransaction>();
    public ICollection<DiscountUsage> DiscountUsages { get; private set; } = new List<DiscountUsage>();
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    // Computed Properties
    public bool IsPaid => Status.IsPaid();

    public bool IsShipped => Status.IsShipped();
    public bool IsDelivered => Status.IsDelivered();
    public bool IsCancelled => Status.IsCancelled();
    public bool IsPending => Status.IsPending();
    public bool IsProcessing => Status.IsProcessing();

    private Order()
    {
        Status = OrderStatusValue.Pending;
    }

    #region Factory Methods

    public static Order Place(
        int userId,
        int? userAddressId,
        string receiverName,
        AddressSnapshot addressSnapshot,
        int shippingMethodId,
        string idempotencyKey,
        Money shippingCost,
        IEnumerable<OrderItemSnapshot> items)
    {
        Guard.Against.NullOrWhiteSpace(receiverName, nameof(receiverName));
        Guard.Against.NullOrWhiteSpace(idempotencyKey, nameof(idempotencyKey));
        Guard.Against.Null(addressSnapshot, nameof(addressSnapshot));
        Guard.Against.Null(shippingCost, nameof(shippingCost));
        Guard.Against.Null(items, nameof(items));

        var itemsList = items.ToList();
        if (!itemsList.Any())
            throw new DomainException("سفارش باید حداقل یک آیتم داشته باشد.");

        var order = new Order
        {
            UserId = userId,
            UserAddressId = userAddressId,
            ReceiverName = receiverName.Trim(),
            AddressSnapshot = addressSnapshot,
            ShippingMethodId = shippingMethodId,
            IdempotencyKey = idempotencyKey,
            ShippingCost = shippingCost,
            OrderNumber = OrderNumber.Generate(),
            Status = OrderStatusValue.Pending,
            TotalAmount = Money.Zero(),
            TotalProfit = Money.Zero(),
            DiscountAmount = Money.Zero(),
            FinalAmount = Money.Zero(),
            CreatedAt = DateTime.UtcNow
        };

        foreach (var itemSnapshot in itemsList)
        {
            order.AddItemInternal(itemSnapshot);
        }

        order.RecalculateTotals();

        order.AddDomainEvent(new OrderCreatedEvent(order));
        return order;
    }

    #endregion Factory Methods

    #region State Transitions - Order Lifecycle

    public void MarkAsPaid(long refId, string? cardPan = null)
    {
        EnsureNotDeleted();
        EnsureCanTransitionTo(OrderStatusValue.Paid);

        if (!HasItems())
            throw new DomainException("سفارش بدون آیتم قابل پرداخت نیست.");

        Status = OrderStatusValue.Paid;
        PaymentDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderPaidEvent(Id, UserId, FinalAmount.Amount, refId));
    }

    public void StartProcessing()
    {
        EnsureNotDeleted();
        EnsureCanTransitionTo(OrderStatusValue.Processing);

        Status = OrderStatusValue.Processing;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderStatusChangedEvent(
            Id, UserId,
            Convert.ToInt32(OrderStatusValue.Paid.Value), Convert.ToInt32(OrderStatusValue.Processing.Value),
            OrderStatusValue.Paid.DisplayName, OrderStatusValue.Processing.DisplayName));
    }

    public void Ship()
    {
        EnsureNotDeleted();
        EnsureCanTransitionTo(OrderStatusValue.Shipped);

        Status = OrderStatusValue.Shipped;
        ShippedDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderStatusChangedEvent(
            Id, UserId,
            Convert.ToInt32(OrderStatusValue.Processing.Value), Convert.ToInt32(OrderStatusValue.Shipped.Value),
            OrderStatusValue.Processing.DisplayName, OrderStatusValue.Shipped.DisplayName));
    }

    public void MarkAsDelivered()
    {
        EnsureNotDeleted();
        EnsureCanTransitionTo(OrderStatusValue.Delivered);

        Status = OrderStatusValue.Delivered;
        DeliveryDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderStatusChangedEvent(
            Id, UserId,
            Convert.ToInt32(OrderStatusValue.Shipped.Value), Convert.ToInt32(OrderStatusValue.Delivered.Value),
            OrderStatusValue.Shipped.DisplayName, OrderStatusValue.Delivered.DisplayName));
    }

    public void Cancel(int cancelledBy, string reason)
    {
        EnsureNotDeleted();

        if (!CanBeCancelled())
            throw new DomainException(GetCancellationBlockReason());

        var oldStatus = Status;
        Status = OrderStatusValue.Cancelled;
        CancellationReason = reason;
        CancelledBy = cancelledBy;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderCancelledEvent(Id, cancelledBy, reason));
        AddDomainEvent(new OrderStatusChangedEvent(
            Id, UserId,
            Convert.ToInt32(oldStatus.Value), Convert.ToInt32(OrderStatusValue.Cancelled.Value),
            oldStatus.DisplayName, OrderStatusValue.Cancelled.DisplayName));
    }

    public void RequestRefund(string reason)
    {
        EnsureNotDeleted();
        EnsureCanTransitionTo(OrderStatusValue.Refunded);

        if (!IsPaid && !IsDelivered)
            throw new DomainException("فقط سفارش‌های پرداخت شده یا تحویل داده شده قابل استرداد هستند.");

        var oldStatus = Status;
        Status = OrderStatusValue.Refunded;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderStatusChangedEvent(
            Id, UserId,
            Convert.ToInt32(oldStatus.Value), Convert.ToInt32(OrderStatusValue.Refunded.Value),
            oldStatus.DisplayName, OrderStatusValue.Refunded.DisplayName));
    }

    public void MarkAsReturned(string reason)
    {
        EnsureNotDeleted();
        EnsureCanTransitionTo(OrderStatusValue.Returned);

        var oldStatus = Status;
        Status = OrderStatusValue.Returned;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new OrderStatusChangedEvent(
            Id, UserId,
            Convert.ToInt32(oldStatus.Value), Convert.ToInt32(OrderStatusValue.Returned.Value),
            oldStatus.DisplayName, OrderStatusValue.Returned.DisplayName));
    }

    #endregion State Transitions - Order Lifecycle

    #region Discount Management

    public void ApplyDiscount(int discountCodeId, Money discountAmount)
    {
        EnsureCanModify();
        Guard.Against.Null(discountAmount, nameof(discountAmount));

        if (discountAmount.Amount < 0)
            throw new DomainException("مبلغ تخفیف نمی‌تواند منفی باشد.");

        if (discountAmount.Amount > TotalAmount.Amount)
            throw new DomainException("مبلغ تخفیف نمی‌تواند بیشتر از مبلغ کل سفارش باشد.");

        DiscountCodeId = discountCodeId;
        DiscountAmount = discountAmount;
        RecalculateFinalAmount();
    }

    public void RemoveDiscount()
    {
        EnsureCanModify();

        DiscountCodeId = null;
        DiscountAmount = Money.Zero();
        RecalculateFinalAmount();
    }

    #endregion Discount Management

    #region Shipping Management

    public void UpdateShippingMethod(int shippingMethodId, Money newShippingCost)
    {
        EnsureCanModify();
        Guard.Against.Null(newShippingCost, nameof(newShippingCost));

        ShippingMethodId = shippingMethodId;
        ShippingCost = newShippingCost;
        RecalculateFinalAmount();
    }

    public void UpdateShippingAddress(AddressSnapshot newAddress, string receiverName)
    {
        EnsureCanModify();
        Guard.Against.Null(newAddress, nameof(newAddress));
        Guard.Against.NullOrWhiteSpace(receiverName, nameof(receiverName));

        AddressSnapshot = newAddress;
        ReceiverName = receiverName.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Shipping Management

    #region Soft Delete

    public void Delete(int? deletedBy)
    {
        if (IsDeleted) return;

        if (IsPaid)
            throw new DomainException("سفارش پرداخت شده قابل حذف نیست.");

        if (IsShipped)
            throw new DomainException("سفارش ارسال شده قابل حذف نیست.");

        if (IsDelivered)
            throw new DomainException("سفارش تحویل داده شده قابل حذف نیست.");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        if (!IsDeleted) return;

        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Soft Delete

    #region Query Methods

    public bool HasItems() => _orderItems.Any();

    public int GetTotalItemsCount() => _orderItems.Sum(i => i.Quantity);

    public bool ContainsVariant(int variantId) =>
        _orderItems.Any(i => i.VariantId == variantId);

    public OrderItem? GetItemByVariantId(int variantId) =>
        _orderItems.FirstOrDefault(i => i.VariantId == variantId);

    public bool CanBeCancelled()
    {
        if (IsDeleted) return false;
        if (IsShipped) return false;
        if (IsDelivered) return false;
        if (IsCancelled) return false;
        if (Status == OrderStatusValue.Refunded) return false;
        if (Status == OrderStatusValue.Returned) return false;

        return true;
    }

    public bool CanBeModified()
    {
        return !IsPaid && !IsDeleted && !IsCancelled;
    }

    public bool CanTransitionTo(OrderStatusValue newStatus)
    {
        return Status.CanTransitionTo(newStatus);
    }

    public void UpdateItemQuantity(int orderItemId, int newQuantity)
    {
        EnsureCanModify();
        var item = _orderItems.FirstOrDefault(i => i.Id == orderItemId);
        if (item == null) throw new DomainException("آیتم یافت نشد.");
        if (newQuantity <= 0) throw new DomainException("تعداد نامعتبر است.");

        item.UpdateQuantity(newQuantity);
        RecalculateTotals();
    }

    public void RemoveItem(int orderItemId)
    {
        EnsureCanModify();
        var item = _orderItems.FirstOrDefault(i => i.Id == orderItemId);
        if (item == null) throw new DomainException("آیتم یافت نشد.");

        _orderItems.Remove(item);
        RecalculateTotals();
    }

    #endregion Query Methods

    #region Private Methods

    private void AddItemInternal(OrderItemSnapshot snapshot)
    {
        var existingItem = _orderItems.FirstOrDefault(i => i.VariantId == snapshot.VariantId);
        if (existingItem != null)
            throw new DomainException("این محصول قبلاً به سفارش اضافه شده است.");

        var item = OrderItem.CreateFromSnapshot(snapshot);
        _orderItems.Add(item);
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
            throw new DomainException("سفارش حذف شده است.");
    }

    private void EnsureCanModify()
    {
        EnsureNotDeleted();

        if (IsPaid)
            throw new DomainException("امکان تغییر سفارش پرداخت شده وجود ندارد.");

        if (IsCancelled)
            throw new DomainException("سفارش لغو شده قابل ویرایش نیست.");
    }

    private void EnsureCanTransitionTo(OrderStatusValue newStatus)
    {
        if (!Status.CanTransitionTo(newStatus))
        {
            throw new DomainException(
                $"امکان تغییر وضعیت از '{Status.DisplayName}' به '{newStatus.DisplayName}' وجود ندارد.");
        }
    }

    private string GetCancellationBlockReason()
    {
        if (IsDeleted) return "سفارش حذف شده است.";
        if (IsShipped) return "سفارش ارسال شده قابل لغو نیست.";
        if (IsDelivered) return "سفارش تحویل داده شده قابل لغو نیست.";
        if (IsCancelled) return "سفارش قبلاً لغو شده است.";
        if (Status == OrderStatusValue.Refunded) return "سفارش استرداد شده قابل لغو نیست.";
        if (Status == OrderStatusValue.Returned) return "سفارش برگشت خورده قابل لغو نیست.";
        return "این سفارش قابل لغو نیست.";
    }

    private void RecalculateTotals()
    {
        var totalAmount = Money.Zero();
        var totalProfit = Money.Zero();

        foreach (var item in _orderItems)
        {
            totalAmount = totalAmount.Add(item.Amount);
            totalProfit = totalProfit.Add(item.Profit);
        }

        TotalAmount = totalAmount;
        TotalProfit = totalProfit;

        RecalculateFinalAmount();
    }

    private void RecalculateFinalAmount()
    {
        FinalAmount = TotalAmount
            .Add(ShippingCost)
            .Subtract(DiscountAmount);

        if (FinalAmount.Amount < 0)
        {
            FinalAmount = Money.Zero();
        }

        UpdatedAt = DateTime.UtcNow;
    }

    #endregion Private Methods
}