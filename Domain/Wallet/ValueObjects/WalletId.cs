namespace Domain.Wallet.ValueObjects;

public sealed record WalletId : StronglyTypedId<WalletId>
{
    private WalletId(Guid value) : base(value) { }

    public static implicit operator Guid(WalletId id) => id.Value;

    public override string ToString() => Value.ToString();
}
