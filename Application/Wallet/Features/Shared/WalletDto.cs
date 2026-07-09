namespace Application.Wallet.Features.Shared;

public sealed record WalletDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal ReservedBalance { get; init; }
    public decimal AvailableBalance { get; init; }
    public bool IsActive { get; init; }
    public string? FreezeReason { get; init; }
    public DateTime? FrozenAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed record WalletLedgerEntryDto(
    Guid Id,
    Guid WalletId,
    Guid UserId,
    decimal AmountDelta,
    decimal BalanceAfter,
    string TransactionType,
    string ReferenceType,
    Guid ReferenceId,
    string? Description,
    DateTime CreatedAt,
    bool IsAdminAdjustment);

public sealed record InitiateTopUpResultDto
{
    public Guid TopUpId { get; init; }
    public string PaymentUrl { get; init; } = string.Empty;
    public string Authority { get; init; } = string.Empty;
    public string Gateway { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}

public sealed record WalletWithdrawalRequestDto(
    Guid Id,
    Guid UserId,
    string? UserFullName,
    decimal Amount,
    string Iban,
    string AccountHolder,
    string? Description,
    string Status,
    string? RejectionReason,
    string? BankReferenceNumber,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? RejectedAt,
    DateTime? PaidAt,
    DateTime? CancelledAt);

public sealed record WalletFraudAlertDto(
    Guid Id,
    Guid WalletId,
    Guid UserId,
    string? UserFullName,
    string RuleName,
    string Severity,
    string Description,
    string? Metadata,
    string Status,
    DateTime TriggeredAt,
    Guid? ReviewedBy,
    DateTime? ReviewedAt,
    string? ReviewNote,
    DateTime CreatedAt);

public sealed record WalletOverviewDto(
    Guid WalletId,
    Guid UserId,
    string UserFullName,
    string UserEmail,
    decimal CurrentBalance,
    decimal ReservedBalance,
    decimal AvailableBalance,
    bool IsActive,
    string? FreezeReason,
    DateTime CreatedAt,
    DateTime? LastActivityAt);

public sealed record WalletStatisticsDto(
    decimal TotalSystemBalance,
    decimal TotalReservedBalance,
    decimal TotalAvailableBalance,
    int ActiveWalletsCount,
    int FrozenWalletsCount,
    int TotalWalletsCount,
    decimal TodayCreditVolume,
    decimal TodayDebitVolume,
    int Last7DaysTransactionCount,
    int PendingWithdrawalsCount,
    int OpenFraudAlertsCount,
    DateTime GeneratedAt);