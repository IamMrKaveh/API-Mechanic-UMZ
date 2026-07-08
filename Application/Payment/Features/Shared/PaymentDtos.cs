namespace Application.Payment.Features.Shared;

public record PaymentTransactionDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public string Authority { get; init; } = string.Empty;
    public string Gateway { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public string StatusDisplayName { get; init; } = string.Empty;
    public long? RefId { get; init; }
    public bool IsSuccessful { get; init; }
    public DateTime? VerifiedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed record PaymentInitiationResult(
    string Authority,
    string PaymentUrl,
    Guid TransactionId);

public sealed record PaymentVerificationResult(
    Guid? TransactionId,
    bool IsVerified,
    long? RefId,
    string? CardPan,
    decimal Fee);

public record PaymentStatusDto
{
    public string Authority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsSuccess { get; init; }
    public long? RefId { get; init; }
    public decimal Amount { get; init; }
}