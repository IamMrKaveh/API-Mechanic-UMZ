namespace Domain.Wallet.ValueObjects;

public sealed record WalletReservationId : StronglyTypedId<WalletReservationId>
{
    private WalletReservationId(Guid value) : base(value) { }

    public static implicit operator Guid(WalletReservationId id) => id.Value;

    public override string ToString() => Value.ToString();
}
