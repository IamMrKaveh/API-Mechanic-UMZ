namespace Domain.Payment.Services;

/// <summary>
/// Domain Service برای هماهنگی تسویه پرداخت در سناریوی Payment + Order
/// منطق Cross-Aggregate برای استرداد و تأیید پرداخت
/// Stateless - فقط Domain Type می‌گیرد و Domain Result برمی‌گرداند
/// </summary>
public sealed class PaymentSettlementService
{
    /// <summary>
    /// اعتبارسنجی امکان استرداد - Cross-Aggregate بین Order و Payment
    /// </summary>
    public RefundEligibilityResult ValidateRefundEligibility(
        Order.Order order,
        PaymentTransaction payment)
    {
        Guard.Against.Null(order, nameof(order));
        Guard.Against.Null(payment, nameof(payment));

        if (order.IsDeleted)
            return RefundEligibilityResult.Failed("سفارش حذف شده است.");

        if (!order.IsPaid && !order.IsDelivered)
            return RefundEligibilityResult.Failed(
                $"استرداد فقط برای سفارش‌های پرداخت‌شده یا تحویل‌داده‌شده مجاز است. " +
                $"وضعیت فعلی: {order.Status.DisplayName}");

        if (!payment.IsSuccessful())
            return RefundEligibilityResult.Failed("فقط تراکنش‌های پرداخت موفق قابل استرداد هستند.");

        if (payment.IsRefunded())
            return RefundEligibilityResult.Failed("این تراکنش قبلاً استرداد شده است.");

        if (payment.OrderId != order.Id)
            return RefundEligibilityResult.Failed("این تراکنش متعلق به سفارش درخواستی نیست.");

        return RefundEligibilityResult.Success(payment.Amount.Amount);
    }

    /// <summary>
    /// اعتبارسنجی مبلغ استرداد جزئی
    /// </summary>
    public RefundAmountValidation ValidateRefundAmount(
        PaymentTransaction payment,
        decimal? partialAmount)
    {
        Guard.Against.Null(payment, nameof(payment));

        var refundAmount = partialAmount ?? payment.Amount.Amount;

        if (refundAmount <= 0)
            return RefundAmountValidation.Failed($"مبلغ استرداد باید بزرگ‌تر از صفر باشد. مقدار: {refundAmount:N0}");

        if (refundAmount > payment.Amount.Amount)
            return RefundAmountValidation.Failed(
                $"مبلغ استرداد ({refundAmount:N0}) نمی‌تواند بیشتر از مبلغ پرداخت ({payment.Amount.Amount:N0}) باشد.");

        return RefundAmountValidation.Success(refundAmount);
    }

    /// <summary>
    /// اعمال استرداد روی تراکنش و سفارش (تغییر وضعیت هر دو Aggregate)
    /// رویدادهای دامنه توسط خود Aggregateها منتشر می‌شوند
    /// </summary>
    public SettlementRefundResult ProcessRefund(
        Order.Order order,
        PaymentTransaction payment,
        string reason)
    {
        Guard.Against.Null(order, nameof(order));
        Guard.Against.Null(payment, nameof(payment));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        var eligibility = ValidateRefundEligibility(order, payment);
        if (!eligibility.IsValid)
            return SettlementRefundResult.Failed(eligibility.Error!);

        
        payment.Refund(reason);

        
        order.RequestRefund(reason);

        return SettlementRefundResult.Success(payment.Amount.Amount);
    }

    /// <summary>
    /// چرخه کامل تسویه پرداخت موفق را مدیریت می‌کند.
    /// Domain Service تصمیم می‌گیرد که order.MarkAsPaid و order.StartProcessing
    /// فراخوانی شوند — Saga فقط موجودیت‌ها را ذخیره می‌کند.
    /// </summary>
    public PaymentSuccessSettlementResult ProcessPaymentSuccess(
        Order.Order order,
        long refId,
        string? cardPan)
    {
        Guard.Against.Null(order, nameof(order));

        if (order.IsDeleted)
            return PaymentSuccessSettlementResult.Failed("سفارش حذف شده است.");

        
        if (order.IsPaid)
            return PaymentSuccessSettlementResult.Idempotent();

        order.MarkAsPaid(refId, cardPan);
        order.StartProcessing();

        return PaymentSuccessSettlementResult.Success();
    }
}

#region Result Types

public sealed class PaymentSuccessSettlementResult
{
    public bool IsSuccess { get; private set; }
    public bool IsIdempotent { get; private set; }
    public string? Error { get; private set; }

    private PaymentSuccessSettlementResult()
    { }

    public static PaymentSuccessSettlementResult Success() =>
        new() { IsSuccess = true };

    /// <summary>تراکنش قبلاً پردازش شده - بدون تغییر مجدد، موفق برمی‌گردد</summary>
    public static PaymentSuccessSettlementResult Idempotent() =>
        new() { IsSuccess = true, IsIdempotent = true };

    public static PaymentSuccessSettlementResult Failed(string error) =>
        new() { IsSuccess = false, Error = error };
}

public sealed class RefundEligibilityResult
{
    public bool IsValid { get; private set; }
    public string? Error { get; private set; }
    public decimal? EligibleAmount { get; private set; }

    private RefundEligibilityResult()
    { }

    public static RefundEligibilityResult Success(decimal amount) =>
        new() { IsValid = true, EligibleAmount = amount };

    public static RefundEligibilityResult Failed(string error) =>
        new() { IsValid = false, Error = error };
}

public sealed class RefundAmountValidation
{
    public bool IsValid { get; private set; }
    public string? Error { get; private set; }
    public decimal RefundAmount { get; private set; }

    private RefundAmountValidation()
    { }

    public static RefundAmountValidation Success(decimal amount) =>
        new() { IsValid = true, RefundAmount = amount };

    public static RefundAmountValidation Failed(string error) =>
        new() { IsValid = false, Error = error };
}

public sealed class SettlementRefundResult
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }
    public decimal? RefundedAmount { get; private set; }

    private SettlementRefundResult()
    { }

    public static SettlementRefundResult Success(decimal amount) =>
        new() { IsSuccess = true, RefundedAmount = amount };

    public static SettlementRefundResult Failed(string error) =>
        new() { IsSuccess = false, Error = error };
}

#endregion Result Types