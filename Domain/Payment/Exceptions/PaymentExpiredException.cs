using Domain.Common.Exceptions;
using Domain.Payment.ValueObjects;

namespace Domain.Payment.Exceptions;

public sealed class PaymentExpiredException : DomainException
{
    public PaymentAuthority Authority { get; }
    public DateTime ExpiryDate { get; }
    public TimeSpan ExpiredSince => DateTime.UtcNow - ExpiryDate;

    public override string ErrorCode => "PAYMENT_EXPIRED";

    public PaymentExpiredException(PaymentAuthority authority, DateTime expiryDate)
        : base($"تراکنش پرداخت با شناسه '{authority}' منقضی شده است. زمان انقضا: {expiryDate:yyyy/MM/dd HH:mm}")
    {
        Authority = authority;
        ExpiryDate = expiryDate;
    }

    public bool IsRecentlyExpired() => ExpiredSince.TotalMinutes < 5;
}