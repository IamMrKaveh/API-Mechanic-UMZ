using System;

namespace Domain.Wallet.ValueObjects;

public sealed record WalletReservationId
{
    public Guid Value { get; }

    private WalletReservationId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("WalletReservationId cannot be empty.", nameof(value));

        Value = value;
    }

    public static WalletReservationId NewId() => new(Guid.NewGuid());

    public static WalletReservationId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}