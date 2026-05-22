using Domain.Payment.ValueObjects;

namespace Domain.Payment.Exceptions;

public sealed class PaymentExpiredException(PaymentAuthority authority, DateTime expiryDate) : DomainException($"تراکنش پرداخت با شناسه '{authority}' منقضی شده است. زمان انقضا: {expiryDate:yyyy/MM/dd HH:mm}")
{
    public PaymentAuthority Authority { get; } = authority;
    public DateTime ExpiryDate { get; } = expiryDate;
    public TimeSpan ExpiredSince => DateTime.UtcNow - ExpiryDate;

    public override string ErrorCode => "PAYMENT_EXPIRED";
}