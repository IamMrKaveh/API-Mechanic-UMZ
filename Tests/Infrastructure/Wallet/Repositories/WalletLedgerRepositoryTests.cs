using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Infrastructure.Persistence.Context;
using Infrastructure.Wallet.Repositories;
using Tests.TestInfrastructure.Builders;
using Users = Domain.User.Aggregates.User;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Infrastructure.Wallet.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletLedgerRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private WalletLedgerRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new WalletLedgerRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [Fact]
    public async Task AddAsync_PersistsEntryAndItCanBeQueriedBack()
    {
        var (owner, wallet) = await SeedActiveUserAndWalletAsync();

        var entry = new WalletLedgerEntryBuilder()
            .WithWalletId(wallet.Id)
            .WithOwnerId(owner.Id)
            .WithAmount(25_000m)
            .WithBalanceAfter(25_000m)
            .WithReferenceId("ref-persist-1")
            .AsCredit()
            .Build();

        await _sut.AddAsync(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.WalletLedgerEntries.FirstOrDefaultAsync(e => e.Id == entry.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.WalletId.ShouldBe(wallet.Id);
        reloaded.OwnerId.ShouldBe(owner.Id);
        reloaded.Amount.Amount.ShouldBe(25_000m);
        reloaded.BalanceAfter.Amount.ShouldBe(25_000m);
        reloaded.ReferenceId.ShouldBe("ref-persist-1");
    }

    [Fact]
    public async Task AddAsync_WithNullEntry_Throws()
    {
        await Should.ThrowAsync<ArgumentNullException>(async () => await _sut.AddAsync(null!));
    }

    [Fact]
    public async Task HasIdempotencyKeyAsync_WithGlobalOverload_WhenKeyExists_ReturnsTrue()
    {
        var (owner, wallet) = await SeedActiveUserAndWalletAsync();

        const string key = "global-idem-key";
        var entry = new WalletLedgerEntryBuilder()
            .WithWalletId(wallet.Id)
            .WithOwnerId(owner.Id)
            .WithReferenceId("ref-global-1")
            .WithIdempotencyKey(key)
            .Build();

        await _sut.AddAsync(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.HasIdempotencyKeyAsync(key);

        exists.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HasIdempotencyKeyAsync_WithGlobalOverload_WhenKeyIsBlank_ReturnsFalse(string? key)
    {
        var exists = await _sut.HasIdempotencyKeyAsync(key);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task HasIdempotencyKeyAsync_WithGlobalOverload_WhenKeyDoesNotExist_ReturnsFalse()
    {
        var exists = await _sut.HasIdempotencyKeyAsync("never-seen-global-key");

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task HasIdempotencyKeyAsync_WithOwnerOverload_WhenKeyExistsForOwner_ReturnsTrue()
    {
        var (owner, wallet) = await SeedActiveUserAndWalletAsync();

        const string key = "owner-idem-key";
        var entry = new WalletLedgerEntryBuilder()
            .WithWalletId(wallet.Id)
            .WithOwnerId(owner.Id)
            .WithReferenceId("ref-owner-1")
            .WithIdempotencyKey(key)
            .Build();

        await _sut.AddAsync(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.HasIdempotencyKeyAsync(owner.Id, key);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task HasIdempotencyKeyAsync_WithOwnerOverload_WhenKeyBelongsToDifferentOwner_ReturnsFalse()
    {
        var (ownerA, walletA) = await SeedActiveUserAndWalletAsync();
        var (ownerB, _) = await SeedActiveUserAndWalletAsync();

        const string key = "cross-owner-idem-key";
        var entry = new WalletLedgerEntryBuilder()
            .WithWalletId(walletA.Id)
            .WithOwnerId(ownerA.Id)
            .WithReferenceId("ref-cross-1")
            .WithIdempotencyKey(key)
            .Build();

        await _sut.AddAsync(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.HasIdempotencyKeyAsync(ownerB.Id, key);

        exists.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HasIdempotencyKeyAsync_WithOwnerOverload_WhenKeyIsBlank_ReturnsFalse(string? key)
    {
        var exists = await _sut.HasIdempotencyKeyAsync(UserId.NewId(), key!);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task HasIdempotencyKeyAsync_WithGlobalOverload_IgnoresQueryFiltersAndFindsEntryWhenOwnerIsInactive()
    {
        var (owner, wallet) = await SeedActiveUserAndWalletAsync();

        const string key = "inactive-owner-idem";
        var entry = new WalletLedgerEntryBuilder()
            .WithWalletId(wallet.Id)
            .WithOwnerId(owner.Id)
            .WithReferenceId("ref-inactive-1")
            .WithIdempotencyKey(key)
            .Build();

        await _sut.AddAsync(entry);
        owner.Deactivate();
        _context.Users.Update(owner);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.HasIdempotencyKeyAsync(key);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task AddAsync_TwoEntriesWithSameIdempotencyKey_ViolatesUniqueIndex()
    {
        var (owner, wallet) = await SeedActiveUserAndWalletAsync();
        const string key = "dup-idem-key";

        var first = new WalletLedgerEntryBuilder()
            .WithWalletId(wallet.Id)
            .WithOwnerId(owner.Id)
            .WithReferenceId("ref-a")
            .WithIdempotencyKey(key)
            .Build();

        var second = new WalletLedgerEntryBuilder()
            .WithWalletId(wallet.Id)
            .WithOwnerId(owner.Id)
            .WithReferenceId("ref-b")
            .WithIdempotencyKey(key)
            .Build();

        await _sut.AddAsync(first);
        await _sut.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task AddAsync_PersistsTransactionTypeAsStringInDatabase()
    {
        var (owner, wallet) = await SeedActiveUserAndWalletAsync();

        var debit = new WalletLedgerEntryBuilder()
            .WithWalletId(wallet.Id)
            .WithOwnerId(owner.Id)
            .WithAmount(30_000m)
            .WithBalanceAfter(0m)
            .WithReferenceId("ref-debit-1")
            .AsDebit()
            .Build();

        await _sut.AddAsync(debit);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.WalletLedgerEntries.FirstOrDefaultAsync(e => e.Id == debit.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.TransactionType.ShouldBe(WalletTransactionType.Debit);
    }

    private async Task<(Users owner, Wallets wallet)> SeedActiveUserAndWalletAsync()
    {
        var owner = new UserBuilder().Build();
        owner.ClearDomainEvents();
        await _context.Users.AddAsync(owner);

        var wallet = new WalletBuilder().WithOwnerId(owner.Id).Build();
        wallet.ClearDomainEvents();
        await _context.Wallets.AddAsync(wallet);

        await _context.SaveChangesAsync();
        return (owner, wallet);
    }
}
