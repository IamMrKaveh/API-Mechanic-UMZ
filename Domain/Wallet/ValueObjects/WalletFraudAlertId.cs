namespace Domain.Wallet.ValueObjects;

public sealed record WalletFraudAlertId : StronglyTypedId<WalletFraudAlertId>
{
    private WalletFraudAlertId(Guid value) : base(value) { }

    public static implicit operator Guid(WalletFraudAlertId id) => id.Value;

    public override string ToString() => Value.ToString();
}
