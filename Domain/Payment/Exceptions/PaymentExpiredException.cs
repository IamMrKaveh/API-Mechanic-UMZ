namespace Domain.Payment.Exceptions;

public sealed class PaymentExpiredException(string authority, DateTime expiryDate) : DomainException($"تراکنش پرداخت با شناسه '{authority}' منقضی شده است. زمان انقضا: {expiryDate:yyyy/MM/dd HH:mm}")
{
    public string Authority { get; } = authority;
    public DateTime ExpiryDate { get; } = expiryDate;
    public TimeSpan ExpiredSince { get; } = DateTime.UtcNow - expiryDate;

    public PaymentExpiredException(PaymentAuthority authority, DateTime expiryDate)
        : this(authority.Value, expiryDate)
    {
    }

    public bool IsRecentlyExpired() => ExpiredSince.TotalMinutes < 5;
}