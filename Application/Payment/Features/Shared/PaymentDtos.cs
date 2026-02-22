namespace Application.Payment.Features.Shared;

// ── View Models ──

public record PaymentTransactionDto
{
    public int Id { get; init; }
    public int OrderId { get; init; }
    public int UserId { get; init; }
    public string Authority { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Gateway { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public long? RefId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? VerifiedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public string? CardPan { get; init; }
    public decimal Fee { get; init; }
}

public class PaymentInitiationDto
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public Domain.Common.Shared.ValueObjects.Money Amount { get; set; } = null!;
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

public record PaymentStatusDto
{
    public int TransactionId { get; init; }
    public int OrderId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string StatusDisplayName { get; init; } = string.Empty;
    public long? RefId { get; init; }
    public DateTime? VerifiedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan? TimeUntilExpiry { get; init; }
}

// ── Search Parameters ──

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