using Domain.Wallet.Entities;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;
using Users = Domain.User.Aggregates.User;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Infrastructure.Wallet.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletLedgerEntryConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<(Users owner, Wallets wallet)> SeedOwnerAndWalletAsync()
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

    private async Task<WalletLedgerEntry> SeedCreditEntryAsync(
        Wallets wallet,
        Users owner,
        decimal amount = 10_000m,
        decimal balanceAfter = 10_000m,
        string? description = "credit-desc",
        string? idempotencyKey = null,
        string? correlationId = null)
    {
        var entry = WalletLedgerEntry.NewCredit(
            wallet.Id,
            owner.Id,
            Money.Create(amount, "IRT"),
            Money.Create(balanceAfter, "IRT"),
            description,
            Guid.NewGuid().ToString("N"),
            idempotencyKey,
            correlationId);
        await _context.WalletLedgerEntries.AddAsync(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    [Fact]
    public async Task Persist_LedgerEntry_RoundTripsCreditWithAllProperties()
    {
        var (owner, wallet) = await SeedOwnerAndWalletAsync();
        var entry = await SeedCreditEntryAsync(
            wallet, owner,
            amount: 25_000m,
            balanceAfter: 75_000m,
            description: "top-up",
            idempotencyKey: "idem-key",
            correlationId: "corr-id");
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.WalletLedgerEntries.FirstOrDefaultAsync(e => e.Id == entry.Id);

        loaded.ShouldNotBeNull();
        loaded!.WalletId.ShouldBe(wallet.Id);
        loaded.OwnerId.ShouldBe(owner.Id);
        loaded.Amount.Amount.ShouldBe(25_000m);
        loaded.Amount.Currency.ShouldBe("IRT");
        loaded.BalanceAfter.Amount.ShouldBe(75_000m);
        loaded.BalanceAfter.Currency.ShouldBe("IRT");
        loaded.TransactionType.ShouldBe(WalletTransactionType.Credit);
        loaded.Description.ShouldBe("top-up");
        loaded.ReferenceId.ShouldBe(entry.ReferenceId);
        loaded.IdempotencyKey.ShouldBe("idem-key");
        loaded.CorrelationId.ShouldBe("corr-id");
    }

    [Fact]
    public async Task Persist_LedgerEntry_TransactionTypeIsStoredAsString()
    {
        var (owner, wallet) = await SeedOwnerAndWalletAsync();
        var entry = await SeedCreditEntryAsync(wallet, owner);
        _context.ChangeTracker.Clear();

        var entryId = entry.Id.Value;
        var raw = await _context.Database
            .SqlQuery<string>($"SELECT \"TransactionType\" AS \"Value\" FROM \"WalletLedgerEntries\" WHERE \"Id\" = {entryId}")
            .FirstOrDefaultAsync();

        raw.ShouldBe(WalletTransactionType.Credit.ToString());
    }

    [Fact]
    public async Task Persist_LedgerEntry_OccurredAtIsMappedToCreatedAtColumn()
    {
        var (owner, wallet) = await SeedOwnerAndWalletAsync();
        var entry = await SeedCreditEntryAsync(wallet, owner);

        var entityType = _context.Model.FindEntityType(typeof(WalletLedgerEntry));
        var property = entityType!.FindProperty(nameof(WalletLedgerEntry.OccurredAt));
        var storeObject = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());

        property!.GetColumnName(storeObject).ShouldBe("CreatedAt");
    }

    [Fact]
    public async Task Persist_LedgerEntry_OwnerIdIsMappedToUserIdColumn()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletLedgerEntry));
        var property = entityType!.FindProperty(nameof(WalletLedgerEntry.OwnerId));
        var storeObject = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());

        property!.GetColumnName(storeObject).ShouldBe("UserId");
    }

    [Fact]
    public async Task Persist_LedgerEntry_AmountIsMappedToAmountDeltaColumn()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletLedgerEntry));
        var amountOwnedType = entityType!.FindNavigation(nameof(WalletLedgerEntry.Amount))!.TargetEntityType;
        var amountProperty = amountOwnedType.FindProperty(nameof(Money.Amount));
        var storeObject = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());

        amountProperty!.GetColumnName(storeObject).ShouldBe("AmountDelta");
    }

    [Fact]
    public async Task Persist_LedgerEntry_DuplicateIdempotencyKey_ThrowsDueToUniqueFilteredIndex()
    {
        var (owner, wallet) = await SeedOwnerAndWalletAsync();
        var duplicateKey = "duplicate-idem-key";
        await SeedCreditEntryAsync(wallet, owner, idempotencyKey: duplicateKey);

        var duplicate = WalletLedgerEntry.NewCredit(
            wallet.Id, owner.Id,
            Money.Create(1_000m, "IRT"), Money.Create(2_000m, "IRT"),
            "desc", Guid.NewGuid().ToString("N"),
            duplicateKey);
        await _context.WalletLedgerEntries.AddAsync(duplicate);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_LedgerEntry_TwoNullIdempotencyKeys_AreAllowedByFilteredIndex()
    {
        var (owner, wallet) = await SeedOwnerAndWalletAsync();

        await SeedCreditEntryAsync(wallet, owner, idempotencyKey: null);
        var second = WalletLedgerEntry.NewCredit(
            wallet.Id, owner.Id,
            Money.Create(1_000m, "IRT"), Money.Create(2_000m, "IRT"),
            "desc", Guid.NewGuid().ToString("N"),
            null);
        await _context.WalletLedgerEntries.AddAsync(second);

        await Should.NotThrowAsync(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_LedgerEntry_ReferenceIdIsRequired()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletLedgerEntry));
        var property = entityType!.FindProperty(nameof(WalletLedgerEntry.ReferenceId));

        property!.IsNullable.ShouldBeFalse();
        property.GetMaxLength().ShouldBe(200);
    }

    [Fact]
    public async Task Persist_LedgerEntry_DebitEntryPersistsAsDebitTransactionType()
    {
        var (owner, wallet) = await SeedOwnerAndWalletAsync();
        var entry = WalletLedgerEntry.NewDebit(
            wallet.Id, owner.Id,
            Money.Create(5_000m, "IRT"), Money.Create(0m, "IRT"),
            "purchase", "order-123", null);
        await _context.WalletLedgerEntries.AddAsync(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.WalletLedgerEntries.FirstAsync(e => e.Id == entry.Id);

        loaded.TransactionType.ShouldBe(WalletTransactionType.Debit);
        loaded.Amount.Amount.ShouldBe(5_000m);
    }

    [Fact]
    public async Task Persist_LedgerEntry_QueryFilterHidesEntriesForInactiveOwner()
    {
        var (owner, wallet) = await SeedOwnerAndWalletAsync();
        var entry = await SeedCreditEntryAsync(wallet, owner);

        owner.Deactivate();
        _context.Users.Update(owner);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var visible = await _context.WalletLedgerEntries.FirstOrDefaultAsync(e => e.Id == entry.Id);
        var ignoringFilters = await _context.WalletLedgerEntries.IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == entry.Id);

        visible.ShouldBeNull();
        ignoringFilters.ShouldNotBeNull();
    }

    [Fact]
    public async Task Persist_LedgerEntry_ForeignKeyToNonexistentWallet_ThrowsOnSave()
    {
        var owner = new UserBuilder().Build();
        owner.ClearDomainEvents();
        await _context.Users.AddAsync(owner);
        await _context.SaveChangesAsync();

        var entry = WalletLedgerEntry.NewCredit(
            WalletId.NewId(),
            owner.Id,
            Money.Create(1_000m, "IRT"), Money.Create(1_000m, "IRT"),
            "d", Guid.NewGuid().ToString("N"), null);
        await _context.WalletLedgerEntries.AddAsync(entry);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_LedgerEntry_DeletingWalletWithLedgerEntries_ThrowsBecauseRestrictOnDelete()
    {
        var (owner, wallet) = await SeedOwnerAndWalletAsync();
        await SeedCreditEntryAsync(wallet, owner);
        _context.ChangeTracker.Clear();

        var walletToDelete = await _context.Wallets.FirstAsync(w => w.Id == wallet.Id);
        _context.Wallets.Remove(walletToDelete);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Theory]
    [InlineData("IX_WalletLedgerEntries_WalletId_OccurredAt")]
    [InlineData("IX_WalletLedgerEntries_UserId_OccurredAt")]
    [InlineData("IX_WalletLedgerEntries_IdempotencyKey")]
    [InlineData("IX_WalletLedgerEntries_CorrelationId")]
    [InlineData("IX_WalletLedgerEntries_DebitRequestId")]
    [InlineData("IX_WalletLedgerEntries_WithdrawalRequestId")]
    [InlineData("IX_WalletLedgerEntries_TransferId")]
    [InlineData("IX_WalletLedgerEntries_TopUpId")]
    public void Model_LedgerEntry_DeclaresExpectedIndex(string indexName)
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletLedgerEntry));
        var index = entityType!.GetIndexes().FirstOrDefault(i => i.GetDatabaseName() == indexName);

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_LedgerEntry_IdempotencyKeyIndexIsUnique()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletLedgerEntry));
        var index = entityType!.GetIndexes()
            .First(i => i.GetDatabaseName() == "IX_WalletLedgerEntries_IdempotencyKey");

        index.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public async Task Persist_LedgerEntry_ForeignKeyToRelatedTopUp_IsSetNullOnDelete()
    {
        var (owner, wallet) = await SeedOwnerAndWalletAsync();
        var topUp = new WalletTopUpBuilder().WithUserId(owner.Id).WithAmount(50_000m).Build();
        topUp.ClearDomainEvents();
        await _context.WalletTopUps.AddAsync(topUp);
        await _context.SaveChangesAsync();

        var entry = WalletLedgerEntry.NewCredit(
            wallet.Id, owner.Id,
            Money.Create(50_000m, "IRT"), Money.Create(50_000m, "IRT"),
            "top-up", Guid.NewGuid().ToString("N"),
            null, null,
            topUpId: topUp.Id);
        await _context.WalletLedgerEntries.AddAsync(entry);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var topUpToDelete = await _context.WalletTopUps.FirstAsync(t => t.Id == topUp.Id);
        _context.WalletTopUps.Remove(topUpToDelete);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.WalletLedgerEntries.FirstAsync(e => e.Id == entry.Id);

        loaded.TopUpId.ShouldBeNull();
    }
}
