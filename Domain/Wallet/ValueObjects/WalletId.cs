using System;

namespace Domain.Wallet.ValueObjects;

public sealed record WalletId
{
    public Guid Value { get; }

    private WalletId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("WalletId cannot be empty.", nameof(value));

        Value = value;
    }

    public static WalletId NewId() => new(Guid.NewGuid());

    public static WalletId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}