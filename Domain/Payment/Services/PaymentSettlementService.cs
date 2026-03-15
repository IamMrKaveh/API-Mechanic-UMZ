using Domain.Payment.Aggregates;
using Domain.Payment.Results;

namespace Domain.Payment.Services;

public sealed class PaymentSettlementService
{
    public static RefundEligibilityResult ValidateRefundEligibility(
        Order.Aggregates.Order order,
        PaymentTransaction payment)
    {
        Guard.Against.Null(order, nameof(order));
        Guard.Against.Null(payment, nameof(payment));

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

    public static RefundAmountValidation ValidateRefundAmount(
        PaymentTransaction payment)
    {
        Guard.Against.Null(payment, nameof(payment));

        var refundAmount = payment.Amount.Amount;

        if (refundAmount <= 0)
            return RefundAmountValidation.Failed($"مبلغ استرداد باید بزرگ‌تر از صفر باشد. مقدار: {refundAmount:N0}");

        if (refundAmount > payment.Amount.Amount)
            return RefundAmountValidation.Failed(
                $"مبلغ استرداد ({refundAmount:N0}) نمی‌تواند بیشتر از مبلغ پرداخت ({payment.Amount.Amount:N0}) باشد.");

        return RefundAmountValidation.Success(refundAmount);
    }

    public static SettlementRefundResult ProcessRefund(
        Order.Aggregates.Order order,
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

        order.Refund();

        return SettlementRefundResult.Success(payment.Amount.Amount);
    }

    public static PaymentSuccessSettlementResult ProcessPaymentSuccess(
        Order.Aggregates.Order order,
        Guid paymentTransactionId)
    {
        Guard.Against.Null(order, nameof(order));

        if (order.IsPaid)
            return PaymentSuccessSettlementResult.Idempotent();

        order.MarkAsPaid(paymentTransactionId);
        order.StartProcessing();

        return PaymentSuccessSettlementResult.Success();
    }
}