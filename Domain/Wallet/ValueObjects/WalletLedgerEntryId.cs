namespace Domain.Wallet.ValueObjects;

public sealed record WalletLedgerEntryId
{
    public Guid Value { get; }

    private WalletLedgerEntryId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("WalletLedgerEntryId cannot be empty.", nameof(value));

        Value = value;
    }

    public static WalletLedgerEntryId NewId() => new(Guid.NewGuid());

    public static WalletLedgerEntryId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}