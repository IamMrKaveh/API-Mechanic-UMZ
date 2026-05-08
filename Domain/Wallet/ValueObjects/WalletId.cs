namespace Domain.Wallet.ValueObjects;

public sealed record WalletId : IStronglyTypedId
{
    public Guid Value { get; }

    private WalletId(Guid value) => Value = value;

    public static WalletId NewId() => new(Guid.NewGuid());

    public static WalletId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("WalletId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(WalletId id) => id.Value;
}