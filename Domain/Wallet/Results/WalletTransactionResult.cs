namespace Domain.Wallet.Results;

public sealed record WalletTransactionResult
{
    public bool IsSuccess { get; private init; }
    public WalletId? WalletId { get; private init; }
    public Money? NewBalance { get; private init; }
    public WalletLedgerEntry? LedgerEntry { get; private init; }
    public string? Error { get; private init; }

    private WalletTransactionResult()
    {
    }

    public static WalletTransactionResult Success(WalletId walletId, Money newBalance, WalletLedgerEntry ledgerEntry) =>
        new() { IsSuccess = true, WalletId = walletId, NewBalance = newBalance, LedgerEntry = ledgerEntry };

    public static WalletTransactionResult Failed(string error) =>
        new() { IsSuccess = false, Error = error };
}