namespace Domain.Wallet.ValueObjects;

public sealed record WalletTopUpId : StronglyTypedId<WalletTopUpId>
{
    private WalletTopUpId(Guid value) : base(value) { }

    public static implicit operator Guid(WalletTopUpId id) => id.Value;

    public override string ToString() => Value.ToString();
}
