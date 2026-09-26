namespace Domain.Wallet.ValueObjects;

public sealed record WalletLedgerEntryId : StronglyTypedId<WalletLedgerEntryId>
{
    private WalletLedgerEntryId(Guid value) : base(value) { }

    public static implicit operator Guid(WalletLedgerEntryId id) => id.Value;

    public override string ToString() => Value.ToString();
}
