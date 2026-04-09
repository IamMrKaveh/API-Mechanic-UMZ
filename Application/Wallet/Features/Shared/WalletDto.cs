namespace Application.Wallet.Features.Shared;

public sealed record WalletDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal ReservedBalance { get; init; }
    public decimal AvailableBalance { get; init; }
    public string Status { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed record WalletLedgerEntryDto
{
    public Guid Id { get; init; }
    public Guid WalletId { get; init; }
    public Guid UserId { get; init; }
    public decimal AmountDelta { get; init; }
    public decimal BalanceAfter { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public string ReferenceType { get; init; } = string.Empty;
    public Guid ReferenceId { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
}