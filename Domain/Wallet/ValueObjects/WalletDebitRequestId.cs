namespace Domain.Wallet.ValueObjects;

public sealed record WalletDebitRequestId : IStronglyTypedId
{
    public Guid Value { get; }

    private WalletDebitRequestId(Guid value) => Value = value;

    public static WalletDebitRequestId NewId() => new(Guid.NewGuid());

    public static WalletDebitRequestId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("WalletDebitRequestId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();
}
