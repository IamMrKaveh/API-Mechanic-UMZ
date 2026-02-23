namespace Domain.Discount.Services;

/// <summary>
/// Domain Service برای هماهنگی اعمال تخفیف در سناریوی Discount + Order
/// منطق Cross-Aggregate: اعتبارسنجی + اعمال تخفیف + ثبت استفاده
/// Stateless - فقط Domain Type می‌گیرد و Domain Result برمی‌گرداند
/// </summary>
public sealed class DiscountApplicationService
{
    /// <summary>
    /// اعتبارسنجی و اعمال تخفیف روی سفارش
    /// تغییر وضعیت سفارش (ApplyDiscount) + ثبت Usage روی DiscountCode
    /// </summary>
    public CrossAggregateDiscountResult ApplyToOrder(
        DiscountCode discount,
        Order.Order order,
        int userId,
        int userPreviousUsageCount)
    {
        Guard.Against.Null(discount, nameof(discount));
        Guard.Against.Null(order, nameof(order));

        // اعتبارسنجی از طریق Aggregate خود Discount
        var validation = discount.ValidateForApplication(
            order.TotalAmount.Amount,
            userId,
            userPreviousUsageCount);

        if (!validation.IsValid)
            return CrossAggregateDiscountResult.Failed(validation.Error!);

        // محاسبه مبلغ تخفیف
        var discountMoney = discount.CalculateDiscountMoney(order.TotalAmount);

        // ثبت استفاده روی DiscountCode → DiscountAppliedEvent منتشر می‌شود
        var usage = discount.RecordUsage(userId, order.Id, discountMoney);

        // اعمال تخفیف روی سفارش → FinalAmount محاسبه مجدد می‌شود
        order.ApplyDiscount(discount.Id, discountMoney);

        return CrossAggregateDiscountResult.Success(discountMoney, usage);
    }

    /// <summary>
    /// لغو استفاده از تخفیف (هنگام لغو سفارش)
    /// </summary>
    public CrossAggregateDiscountCancelResult CancelForOrder(
        DiscountCode discount,
        Order.Order order)
    {
        Guard.Against.Null(discount, nameof(discount));
        Guard.Against.Null(order, nameof(order));

        if (!order.DiscountCodeId.HasValue || order.DiscountCodeId.Value != discount.Id)
            return CrossAggregateDiscountCancelResult.NotApplicable();

        // لغو استفاده روی DiscountCode → DiscountUsageCancelledEvent منتشر می‌شود
        discount.CancelUsage(order.Id);

        // حذف تخفیف از سفارش
        order.RemoveDiscount();

        return CrossAggregateDiscountCancelResult.Success();
    }
}

#region Result Types

public sealed class CrossAggregateDiscountResult
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }
    public Money? DiscountAmount { get; private set; }
    public DiscountUsage? Usage { get; private set; }

    private CrossAggregateDiscountResult()
    { }

    public static CrossAggregateDiscountResult Success(Money discountAmount, DiscountUsage usage) =>
        new() { IsSuccess = true, DiscountAmount = discountAmount, Usage = usage };

    public static CrossAggregateDiscountResult Failed(string error) =>
        new() { IsSuccess = false, Error = error };
}

public sealed class CrossAggregateDiscountCancelResult
{
    public bool IsSuccess { get; private set; }
    public bool IsNotApplicable { get; private set; }

    private CrossAggregateDiscountCancelResult()
    { }

    public static CrossAggregateDiscountCancelResult Success() => new() { IsSuccess = true };

    public static CrossAggregateDiscountCancelResult NotApplicable() => new() { IsNotApplicable = true, IsSuccess = true };
}

#endregion Result Types