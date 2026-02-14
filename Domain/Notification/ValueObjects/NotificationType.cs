namespace Domain.Notification.ValueObjects;

public sealed class NotificationType : ValueObject
{
    public string Value { get; }
    public string DisplayName { get; }
    public string Icon { get; }
    public string Color { get; }
    public NotificationCategory Category { get; }

    private NotificationType(string value, string displayName, string icon, string color, NotificationCategory category)
    {
        Value = value;
        DisplayName = displayName;
        Icon = icon;
        Color = color;
        Category = category;
    }

    #region Predefined Types - Order Related

    public static NotificationType OrderCreated => new("OrderCreated", "ثبت سفارش", "shopping-cart", "green", NotificationCategory.Order);
    public static NotificationType OrderPaid => new("OrderPaid", "پرداخت سفارش", "credit-card", "green", NotificationCategory.Order);
    public static NotificationType OrderShipped => new("OrderShipped", "ارسال سفارش", "truck", "blue", NotificationCategory.Order);
    public static NotificationType OrderDelivered => new("OrderDelivered", "تحویل سفارش", "check-circle", "green", NotificationCategory.Order);
    public static NotificationType OrderCancelled => new("OrderCancelled", "لغو سفارش", "x-circle", "red", NotificationCategory.Order);

    #endregion Predefined Types - Order Related

    #region Predefined Types - Support Related

    public static NotificationType TicketReply => new("TicketReply", "پاسخ تیکت", "message-circle", "blue", NotificationCategory.Support);

    #endregion Predefined Types - Support Related

    #region Predefined Types - Product Related

    public static NotificationType PriceDropAlert => new("PriceDropAlert", "کاهش قیمت", "trending-down", "orange", NotificationCategory.Product);
    public static NotificationType StockAlert => new("StockAlert", "موجود شدن محصول", "package", "green", NotificationCategory.Product);

    #endregion Predefined Types - Product Related

    #region Predefined Types - Marketing Related

    public static NotificationType DiscountCode => new("DiscountCode", "کد تخفیف", "tag", "purple", NotificationCategory.Marketing);

    #endregion Predefined Types - Marketing Related

    #region Predefined Types - System Related

    public static NotificationType SystemAlert => new("SystemAlert", "اطلاعیه سیستم", "bell", "gray", NotificationCategory.System);
    public static NotificationType SecurityAlert => new("SecurityAlert", "هشدار امنیتی", "shield", "red", NotificationCategory.System);
    public static NotificationType AccountUpdate => new("AccountUpdate", "به‌روزرسانی حساب", "user", "blue", NotificationCategory.System);

    #endregion Predefined Types - System Related

    public static NotificationType FromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("نوع اعلان نمی‌تواند خالی باشد.");

        return value.ToLowerInvariant() switch
        {
            "ordercreated" => OrderCreated,
            "orderpaid" => OrderPaid,
            "ordershipped" => OrderShipped,
            "orderdelivered" => OrderDelivered,
            "ordercancelled" => OrderCancelled,
            "ticketreply" => TicketReply,
            "pricedropalert" => PriceDropAlert,
            "stockalert" => StockAlert,
            "discountcode" => DiscountCode,
            "securityalert" => SecurityAlert,
            "accountupdate" => AccountUpdate,
            _ => SystemAlert
        };
    }

    public static NotificationType Custom(string value, string displayName, string icon = "bell", string color = "gray")
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("مقدار نوع اعلان الزامی است.");

        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("نام نمایشی نوع اعلان الزامی است.");

        return new NotificationType(value.Trim(), displayName.Trim(), icon, color, NotificationCategory.Custom);
    }

    public static IEnumerable<NotificationType> GetAll()
    {
        yield return OrderCreated;
        yield return OrderPaid;
        yield return OrderShipped;
        yield return OrderDelivered;
        yield return OrderCancelled;
        yield return TicketReply;
        yield return PriceDropAlert;
        yield return StockAlert;
        yield return DiscountCode;
        yield return SystemAlert;
        yield return SecurityAlert;
        yield return AccountUpdate;
    }

    public static IEnumerable<NotificationType> GetByCategory(NotificationCategory category)
    {
        return GetAll().Where(t => t.Category == category);
    }

    public bool IsOrderRelated() => Category == NotificationCategory.Order;

    public bool IsSupportRelated() => Category == NotificationCategory.Support;

    public bool IsProductRelated() => Category == NotificationCategory.Product;

    public bool IsMarketingRelated() => Category == NotificationCategory.Marketing;

    public bool IsSystemRelated() => Category == NotificationCategory.System;

    public bool IsHighPriority() =>
        this == SecurityAlert || this == OrderCancelled;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => DisplayName;

    public static implicit operator string(NotificationType type) => type.Value;
}

public enum NotificationCategory
{
    Order,
    Support,
    Product,
    Marketing,
    System,
    Custom
}