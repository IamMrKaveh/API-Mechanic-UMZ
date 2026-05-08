namespace Domain.Wallet.ValueObjects;

public sealed record WalletLedgerEntryId : IStronglyTypedId
{
    public Guid Value { get; }

    private WalletLedgerEntryId(Guid value) => Value = value;

    public static WalletLedgerEntryId NewId() => new(Guid.NewGuid());

    public static WalletLedgerEntryId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("WalletLedgerEntryId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(WalletLedgerEntryId id) => id.Value;
}