using Domain.User.ValueObjects; using Domain.Wallet.Aggregates; using SharedKernel.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class WalletTransferBuilder { private static readonly Faker Faker = new();

private UserId _fromUserId = UserId.NewId();
private UserId _toUserId = UserId.NewId();
private Money _amount = Money.Create(50_000m, "IRT");
private string _otpHash = Faker.Random.Hash(64);
private TimeSpan _otpTtl = TimeSpan.FromMinutes(5);
private string? _description;

public WalletTransferBuilder FromUser(UserId userId)
{
    _fromUserId = userId;
    return this;
}

public WalletTransferBuilder ToUser(UserId userId)
{
    _toUserId = userId;
    return this;
}

public WalletTransferBuilder WithAmount(decimal amount, string currency = "IRT")
{
    _amount = Money.Create(amount, currency);
    return this;
}

public WalletTransferBuilder WithAmount(Money amount)
{
    _amount = amount;
    return this;
}

public WalletTransferBuilder WithOtpHash(string otpHash)
{
    _otpHash = otpHash;
    return this;
}

public WalletTransferBuilder WithOtpTtl(TimeSpan ttl)
{
    _otpTtl = ttl;
    return this;
}

public WalletTransferBuilder WithDescription(string? description)
{
    _description = description;
    return this;
}

public WalletTransfer Build() => WalletTransfer.Initiate(_fromUserId, _toUserId, _amount, _otpHash, _otpTtl, _description);
}