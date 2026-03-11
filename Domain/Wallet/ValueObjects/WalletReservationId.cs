namespace Domain.Wallet.ValueObjects;

public sealed record WalletReservationId(Guid Value)
{
    public static WalletReservationId NewId() => new(Guid.NewGuid());
    public static WalletReservationId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}