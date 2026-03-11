namespace Domain.Wallet.ValueObjects;

public sealed record WalletId(Guid Value)
{
    public static WalletId NewId() => new(Guid.NewGuid());
    public static WalletId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}