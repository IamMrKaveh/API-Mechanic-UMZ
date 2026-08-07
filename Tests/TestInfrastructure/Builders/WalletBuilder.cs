using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;

namespace Tests.TestInfrastructure.Builders;

public sealed class WalletBuilder
{
    private UserId _ownerId = UserId.NewId();
    private string _currency = "IRT";

    public WalletBuilder WithOwnerId(UserId ownerId)
    {
        _ownerId = ownerId;
        return this;
    }

    public WalletBuilder WithCurrency(string currency)
    {
        _currency = currency;
        return this;
    }

    public Wallet Build() => Wallet.Create(_ownerId, _currency);
}
