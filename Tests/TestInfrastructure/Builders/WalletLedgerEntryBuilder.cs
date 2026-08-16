using Domain.User.ValueObjects; using Domain.Wallet.Entities; using Domain.Wallet.ValueObjects; using SharedKernel.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class WalletLedgerEntryBuilder { private static readonly Faker Faker = new();

private WalletId _walletId = WalletId.NewId();
private UserId _ownerId = UserId.NewId();
private Money _amount = Money.Create(10_000m, "IRT");
private Money _balanceAfter = Money.Create(10_000m, "IRT");
private string? _description = Faker.Lorem.Sentence();
private string _referenceId = Guid.NewGuid().ToString("N");
private string? _idempotencyKey;
private string? _correlationId;
private bool _isCredit = true;

public WalletLedgerEntryBuilder WithWalletId(WalletId walletId)
{
    _walletId = walletId;
    return this;
}

public WalletLedgerEntryBuilder WithOwnerId(UserId ownerId)
{
    _ownerId = ownerId;
    return this;
}

public WalletLedgerEntryBuilder WithAmount(decimal amount, string currency = "IRT")
{
    _amount = Money.Create(amount, currency);
    return this;
}

public WalletLedgerEntryBuilder WithBalanceAfter(decimal balanceAfter, string currency = "IRT")
{
    _balanceAfter = Money.Create(balanceAfter, currency);
    return this;
}

public WalletLedgerEntryBuilder WithDescription(string? description)
{
    _description = description;
    return this;
}

public WalletLedgerEntryBuilder WithReferenceId(string referenceId)
{
    _referenceId = referenceId;
    return this;
}

public WalletLedgerEntryBuilder WithIdempotencyKey(string? idempotencyKey)
{
    _idempotencyKey = idempotencyKey;
    return this;
}

public WalletLedgerEntryBuilder WithCorrelationId(string? correlationId)
{
    _correlationId = correlationId;
    return this;
}

public WalletLedgerEntryBuilder AsCredit()
{
    _isCredit = true;
    return this;
}

public WalletLedgerEntryBuilder AsDebit()
{
    _isCredit = false;
    return this;
}

public WalletLedgerEntry Build() => _isCredit
    ? WalletLedgerEntry.NewCredit(_walletId, _ownerId, _amount, _balanceAfter, _description, _referenceId, _idempotencyKey, _correlationId)
    : WalletLedgerEntry.NewDebit(_walletId, _ownerId, _amount, _balanceAfter, _description, _referenceId, _idempotencyKey, _correlationId);
}