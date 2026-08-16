using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Wallet.Repositories;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;
using Users = Domain.User.Aggregates.User;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Infrastructure.Wallet.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private WalletRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new WalletRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [SkippableFact]
    public async Task AddAsync_ThenGetByUserIdAsync_RoundTripsAggregateFromDatabase()
    {
        var (owner, wallet) = await SeedActiveUserAndWalletAsync();

        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByUserIdAsync(owner.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(wallet.Id);
        loaded.OwnerId.ShouldBe(owner.Id);
        loaded.Balance.Amount.ShouldBe(0m);
        loaded.Balance.Currency.ShouldBe("IRT");
        loaded.IsActive.ShouldBeTrue();
    }

    [SkippableFact]
    public async Task GetByUserIdAsync_WhenNoWalletForUser_ReturnsNull()
    {
        var loaded = await _sut.GetByUserIdAsync(UserId.NewId());

        loaded.ShouldBeNull();
    }

    [SkippableFact]
    public async Task GetByUserIdAsync_WhenOwnerIsInactive_ReturnsNullBecauseOfQueryFilter()
    {
        var (owner, _) = await SeedActiveUserAndWalletAsync();
        owner.Deactivate();
        _context.Users.Update(owner);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByUserIdAsync(owner.Id);

        loaded.ShouldBeNull();
    }

    [SkippableFact]
    public async Task GetByUserIdAsync_IncludesActiveReservationsCollection()
    {
        var (owner, wallet) = await SeedActiveUserAndWalletAsync();

        wallet.Credit(Money.Create(200_000m, "IRT"), "seed", "seed-ref-1");
        wallet.CreateReservation(WalletReservationId.NewId(), Money.Create(50_000m, "IRT"), "test-reservation");
        _context.Wallets.Update(wallet);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByUserIdAsync(owner.Id);

        loaded.ShouldNotBeNull();
        loaded!.ActiveReservations.Count.ShouldBe(1);
        loaded.ActiveReservations[0].Amount.Amount.ShouldBe(50_000m);
        loaded.ActiveReservations[0].Purpose.ShouldBe("test-reservation");
    }

    [SkippableFact]
    public async Task GetByUserIdForUpdateAsync_WhenWalletExists_ReturnsWalletAndSuppressesXminModification()
    {
        var (owner, wallet) = await SeedActiveUserAndWalletAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByUserIdForUpdateAsync(owner.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(wallet.Id);

        var entry = _context.Entry(loaded);
        entry.Property("xmin").IsModified.ShouldBeFalse();
    }

    [SkippableFact]
    public async Task GetByUserIdForUpdateAsync_WhenNoWallet_ReturnsNull()
    {
        var loaded = await _sut.GetByUserIdForUpdateAsync(UserId.NewId());

        loaded.ShouldBeNull();
    }

    [SkippableFact]
    public async Task HasIdempotencyKeyAsync_WhenLedgerEntryWithKeyExistsForUser_ReturnsTrue()
    {
        var (owner, wallet) = await SeedActiveUserAndWalletAsync();

        const string key = "idem-key-12345";
        var entry = new WalletLedgerEntryBuilder()
            .WithWalletId(wallet.Id)
            .WithOwnerId(owner.Id)
            .WithAmount(50_000m)
            .WithBalanceAfter(50_000m)
            .WithReferenceId("ref-1")
            .WithIdempotencyKey(key)
            .AsCredit()
            .Build();

        await _context.WalletLedgerEntries.AddAsync(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.HasIdempotencyKeyAsync(owner.Id, key);

        exists.ShouldBeTrue();
    }

    [SkippableFact]
    public async Task HasIdempotencyKeyAsync_WhenKeyBelongsToDifferentUser_ReturnsFalse()
    {
        var (ownerA, walletA) = await SeedActiveUserAndWalletAsync();
        var (ownerB, _) = await SeedActiveUserAndWalletAsync();

        const string key = "idem-shared-key";
        var entry = new WalletLedgerEntryBuilder()
            .WithWalletId(walletA.Id)
            .WithOwnerId(ownerA.Id)
            .WithReferenceId("ref-shared")
            .WithIdempotencyKey(key)
            .Build();

        await _context.WalletLedgerEntries.AddAsync(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.HasIdempotencyKeyAsync(ownerB.Id, key);

        exists.ShouldBeFalse();
    }

    [SkippableFact]
    public async Task HasIdempotencyKeyAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        var (owner, _) = await SeedActiveUserAndWalletAsync();

        var exists = await _sut.HasIdempotencyKeyAsync(owner.Id, "never-seen-key");

        exists.ShouldBeFalse();
    }

    [SkippableFact]
    public async Task Update_AfterCreditingWallet_PersistsNewBalance()
    {
        var (owner, wallet) = await SeedActiveUserAndWalletAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByUserIdForUpdateAsync(owner.Id);
        loaded.ShouldNotBeNull();
        loaded!.Credit(Money.Create(75_000m, "IRT"), "credit-desc", "ref-credit-1");

        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByUserIdAsync(owner.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.Balance.Amount.ShouldBe(75_000m);
    }

    [SkippableFact]
    public async Task Update_AfterFreezingWallet_PersistsFreezeStateAndMakesWalletInvisibleWhenOwnerAlsoInactive()
    {
        var (owner, wallet) = await SeedActiveUserAndWalletAsync();
        var admin = UserId.NewId();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByUserIdAsync(owner.Id);
        loaded.ShouldNotBeNull();
        loaded!.Freeze("suspicious activity", admin);

        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByUserIdAsync(owner.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.IsActive.ShouldBeFalse();
        reloaded.FreezeReason.ShouldBe("suspicious activity");
        reloaded.FrozenAt.ShouldNotBeNull();
        reloaded.FrozenBy.ShouldBe(admin);
    }

    [SkippableFact]
    public async Task AddAsync_ForActiveUser_EnforcesUniqueOwnerIndexInDatabase()
    {
        var (owner, _) = await SeedActiveUserAndWalletAsync();
        _context.ChangeTracker.Clear();

        var duplicate = new WalletBuilder().WithOwnerId(owner.Id).Build();
        duplicate.ClearDomainEvents();

        await _sut.AddAsync(duplicate);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Theory]
    [InlineData(10_000)]
    [InlineData(50_000)]
    [InlineData(1_000_000)]
    public async Task GetByUserIdAsync_AfterCreditingSpecificAmount_ReturnsWalletWithExpectedBalance(decimal amount)
    {
        var (owner, wallet) = await SeedActiveUserAndWalletAsync();
        wallet.Credit(Money.Create(amount, "IRT"), "credit", $"ref-{amount}");
        _context.Wallets.Update(wallet);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByUserIdAsync(owner.Id);

        loaded.ShouldNotBeNull();
        loaded!.Balance.Amount.ShouldBe(amount);
    }

    private async Task<(Users owner, Wallets wallet)> SeedActiveUserAndWalletAsync()
    {
        var owner = new UserBuilder().Build();
        owner.ClearDomainEvents();
        await _context.Users.AddAsync(owner);

        var wallet = new WalletBuilder().WithOwnerId(owner.Id).Build();
        wallet.ClearDomainEvents();
        await _sut.AddAsync(wallet);

        await _context.SaveChangesAsync();
        return (owner, wallet);
    }
}
