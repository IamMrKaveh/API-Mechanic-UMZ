namespace Domain.Payment;

public class PaymentTransaction : IAuditable
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order.Order Order { get; set; } = null!;

    public int UserId { get; set; }

    public string? Description { get; set; }

    public required string Authority { get; set; }

    public decimal Amount { get; set; }

    public decimal OriginalAmount { get; set; }

    public required string Status { get; set; }

    public required string Gateway { get; set; }

    public long? RefId { get; set; }

    public string? CardPan { get; set; }

    public string? CardHash { get; set; }

    public decimal Fee { get; set; }

    public string? IpAddress { get; set; }

    public string? ErrorMessage { get; set; }

    public string? GatewayRawResponse { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public DateTime? LastVerificationAttempt { get; set; }

    public int VerificationCount { get; set; }
    public DateTime VerificationAttemptedAt { get; set; }

    public static class PaymentStatuses
    {
        public const string Initialized = "Initialized";
        public const string Pending = "Pending";
        public const string VerificationInProgress = "VerificationInProgress";
        public const string Success = "Success";
        public const string Failed = "Failed";
        public const string Timeout = "Timeout";
        public const string Expired = "Expired";
        public const string Refunded = "Refunded";
        public const string VerificationRetryable = "VerificationFailed-Retryable";
    }
}