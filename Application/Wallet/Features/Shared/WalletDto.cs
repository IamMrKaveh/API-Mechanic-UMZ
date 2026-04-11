namespace Application.Wallet.Features.Shared;

public sealed record WalletDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal ReservedBalance { get; init; }
    public decimal AvailableBalance { get; init; }
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
    DateTime CreatedAt);

public sealed record AdminWalletAdjustmentDto(
    decimal Amount,
    string Reason,
    string? Description = null
);

public sealed record CreditWalletDto(
    decimal Amount,
    string Description,
    string ReferenceId,
    string? IdempotencyKey = null
);

public sealed record DebitWalletDto(
    decimal Amount,
    string Description,
    string ReferenceId,
    string? IdempotencyKey = null
);