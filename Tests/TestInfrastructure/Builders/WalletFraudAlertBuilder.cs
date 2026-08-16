using Domain.User.ValueObjects; using Domain.Wallet.Aggregates; using Domain.Wallet.Enums; using Domain.Wallet.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class WalletFraudAlertBuilder { private static readonly Faker Faker = new();

private WalletId _walletId = WalletId.NewId();
private UserId _userId = UserId.NewId();
private string _ruleName = Faker.PickRandom(new[] { "HighAmountRule", "VelocityRule", "GeoAnomalyRule" });
private FraudAlertSeverity _severity = FraudAlertSeverity.Medium;
private string _description = Faker.Lorem.Sentence();
private string? _metadata;

public WalletFraudAlertBuilder WithWalletId(WalletId walletId)
{
    _walletId = walletId;
    return this;
}

public WalletFraudAlertBuilder WithUserId(UserId userId)
{
    _userId = userId;
    return this;
}

public WalletFraudAlertBuilder WithRuleName(string ruleName)
{
    _ruleName = ruleName;
    return this;
}

public WalletFraudAlertBuilder WithSeverity(FraudAlertSeverity severity)
{
    _severity = severity;
    return this;
}

public WalletFraudAlertBuilder WithDescription(string description)
{
    _description = description;
    return this;
}

public WalletFraudAlertBuilder WithMetadata(string? metadata)
{
    _metadata = metadata;
    return this;
}

public WalletFraudAlert Build() =>
    WalletFraudAlert.Raise(_walletId, _userId, _ruleName, _severity, _description, _metadata);
}