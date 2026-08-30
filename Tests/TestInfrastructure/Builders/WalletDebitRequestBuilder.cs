using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Entities;
using Domain.Wallet.ValueObjects;
using SharedKernel.ValueObjects;

namespace Tests.TestInfrastructure.Builders;

public sealed class WalletDebitRequestBuilder
{
    private static readonly Faker Faker = new();

    private UserId _ownerId = UserId.NewId();
    private decimal _initialBalance = 500_000m;
    private decimal _amount = 100_000m;
    private string _reason = Faker.Lorem.Sentence(3);
    private string? _description;
    private UserId _requestedBy = UserId.NewId();
    private TimeSpan _expiry = TimeSpan.FromHours(72);
    private WalletDebitRequestId _requestId = WalletDebitRequestId.NewId();

    public WalletDebitRequestBuilder WithOwner(UserId ownerId)
    {
        _ownerId = ownerId;
        return this;
    }

    public WalletDebitRequestBuilder WithInitialBalance(decimal balance)
    {
        _initialBalance = balance;
        return this;
    }

    public WalletDebitRequestBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public WalletDebitRequestBuilder WithRequestId(WalletDebitRequestId requestId)
    {
        _requestId = requestId;
        return this;
    }

    public WalletDebitRequestBuilder WithRequestedBy(UserId requestedBy)
    {
        _requestedBy = requestedBy;
        return this;
    }

    public WalletDebitRequestBuilder WithExpiry(TimeSpan expiry)
    {
        _expiry = expiry;
        return this;
    }

    public (Wallet wallet, WalletDebitRequest request) Build()
    {
        var wallet = new WalletBuilder().WithOwnerId(_ownerId).Build();
        wallet.Credit(Money.Create(_initialBalance), "seed", Guid.NewGuid().ToString(), Guid.NewGuid().ToString("N"));
        wallet.CreateDebitRequest(
            _requestId,
            Money.Create(_amount),
            _reason,
            _description,
            _requestedBy,
            _expiry);
        var request = wallet.DebitRequests.Single(r => r.Id == _requestId);
        return (wallet, request);
    }
}

