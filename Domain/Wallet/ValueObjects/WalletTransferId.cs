namespace Domain.Wallet.ValueObjects;

public sealed record WalletTransferId : StronglyTypedId<WalletTransferId>
{
    private WalletTransferId(Guid value) : base(value) { }

    public static implicit operator Guid(WalletTransferId id) => id.Value;

    public override string ToString() => Value.ToString();
}
