using Domain.Common.Shared.ValueObjects;

namespace Domain.Payment.Services;

/// <summary>
/// Domain Service برای عملیات‌های پرداخت که بین چند Aggregate هستند
/// Stateless - بدون وابستگی به Infrastructure
/// </summary>
public sealed class PaymentDomainService
{
    /// <summary>
    /// اعتبارسنجی امکان شروع پرداخت برای سفارش
    /// </summary>
    public PaymentInitiationValidation ValidatePaymentInitiation(
        Order.Order order,
        decimal paymentAmount)
    {
        Guard.Against.Null(order, nameof(order));

        if (order.IsDeleted)
            return PaymentInitiationValidation.Failed("سفارش لغو شده است.");

        if (order.IsPaid)
            return PaymentInitiationValidation.Failed("سفارش قبلاً پرداخت شده است.");

        if (!order.HasItems())
            return PaymentInitiationValidation.Failed("سفارش بدون آیتم است.");

        if (order.FinalAmount.Amount <= 0)
            return PaymentInitiationValidation.Failed("مبلغ سفارش نامعتبر است.");

        if (!IsAmountMatching(order.FinalAmount.Amount, paymentAmount))
            return PaymentInitiationValidation.AmountMismatch(order.FinalAmount.Amount, paymentAmount);

        return PaymentInitiationValidation.Success();
    }

    /// <summary>
    /// اعتبارسنجی امکان تأیید پرداخت
    /// </summary>
    public PaymentVerificationValidation ValidateVerification(
        PaymentTransaction transaction,
        string gatewayStatus)
    {
        Guard.Against.Null(transaction, nameof(transaction));

        if (transaction.IsDeleted)
            return PaymentVerificationValidation.Failed("تراکنش حذف شده است.");

        if (transaction.IsSuccessful())
            return PaymentVerificationValidation.AlreadyVerified(transaction.RefId ?? 0);

        if (transaction.IsExpired())
            return PaymentVerificationValidation.Expired(transaction.ExpiresAt);

        if (!transaction.CanBeVerified())
            return PaymentVerificationValidation.Failed("این تراکنش قابل تأیید نیست.");

        if (!IsGatewayStatusSuccess(gatewayStatus))
            return PaymentVerificationValidation.UserCancelled();

        return PaymentVerificationValidation.Success();
    }

    /// <summary>
    /// پردازش نتیجه موفق پرداخت و تأثیر روی سفارش
    /// </summary>
    public PaymentProcessResult ProcessSuccessfulPayment(
        PaymentTransaction transaction,
        Order.Order order,
        long refId,
        string? cardPan = null,
        string? cardHash = null,
        decimal fee = 0,
        string? rawResponse = null)
    {
        Guard.Against.Null(transaction, nameof(transaction));
        Guard.Against.Null(order, nameof(order));

        if (transaction.OrderId != order.Id)
            return PaymentProcessResult.Failed("تراکنش متعلق به این سفارش نیست.");

        
        transaction.MarkAsSuccess(refId, cardPan, cardHash, fee, rawResponse);

        
        if (!order.IsPaid)
        {
            order.MarkAsPaid(refId, cardPan);
        }

        return PaymentProcessResult.Success(refId);
    }

    /// <summary>
    /// پردازش نتیجه ناموفق پرداخت
    /// </summary>
    public void ProcessFailedPayment(
        PaymentTransaction transaction,
        string? errorMessage = null,
        string? rawResponse = null)
    {
        Guard.Against.Null(transaction, nameof(transaction));

        transaction.MarkAsFailed(errorMessage, rawResponse);
    }

    /// <summary>
    /// منقضی کردن تراکنش‌های قدیمی
    /// </summary>
    public int ExpireStaleTransactions(IEnumerable<PaymentTransaction> transactions)
    {
        Guard.Against.Null(transactions, nameof(transactions));

        var expiredCount = 0;

        foreach (var transaction in transactions)
        {
            if (transaction.IsExpired() && transaction.IsPending())
            {
                transaction.Expire();
                expiredCount++;
            }
        }

        return expiredCount;
    }

    /// <summary>
    /// بررسی امکان بازگشت وجه
    /// </summary>
    public (bool CanRefund, string? Error) ValidateRefund(PaymentTransaction transaction)
    {
        Guard.Against.Null(transaction, nameof(transaction));

        if (!transaction.IsSuccessful())
            return (false, "فقط تراکنش‌های موفق قابل بازگشت هستند.");

        if (transaction.IsRefunded())
            return (false, "این تراکنش قبلاً بازگشت داده شده است.");

        return (true, null);
    }

    /// <summary>
    /// محاسبه مبلغ خالص پرداخت (پس از کسر کارمزد)
    /// </summary>
    public Money CalculateNetAmount(PaymentTransaction transaction)
    {
        Guard.Against.Null(transaction, nameof(transaction));

        if (!transaction.IsSuccessful())
            return Money.Zero();

        var netAmount = transaction.Amount.Amount - transaction.Fee;
        return Money.FromDecimal(Math.Max(0, netAmount));
    }

    #region Private Methods

    private static bool IsAmountMatching(decimal expected, decimal actual, decimal tolerance = 1m)
    {
        return Math.Abs(expected - actual) <= tolerance;
    }

    private static bool IsGatewayStatusSuccess(string gatewayStatus)
    {
        if (string.IsNullOrWhiteSpace(gatewayStatus))
            return false;

        return gatewayStatus.Equals("OK", StringComparison.OrdinalIgnoreCase) ||
               gatewayStatus.Equals("100", StringComparison.Ordinal);
    }

    #endregion
}

#region Result Types

public sealed class PaymentInitiationValidation
{
    public bool IsValid { get; private set; }
    public string? Error { get; private set; }
    public decimal? ExpectedAmount { get; private set; }
    public decimal? ActualAmount { get; private set; }

    private PaymentInitiationValidation() { }

    public static PaymentInitiationValidation Success()
    {
        return new PaymentInitiationValidation { IsValid = true };
    }

    public static PaymentInitiationValidation Failed(string error)
    {
        return new PaymentInitiationValidation { IsValid = false, Error = error };
    }

    public static PaymentInitiationValidation AmountMismatch(decimal expected, decimal actual)
    {
        return new PaymentInitiationValidation
        {
            IsValid = false,
            Error = $"مبلغ پرداخت با مبلغ سفارش مطابقت ندارد. مورد انتظار: {expected:N0}، دریافتی: {actual:N0}",
            ExpectedAmount = expected,
            ActualAmount = actual
        };
    }

    public bool IsAmountMismatch => ExpectedAmount.HasValue && ActualAmount.HasValue;
}

public sealed class PaymentVerificationValidation
{
    public bool IsValid { get; private set; }
    public string? Error { get; private set; }
    public bool IsAlreadyVerified { get; private set; }
    public long? ExistingRefId { get; private set; }
    public bool IsExpired { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public bool IsUserCancelled { get; private set; }

    private PaymentVerificationValidation() { }

    public static PaymentVerificationValidation Success()
    {
        return new PaymentVerificationValidation { IsValid = true };
    }

    public static PaymentVerificationValidation Failed(string error)
    {
        return new PaymentVerificationValidation { IsValid = false, Error = error };
    }

    public static PaymentVerificationValidation AlreadyVerified(long refId)
    {
        return new PaymentVerificationValidation
        {
            IsValid = false,
            Error = $"این تراکنش قبلاً با کد پیگیری {refId} تأیید شده است.",
            IsAlreadyVerified = true,
            ExistingRefId = refId
        };
    }

    public static PaymentVerificationValidation Expired(DateTime expiryDate)
    {
        return new PaymentVerificationValidation
        {
            IsValid = false,
            Error = $"تراکنش پرداخت منقضی شده است. زمان انقضا: {expiryDate:yyyy/MM/dd HH:mm}",
            IsExpired = true,
            ExpiryDate = expiryDate
        };
    }

    public static PaymentVerificationValidation UserCancelled()
    {
        return new PaymentVerificationValidation
        {
            IsValid = false,
            Error = "پرداخت توسط کاربر لغو شده است.",
            IsUserCancelled = true
        };
    }
}

public sealed class PaymentProcessResult
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }
    public long? RefId { get; private set; }

    private PaymentProcessResult() { }

    public static PaymentProcessResult Success(long refId)
    {
        return new PaymentProcessResult { IsSuccess = true, RefId = refId };
    }

    public static PaymentProcessResult Failed(string error)
    {
        return new PaymentProcessResult { IsSuccess = false, Error = error };
    }
}

#endregion