namespace Domain.Wallet.ValueObjects;

public sealed record WalletReservationId : IStronglyTypedId
{
    public Guid Value { get; }

    private WalletReservationId(Guid value) => Value = value;

    public static WalletReservationId NewId() => new(Guid.NewGuid());

    public static WalletReservationId From(Guid value) => value == Guid.Empty
        ? throw new DomainException("WalletReservationId cannot be empty.")
        : new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(WalletReservationId id) => id.Value;
}