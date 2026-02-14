namespace Domain.Discount;

public class DiscountUsage : AggregateRoot
{
    public int UserId { get; private set; }
    public int DiscountCodeId { get; private set; }
    public int OrderId { get; private set; }
    public Money DiscountAmount { get; private set; } = null!;
    public DateTime UsedAt { get; private set; }
    public bool IsConfirmed { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public bool IsCancelled { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    // Navigation
    public User.User User { get; private set; } = null!;

    public DiscountCode DiscountCode { get; private set; } = null!;
    public Order.Order Order { get; private set; } = null!;

    private DiscountUsage()
    { }

    #region Factory Method

    internal static DiscountUsage Record(int userId, int discountId, int orderId, Money amount)
    {
        Guard.Against.NegativeOrZero(userId, nameof(userId));
        Guard.Against.NegativeOrZero(discountId, nameof(discountId));
        Guard.Against.NegativeOrZero(orderId, nameof(orderId));
        Guard.Against.Null(amount, nameof(amount));

        if (amount.Amount < 0)
            throw new DomainException("مبلغ تخفیف نمی‌تواند منفی باشد.");

        return new DiscountUsage
        {
            UserId = userId,
            DiscountCodeId = discountId,
            OrderId = orderId,
            DiscountAmount = amount,
            UsedAt = DateTime.UtcNow,
            IsConfirmed = false,
            IsCancelled = false
        };
    }

    #endregion Factory Method

    #region State Management

    internal void Confirm()
    {
        if (IsConfirmed)
            throw new DomainException("این استفاده قبلاً تأیید شده است.");

        if (IsCancelled)
            throw new DomainException("امکان تأیید استفاده لغو شده وجود ندارد.");

        IsConfirmed = true;
        ConfirmedAt = DateTime.UtcNow;

        AddDomainEvent(new DiscountUsageConfirmedEvent(Id, OrderId, DiscountCodeId));
    }

    internal void Cancel()
    {
        if (IsCancelled)
            return;

        IsCancelled = true;
        CancelledAt = DateTime.UtcNow;

        AddDomainEvent(new DiscountUsageCancelledEvent(DiscountCodeId, OrderId));
    }

    #endregion State Management

    #region Query Methods

    public bool IsActive() => IsConfirmed && !IsCancelled;

    public bool IsPending() => !IsConfirmed && !IsCancelled;

    #endregion Query Methods
}