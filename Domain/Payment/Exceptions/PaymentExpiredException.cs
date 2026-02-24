namespace Domain.Payment.Exceptions;

public sealed class PaymentExpiredException : DomainException
{
    public string Authority { get; }
    public DateTime ExpiryDate { get; }
    public TimeSpan ExpiredSince { get; }

    public PaymentExpiredException(string authority, DateTime expiryDate)
        : base($"تراکنش پرداخت با شناسه '{authority}' منقضی شده است. زمان انقضا: {expiryDate:yyyy/MM/dd HH:mm}")
    {
        Authority = authority;
        ExpiryDate = expiryDate;
        ExpiredSince = DateTime.UtcNow - expiryDate;
    }

    public bool IsRecentlyExpired() => ExpiredSince.TotalMinutes < 5;
}