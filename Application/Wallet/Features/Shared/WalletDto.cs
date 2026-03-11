namespace Application.Wallet.Features.Shared;

public sealed record WalletDto
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public decimal CurrentBalance { get; init; }
    public decimal ReservedBalance { get; init; }
    public decimal AvailableBalance { get; init; }
    public string Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed record WalletLedgerEntryDto
{
    public int Id { get; init; }
    public int WalletId { get; init; }
    public int UserId { get; init; }
    public decimal AmountDelta { get; init; }
    public decimal BalanceAfter { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public string ReferenceType { get; init; } = string.Empty;
    public int ReferenceId { get; init; }
    public string IdempotencyKey { get; init; } = string.Empty;
    public string? CorrelationId { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
}