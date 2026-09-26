namespace Domain.Wallet.ValueObjects;

public sealed record WalletWithdrawalRequestId : StronglyTypedId<WalletWithdrawalRequestId>
{
    private WalletWithdrawalRequestId(Guid value) : base(value) { }

    public static implicit operator Guid(WalletWithdrawalRequestId id) => id.Value;

    public override string ToString() => Value.ToString();
}
