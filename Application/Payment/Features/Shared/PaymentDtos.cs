namespace Application.Payment.Features.Shared;

public record PaymentInitiationDto(
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string Description,
    string CallbackUrl,
    string? Mobile,
    string? Email
);

public record PaymentResultDto
{
    public bool IsSuccess { get; init; }
    public string? PaymentUrl { get; init; }
    public string? Authority { get; init; }
    public string? Message { get; init; }
    public string? RedirectUrl { get; init; }
    public long? RefId { get; init; }
}

public record PaymentStatisticsDto
{
    public int TotalTransactions { get; init; }
    public int SuccessfulTransactions { get; init; }
    public int FailedTransactions { get; init; }
    public int PendingTransactions { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal SuccessfulAmount { get; init; }
    public decimal TotalFees { get; init; }
    public decimal SuccessRate { get; init; }
}

public sealed record SettlementReportDto(
    DateTime Date,
    int VerifiedCount,
    decimal TotalAmount,
    int DiscrepancyCount,
    IEnumerable<DiscrepancyDto> Discrepancies);

public sealed record DiscrepancyDto(
    Guid TransactionId,
    Guid OrderId,
    string GatewayName,
    decimal Amount,
    string SystemStatus,
    string GatewayStatus);

public record WebhookPayload
{
    public string Authority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public long? RefId { get; init; }
}

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

public sealed record PaymentInitiationResult(string Authority, string PaymentUrl);

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