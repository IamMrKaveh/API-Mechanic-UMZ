using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.ValueObjects;
using Users = Domain.User.Aggregates.User;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Infrastructure.Wallet.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    private async Task<Users> SeedActiveUserAsync()
    {
        var user = new UserBuilder().Build();
        user.ClearDomainEvents();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<Wallets> SeedWalletAsync(Users owner, string currency = "IRT")
    {
        var wallet = new WalletBuilder().WithOwnerId(owner.Id).WithCurrency(currency).Build();
        wallet.ClearDomainEvents();
        await _context.Wallets.AddAsync(wallet);
        await _context.SaveChangesAsync();
        return wallet;
    }

    [Fact]
    public async Task Persist_Wallet_RoundTripsAllScalarProperties()
    {
        var owner = await SeedActiveUserAsync();
        var wallet = await SeedWalletAsync(owner, "IRT");

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Wallets.FirstOrDefaultAsync(w => w.Id == wallet.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(wallet.Id);
        loaded.OwnerId.ShouldBe(owner.Id);
        loaded.Balance.Amount.ShouldBe(0m);
        loaded.Balance.Currency.ShouldBe("IRT");
        loaded.IsActive.ShouldBeTrue();
        loaded.FreezeReason.ShouldBeNull();
        loaded.FrozenAt.ShouldBeNull();
        loaded.FrozenBy.ShouldBeNull();
    }

    [Fact]
    public async Task Persist_Wallet_StoresBalanceInOwnedColumnsCurrentBalanceAndBalanceCurrency()
    {
        var owner = await SeedActiveUserAsync();
        var wallet = await SeedWalletAsync(owner);
        wallet.Credit(Money.Create(250_000m, "IRT"), "seed", "seed-ref-1");
        _context.Wallets.Update(wallet);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var rawColumns = await _context.Wallets
            .Where(w => w.Id == wallet.Id)
            .Select(w => new { Balance = w.Balance.Amount, Currency = w.Balance.Currency })
            .FirstOrDefaultAsync();

        rawColumns.ShouldNotBeNull();
        rawColumns!.Balance.ShouldBe(250_000m);
        rawColumns.Currency.ShouldBe("IRT");
    }

    [Fact]
    public async Task Persist_Wallet_DefaultCurrencyIsIRTWhenCreatedWithoutOverride()
    {
        var owner = await SeedActiveUserAsync();
        var wallet = new WalletBuilder().WithOwnerId(owner.Id).Build();
        wallet.ClearDomainEvents();
        await _context.Wallets.AddAsync(wallet);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.Wallets.FirstAsync(w => w.Id == wallet.Id);

        loaded.Balance.Currency.ShouldBe("IRT");
    }

    [Fact]
    public async Task Persist_Wallet_DuplicateOwnerId_ThrowsDbUpdateExceptionDueToUniqueIndex()
    {
        var owner = await SeedActiveUserAsync();
        await SeedWalletAsync(owner);

        var duplicate = new WalletBuilder().WithOwnerId(owner.Id).Build();
        duplicate.ClearDomainEvents();
        await _context.Wallets.AddAsync(duplicate);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_Wallet_QueryFilterHidesWalletWhenOwnerIsInactive()
    {
        var owner = await SeedActiveUserAsync();
        var wallet = await SeedWalletAsync(owner);

        owner.Deactivate();
        _context.Users.Update(owner);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var visible = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == wallet.Id);
        var ignoringFilters = await _context.Wallets.IgnoreQueryFilters().FirstOrDefaultAsync(w => w.Id == wallet.Id);

        visible.ShouldBeNull();
        ignoringFilters.ShouldNotBeNull();
    }

    [Fact]
    public async Task Persist_Wallet_FreezePropertiesArePersistedWhenSet()
    {
        var owner = await SeedActiveUserAsync();
        var admin = await SeedActiveUserAsync();
        var wallet = await SeedWalletAsync(owner);

        wallet.Freeze("suspicious activity", admin.Id);
        _context.Wallets.Update(wallet);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.Wallets.FirstAsync(w => w.Id == wallet.Id);

        loaded.IsActive.ShouldBeFalse();
        loaded.FreezeReason.ShouldBe("suspicious activity");
        loaded.FrozenAt.ShouldNotBeNull();
        loaded.FrozenBy.ShouldBe(admin.Id);
    }

    [Fact]
    public async Task Persist_Wallet_FreezeReasonLongerThan500Characters_ThrowsOnSave()
    {
        var owner = await SeedActiveUserAsync();
        var admin = await SeedActiveUserAsync();
        var wallet = await SeedWalletAsync(owner);

        wallet.Freeze(new string('x', 501), admin.Id);
        _context.Wallets.Update(wallet);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_Wallet_XminConcurrencyTokenChangesAfterUpdate()
    {
        var owner = await SeedActiveUserAsync();
        var wallet = await SeedWalletAsync(owner);

        var initialXmin = _context.Entry(wallet).Property<uint>("xmin").CurrentValue;

        wallet.Credit(Money.Create(10_000m, "IRT"), "top-up", "ref-1");
        _context.Wallets.Update(wallet);
        await _context.SaveChangesAsync();

        var updatedXmin = _context.Entry(wallet).Property<uint>("xmin").CurrentValue;

        updatedXmin.ShouldNotBe(initialXmin);
    }

    [Fact]
    public async Task Persist_Wallet_ConcurrencyConflict_ThrowsDbUpdateConcurrencyException()
    {
        var owner = await SeedActiveUserAsync();
        var wallet = await SeedWalletAsync(owner);
        _context.ChangeTracker.Clear();

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var walletA = await contextA.Wallets.FirstAsync(w => w.Id == wallet.Id);
        var walletB = await contextB.Wallets.FirstAsync(w => w.Id == wallet.Id);

        walletA.Credit(Money.Create(100m, "IRT"), "credit-a", "ref-a");
        contextA.Wallets.Update(walletA);
        await contextA.SaveChangesAsync();

        walletB.Credit(Money.Create(200m, "IRT"), "credit-b", "ref-b");
        contextB.Wallets.Update(walletB);

        await Should.ThrowAsync<DbUpdateConcurrencyException>(async () => await contextB.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_Wallet_UsesTableNameWallets()
    {
        var entityType = _context.Model.FindEntityType(typeof(Wallets));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("Wallets");
    }

    [Fact]
    public async Task Persist_Wallet_MapsOwnerIdColumnAsUserId()
    {
        var entityType = _context.Model.FindEntityType(typeof(Wallets));
        var property = entityType!.FindProperty(nameof(Wallets.OwnerId));
        var storeObject = StoreObjectIdentifier.Table("Wallets", entityType.GetSchema());

        property!.GetColumnName(storeObject).ShouldBe("UserId");
    }

    [Fact]
    public async Task Persist_Wallet_HasUniqueIndexOnOwnerId()
    {
        var entityType = _context.Model.FindEntityType(typeof(Wallets));
        var index = entityType!.GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == "IX_Wallets_UserId");

        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public async Task Persist_Wallet_HasIndexOnIsActive()
    {
        var entityType = _context.Model.FindEntityType(typeof(Wallets));
        var index = entityType!.GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == "IX_Wallets_IsActive");

        index.ShouldNotBeNull();
    }

    [Fact]
    public async Task Persist_Wallet_MapsIdWithGuidConversion()
    {
        var owner = await SeedActiveUserAsync();
        var wallet = await SeedWalletAsync(owner);
        _context.ChangeTracker.Clear();

        var raw = await _context.Wallets
            .Where(w => w.Id == wallet.Id)
            .Select(w => new { RawId = EF.Property<Guid>(w, "Id") })
            .FirstOrDefaultAsync();

        raw.ShouldNotBeNull();
        raw!.RawId.ShouldBe(wallet.Id.Value);
    }

    [Fact]
    public async Task Persist_Wallet_CascadesDeleteToDebitRequests()
    {
        var owner = await SeedActiveUserAsync();
        var wallet = await SeedWalletAsync(owner);
        wallet.Credit(Money.Create(500_000m, "IRT"), "seed", "seed-ref");
        var requestId = WalletDebitRequestId.NewId();
        wallet.CreateDebitRequest(requestId, Money.Create(100_000m, "IRT"), "reason", null, UserId.From(owner.Id.Value), TimeSpan.FromHours(1));
        _context.Wallets.Update(wallet);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var walletToDelete = await _context.Wallets.FirstAsync(w => w.Id == wallet.Id);
        _context.Wallets.Remove(walletToDelete);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var walletAfter = await freshContext.Wallets.IgnoreQueryFilters().FirstOrDefaultAsync(w => w.Id == wallet.Id);
        var debitRequestExists = await freshContext.Set<WalletDebitRequest>()
            .AnyAsync(r => r.Id == requestId);

        walletAfter.ShouldBeNull();
        debitRequestExists.ShouldBeFalse();
    }

    [Fact]
    public async Task Persist_Wallet_BalanceCurrencyMaxLengthIs10()
    {
        var entityType = _context.Model.FindEntityType(typeof(Wallets));
        var balanceOwnedType = entityType!.FindNavigation(nameof(Wallets.Balance))!.TargetEntityType;
        var currencyProperty = balanceOwnedType.FindProperty(nameof(Money.Currency));

        currencyProperty!.GetMaxLength().ShouldBe(10);
    }
}
