namespace Domain.Wallet.ValueObjects;

public sealed record WalletFraudAlertId : IStronglyTypedId
{
    public Guid Value { get; }

    private WalletFraudAlertId(Guid value) => Value = value;

    public static WalletFraudAlertId NewId() => new(Guid.NewGuid());

    public static WalletFraudAlertId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("WalletFraudAlertId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(WalletFraudAlertId id) => id.Value;
}