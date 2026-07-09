namespace Application.Wallet.Features.Shared;

public sealed record WalletLedgerFilter
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string? TransactionType { get; init; }
    public decimal? MinAmount { get; init; }
    public decimal? MaxAmount { get; init; }
    public string? SearchTerm { get; init; }
    public int MaxRows { get; init; } = 10_000;
}