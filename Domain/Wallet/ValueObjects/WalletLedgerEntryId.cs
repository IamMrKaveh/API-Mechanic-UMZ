namespace Domain.Wallet.ValueObjects;

public sealed record WalletLedgerEntryId(Guid Value)
{
    public static WalletLedgerEntryId NewId() => new(Guid.NewGuid());
    public static WalletLedgerEntryId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}