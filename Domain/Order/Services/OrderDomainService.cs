namespace Domain.Order.Services;

/// <summary>
/// Domain Service برای عملیات‌های پیچیده Order که بین چند Aggregate هستند
/// Stateless - بدون وابستگی به Infrastructure
/// </summary>
public sealed class OrderDomainService
{
    /// <summary>
    /// ایجاد سفارش از آیتم‌های سبد خرید
    /// </summary>
    public Order PlaceOrder(
        int userId,
        UserAddress userAddress,
        string receiverName,
        Shipping.Shipping shippingMethod,
        string idempotencyKey,
        IEnumerable<OrderItemSnapshot> items,
        DiscountApplicationResult? discountResult = null)
    {
        Guard.Against.Null(userAddress, nameof(userAddress));
        Guard.Against.Null(shippingMethod, nameof(shippingMethod));
        Guard.Against.NullOrWhiteSpace(idempotencyKey, nameof(idempotencyKey));

        var itemsList = items.ToList();
        ValidateOrderItems(itemsList);

        var addressSnapshot = AddressSnapshot.FromUserAddress(userAddress);

        var totalAmount = CalculateTotalAmount(itemsList);
        var shippingCost = shippingMethod.CalculateCost(
            Money.FromDecimal(totalAmount),
            CalculateAverageShippingMultiplier(itemsList));

        var order = Order.Place(
            userId,
            userAddress.Id,
            receiverName,
            addressSnapshot,
            shippingMethod.Id,
            idempotencyKey,
            shippingCost,
            itemsList);

        if (discountResult != null && discountResult.IsSuccess && discountResult.DiscountCodeId.HasValue)
        {
            order.ApplyDiscount(discountResult.DiscountCodeId.Value, Money.FromDecimal(discountResult.DiscountAmount));
        }

        return order;
    }

    /// <summary>
    /// اعتبارسنجی امکان لغو سفارش
    /// </summary>
    public OrderCancellationValidation ValidateCancellation(Order order, int requestingUserId, bool isAdmin)
    {
        Guard.Against.Null(order, nameof(order));

        if (order.IsDeleted)
            return OrderCancellationValidation.Failed("سفارش حذف شده است.");

        if (!isAdmin && order.UserId != requestingUserId)
            return OrderCancellationValidation.Failed("شما مجاز به لغو این سفارش نیستید.");

        if (!order.CanBeCancelled())
        {
            if (order.IsCancelled)
                return OrderCancellationValidation.Failed("سفارش قبلاً لغو شده است.");

            if (order.IsShipped)
                return OrderCancellationValidation.Failed("سفارش ارسال شده قابل لغو نیست. لطفاً درخواست برگشت ثبت کنید.");

            if (order.IsDelivered)
                return OrderCancellationValidation.Failed("سفارش تحویل داده شده قابل لغو نیست. لطفاً درخواست استرداد ثبت کنید.");

            return OrderCancellationValidation.Failed("این سفارش در وضعیت فعلی قابل لغو نیست.");
        }

        return OrderCancellationValidation.Success(order.IsPaid);
    }

    /// <summary>
    /// اعتبارسنجی تغییر وضعیت سفارش
    /// </summary>
    public OrderStatusTransitionValidation ValidateStatusTransition(
        Order order,
        OrderStatusValue newStatus)
    {
        Guard.Against.Null(order, nameof(order));
        Guard.Against.Null(newStatus, nameof(newStatus));

        if (order.IsDeleted)
            return OrderStatusTransitionValidation.Failed("سفارش حذف شده قابل تغییر وضعیت نیست.");

        if (order.Status == newStatus)
            return OrderStatusTransitionValidation.Failed("وضعیت جدید با وضعیت فعلی یکسان است.");

        if (!order.CanTransitionTo(newStatus))
        {
            var allowedStatuses = order.Status.GetAllowedNextStatuses();
            var allowedNames = string.Join("، ", allowedStatuses.Select(s => s.DisplayName));

            return OrderStatusTransitionValidation.Failed(
                $"امکان تغییر وضعیت از '{order.Status.DisplayName}' به '{newStatus.DisplayName}' وجود ندارد. " +
                $"وضعیت‌های مجاز: {(string.IsNullOrEmpty(allowedNames) ? "هیچکدام" : allowedNames)}");
        }

        return OrderStatusTransitionValidation.Success();
    }

    /// <summary>
    /// محاسبه مجموع سفارش
    /// </summary>
    public OrderTotals CalculateOrderTotals(
        IEnumerable<OrderItemSnapshot> items,
        Shipping.Shipping shippingMethod,
        Money? discountAmount = null)
    {
        Guard.Against.Null(items, nameof(items));
        Guard.Against.Null(shippingMethod, nameof(shippingMethod));

        var itemsList = items.ToList();

        var subtotal = CalculateTotalAmount(itemsList);
        var profit = CalculateTotalProfit(itemsList);

        var avgMultiplier = CalculateAverageShippingMultiplier(itemsList);
        var shippingCost = shippingMethod.CalculateCost(Money.FromDecimal(subtotal), avgMultiplier);
        var discount = discountAmount ?? Money.Zero();

        var finalAmount = Money.FromDecimal(subtotal)
            .Add(shippingCost)
            .Subtract(discount);

        return new OrderTotals(
            Money.FromDecimal(subtotal),
            Money.FromDecimal(profit),
            shippingCost,
            discount,
            finalAmount,
            itemsList.Sum(x => x.Quantity));
    }

    /// <summary>
    /// اعتبارسنجی آیتم‌های سفارش
    /// </summary>
    public OrderItemsValidation ValidateOrderItems(IEnumerable<OrderItemSnapshot> items)
    {
        Guard.Against.Null(items, nameof(items));

        var itemsList = items.ToList();
        var errors = new List<string>();

        if (!itemsList.Any())
        {
            errors.Add("سفارش باید حداقل یک آیتم داشته باشد.");
            return new OrderItemsValidation(false, errors);
        }

        var duplicateVariants = itemsList
            .GroupBy(i => i.VariantId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateVariants.Any())
        {
            errors.Add($"واریانت‌های تکراری در سفارش: {string.Join(", ", duplicateVariants)}");
        }

        foreach (var item in itemsList)
        {
            if (item.Quantity <= 0)
                errors.Add($"تعداد آیتم {item.ProductName} باید بزرگتر از صفر باشد.");

            if (item.SellingPrice.Amount < item.PurchasePrice.Amount)
                errors.Add($"قیمت فروش آیتم {item.ProductName} نمی‌تواند کمتر از قیمت خرید باشد.");
        }

        return new OrderItemsValidation(errors.Count == 0, errors);
    }

    /// <summary>
    /// اعتبارسنجی یکپارچگی قیمت‌ها - بررسی تطابق قیمت‌های انتظاری کاربر با قیمت‌های فعلی واریانت‌ها
    /// Handler این قیمت‌ها را ارسال می‌کند؛ تصمیم‌گیری توسط Domain Service انجام می‌شود.
    /// </summary>
    public PriceMismatchValidation ValidatePriceIntegrity(
        IEnumerable<(ProductVariant Variant, decimal ExpectedPrice)> priceExpectations)
    {
        Guard.Against.Null(priceExpectations, nameof(priceExpectations));

        var errors = new List<string>();

        foreach (var (variant, expectedPrice) in priceExpectations)
        {
            if (variant == null) continue;

            if (variant.SellingPrice.Amount != expectedPrice)
            {
                errors.Add(
                    $"قیمت محصول '{variant.Product?.Name ?? variant.Id.ToString()}' تغییر کرده است. " +
                    $"لطفاً سبد خرید را بررسی کنید.");
            }
        }

        return errors.Any()
            ? PriceMismatchValidation.Failed(errors)
            : PriceMismatchValidation.Valid();
    }

    #region Private Methods

    private decimal CalculateTotalAmount(List<OrderItemSnapshot> items)
    {
        return items.Sum(x => x.SellingPrice.Amount * x.Quantity);
    }

    private decimal CalculateTotalProfit(List<OrderItemSnapshot> items)
    {
        return items.Sum(x => (x.SellingPrice.Amount - x.PurchasePrice.Amount) * x.Quantity);
    }

    private decimal CalculateAverageShippingMultiplier(List<OrderItemSnapshot> items)
    {
        // در صورت نیاز به محاسبه ضریب ارسال از اطلاعات snapshot
        return 1m;
    }

    #endregion Private Methods
}

#region Result Types

public sealed class PriceMismatchValidation
{
    public bool IsValid { get; private set; }
    public IReadOnlyList<string> Errors { get; private set; } = new List<string>();

    private PriceMismatchValidation()
    { }

    public static PriceMismatchValidation Valid() => new() { IsValid = true };

    public static PriceMismatchValidation Failed(IEnumerable<string> errors) =>
        new() { IsValid = false, Errors = errors.ToList().AsReadOnly() };

    public string GetErrorsSummary() => string.Join(" | ", Errors);
}

#endregion Result Types