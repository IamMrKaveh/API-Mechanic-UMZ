namespace Domain.Wallet.ValueObjects;

public sealed record WalletDebitRequestId : StronglyTypedId<WalletDebitRequestId>
{
    private WalletDebitRequestId(Guid value) : base(value) { }

    public static implicit operator Guid(WalletDebitRequestId id) => id.Value;

    public override string ToString() => Value.ToString();
}
