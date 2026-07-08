namespace Domain.Wallet.ValueObjects;

public sealed record WalletTransferId : IStronglyTypedId
{
    public Guid Value { get; }

    private WalletTransferId(Guid value) => Value = value;

    public static WalletTransferId NewId() => new(Guid.NewGuid());

    public static WalletTransferId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("WalletTransferId cannot be empty.")
        : new(value);

    public static implicit operator Guid(WalletTransferId id) => id.Value;

    public override string ToString() => Value.ToString();
}