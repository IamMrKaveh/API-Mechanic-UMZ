using Domain.Common.ValueObjects;

namespace Application.Payment.Features.Shared;

public record PaymentInitiationDto
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public Money Amount { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? Email { get; set; }
}

public record PaymentResultDto
{
    public bool IsSuccess { get; init; }
    public string? PaymentUrl { get; init; }
    public string? Authority { get; init; }
    public string? Message { get; init; }
    public string? RedirectUrl { get; init; }
    public long? RefId { get; init; }
}

public record PaymentSearchParams
{
    public int? OrderId { get; init; }
    public int? UserId { get; init; }
    public string? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
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
    int TransactionId,
    int OrderId,
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
}

public record PaymentInitiationResult
{
    public string Authority { get; init; } = string.Empty;
    public string PaymentUrl { get; init; } = string.Empty;
    public Guid TransactionId { get; init; }
}

public record PaymentVerificationResult
{
    public bool IsSuccess { get; init; }
    public long? RefId { get; init; }
    public string? Error { get; init; }
    public Guid TransactionId { get; init; }
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
}

public record PaymentStatusDto
{
    public Guid TransactionId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string StatusDisplayName { get; init; } = string.Empty;
    public TimeSpan? TimeUntilExpiry { get; init; }
    public bool CanPay { get; init; }
}