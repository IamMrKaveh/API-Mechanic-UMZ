namespace Domain.Wallet.ValueObjects;

public sealed record WalletTopUpId : IStronglyTypedId
{
    public Guid Value { get; }

    private WalletTopUpId(Guid value) => Value = value;

    public static WalletTopUpId NewId() => new(Guid.NewGuid());

    public static WalletTopUpId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("WalletTopUpId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(WalletTopUpId id) => id.Value;
}