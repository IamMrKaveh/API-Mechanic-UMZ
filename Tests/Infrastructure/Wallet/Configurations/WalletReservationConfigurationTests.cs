using Domain.Wallet.Entities;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;
using Infrastructure.Persistence.Context;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;
using Users = Domain.User.Aggregates.User;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Infrastructure.Wallet.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletReservationConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    private async Task<(Wallets wallet, WalletReservation reservation)> SeedActiveReservationAsync(
        decimal openingBalance = 500_000m,
        decimal reservationAmount = 100_000m,
        string purpose = "hold-for-order")
    {
        var owner = await SeedActiveUserAsync();
        var wallet = new WalletBuilder().WithOwnerId(owner.Id).Build();
        wallet.Credit(Money.Create(openingBalance, "IRT"), "seed", "seed-ref");
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Money.Create(reservationAmount, "IRT"), purpose);
        wallet.ClearDomainEvents();

        await _context.Wallets.AddAsync(wallet);
        await _context.SaveChangesAsync();

        var reservation = wallet.ActiveReservations.Single(r => r.Id == reservationId);
        return (wallet, reservation);
    }

    [Fact]
    public async Task Persist_Reservation_RoundTripsAllScalarProperties()
    {
        var (wallet, reservation) = await SeedActiveReservationAsync(
            openingBalance: 500_000m,
            reservationAmount: 120_000m,
            purpose: "hold-order-x");
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.WalletReservations.FirstOrDefaultAsync(r => r.Id == reservation.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(reservation.Id);
        loaded.WalletId.ShouldBe(wallet.Id);
        loaded.Amount.Amount.ShouldBe(120_000m);
        loaded.Amount.Currency.ShouldBe("IRT");
        loaded.Purpose.ShouldBe("hold-order-x");
        loaded.Status.ShouldBe(WalletReservationStatus.Active);
        loaded.ExpiresAt.ShouldBeNull();
        loaded.ResolvedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Persist_Reservation_StatusIsStoredAsString()
    {
        var (_, reservation) = await SeedActiveReservationAsync();
        _context.ChangeTracker.Clear();

        var raw = await _context.WalletReservations
            .Where(r => r.Id == reservation.Id)
            .Select(r => EF.Property<string>(r, nameof(WalletReservation.Status)))
            .FirstOrDefaultAsync();

        raw.ShouldBe(WalletReservationStatus.Active.ToString());
    }

    [Fact]
    public async Task Persist_Reservation_ReleaseTransitionPersistsResolvedAtAndReleasedStatus()
    {
        var (wallet, reservation) = await SeedActiveReservationAsync();
        var loadedWallet = await _context.Wallets.FirstAsync(w => w.Id == wallet.Id);
        loadedWallet.ReleaseReservation(reservation.Id);
        _context.Wallets.Update(loadedWallet);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.WalletReservations
            .IgnoreQueryFilters()
            .FirstAsync(r => r.Id == reservation.Id);

        loaded.Status.ShouldBe(WalletReservationStatus.Released);
        loaded.ResolvedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Persist_Reservation_PurposeLongerThan200Characters_ThrowsOnSave()
    {
        var owner = await SeedActiveUserAsync();
        var wallet = new WalletBuilder().WithOwnerId(owner.Id).Build();
        wallet.Credit(Money.Create(500_000m, "IRT"), "seed", "seed-ref");
        wallet.CreateReservation(WalletReservationId.NewId(), Money.Create(100_000m, "IRT"), new string('p', 201));
        wallet.ClearDomainEvents();

        await _context.Wallets.AddAsync(wallet);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_Reservation_IsRemovedWhenParentWalletIsDeleted()
    {
        var (wallet, reservation) = await SeedActiveReservationAsync();
        _context.ChangeTracker.Clear();

        var walletToDelete = await _context.Wallets.FirstAsync(w => w.Id == wallet.Id);
        _context.Wallets.Remove(walletToDelete);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var exists = await freshContext.WalletReservations
            .IgnoreQueryFilters()
            .AnyAsync(r => r.Id == reservation.Id);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task Persist_Reservation_QueryFilterHidesReservationWhenOwnerIsInactive()
    {
        var owner = await SeedActiveUserAsync();
        var wallet = new WalletBuilder().WithOwnerId(owner.Id).Build();
        wallet.Credit(Money.Create(500_000m, "IRT"), "seed", "seed-ref");
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Money.Create(100_000m, "IRT"), "hold");
        wallet.ClearDomainEvents();
        await _context.Wallets.AddAsync(wallet);
        await _context.SaveChangesAsync();

        owner.Deactivate();
        _context.Users.Update(owner);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var visible = await _context.WalletReservations.FirstOrDefaultAsync(r => r.Id == reservationId);
        var ignoringFilters = await _context.WalletReservations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        visible.ShouldBeNull();
        ignoringFilters.ShouldNotBeNull();
    }

    [Fact]
    public async Task Persist_Reservation_WalletIdShadowPropertyIsMappedAsColumn()
    {
        var (wallet, reservation) = await SeedActiveReservationAsync();
        _context.ChangeTracker.Clear();

        var raw = await _context.WalletReservations
            .Where(r => r.Id == reservation.Id)
            .Select(r => EF.Property<Guid>(r, "WalletId"))
            .FirstOrDefaultAsync();

        raw.ShouldBe(wallet.Id.Value);
    }

    [Fact]
    public void Model_Reservation_HasIndexOnWalletId()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletReservation));
        var index = entityType!.GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == "IX_WalletReservations_WalletId");

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_Reservation_HasCompositeIndexOnWalletIdAndStatus()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletReservation));
        var index = entityType!.GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == "IX_WalletReservations_WalletId_Status");

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_Reservation_AmountIsMappedToAmountAndAmountCurrencyColumns()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletReservation));
        var amountOwnedType = entityType!.FindNavigation(nameof(WalletReservation.Amount))!.TargetEntityType;
        var storeObject = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());

        amountOwnedType.FindProperty(nameof(Money.Amount))!.GetColumnName(storeObject).ShouldBe("Amount");
        amountOwnedType.FindProperty(nameof(Money.Currency))!.GetColumnName(storeObject).ShouldBe("AmountCurrency");
    }

    [Fact]
    public void Model_Reservation_StatusColumnMaxLengthIs20()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletReservation));
        var property = entityType!.FindProperty(nameof(WalletReservation.Status));

        property!.GetMaxLength().ShouldBe(20);
    }
}
