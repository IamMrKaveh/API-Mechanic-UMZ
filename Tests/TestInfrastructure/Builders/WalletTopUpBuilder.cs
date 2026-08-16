using Domain.User.ValueObjects; using Domain.Wallet.Aggregates; using SharedKernel.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class WalletTopUpBuilder { private static readonly Faker Faker = new();

private UserId _userId = UserId.NewId();
private Money _amount = Money.Create(50_000m, "IRT");
private string _gateway = Faker.PickRandom(new[] { "zarinpal", "idpay", "mock" });

public WalletTopUpBuilder WithUserId(UserId userId)
{
    _userId = userId;
    return this;
}

public WalletTopUpBuilder WithAmount(decimal amount, string currency = "IRT")
{
    _amount = Money.Create(amount, currency);
    return this;
}

public WalletTopUpBuilder WithAmount(Money amount)
{
    _amount = amount;
    return this;
}

public WalletTopUpBuilder WithGateway(string gateway)
{
    _gateway = gateway;
    return this;
}

public WalletTopUp Build() => WalletTopUp.Initiate(_userId, _amount, _gateway);
}