using Domain.User.ValueObjects; using Domain.Wallet.Aggregates; using Domain.Wallet.ValueObjects; using SharedKernel.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class WalletWithdrawalRequestBuilder { private static readonly Faker Faker = new();

private UserId _userId = UserId.NewId();
private Money _amount = Money.Create(100_000m, "IRT");
private IbanNumber _iban = new IbanNumberBuilder().Build();
private string _accountHolder = Faker.Name.FullName();
private WalletReservationId _reservationId = WalletReservationId.NewId();
private string? _description;

public WalletWithdrawalRequestBuilder WithUserId(UserId userId)
{
    _userId = userId;
    return this;
}

public WalletWithdrawalRequestBuilder WithAmount(decimal amount, string currency = "IRT")
{
    _amount = Money.Create(amount, currency);
    return this;
}

public WalletWithdrawalRequestBuilder WithAmount(Money amount)
{
    _amount = amount;
    return this;
}

public WalletWithdrawalRequestBuilder WithIban(IbanNumber iban)
{
    _iban = iban;
    return this;
}

public WalletWithdrawalRequestBuilder WithAccountHolder(string accountHolder)
{
    _accountHolder = accountHolder;
    return this;
}

public WalletWithdrawalRequestBuilder WithReservationId(WalletReservationId reservationId)
{
    _reservationId = reservationId;
    return this;
}

public WalletWithdrawalRequestBuilder WithDescription(string? description)
{
    _description = description;
    return this;
}

public WalletWithdrawalRequest Build() =>
    WalletWithdrawalRequest.Create(_userId, _amount, _iban, _accountHolder, _reservationId, _description);
}