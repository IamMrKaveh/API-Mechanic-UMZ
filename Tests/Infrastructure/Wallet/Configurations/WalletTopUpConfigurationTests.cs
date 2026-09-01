using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Infrastructure.Persistence.Context;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Wallet.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletTopUpConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    private async Task<WalletTopUp> SeedTopUpAsync(
        decimal amount = 50_000m,
        string gateway = "zarinpal")
    {
        var topUp = new WalletTopUpBuilder()
            .WithAmount(amount)
            .WithGateway(gateway)
            .Build();
        topUp.ClearDomainEvents();

        await _context.WalletTopUps.AddAsync(topUp);
        await _context.SaveChangesAsync();
        return topUp;
    }

    [Fact]
    public async Task Persist_TopUp_RoundTripsAllScalarProperties()
    {
        var topUp = await SeedTopUpAsync(amount: 100_000m, gateway: "idpay");
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.WalletTopUps.FirstOrDefaultAsync(t => t.Id == topUp.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(topUp.Id);
        loaded.UserId.ShouldBe(topUp.UserId);
        loaded.Amount.Amount.ShouldBe(100_000m);
        loaded.Amount.Currency.ShouldBe("IRT");
        loaded.Gateway.ShouldBe("idpay");
        loaded.GatewayAuthority.ShouldBeNull();
        loaded.GatewayRefId.ShouldBeNull();
        loaded.Status.ShouldBe(WalletTopUpStatus.Pending);
        loaded.CompletedAt.ShouldBeNull();
        loaded.FailureReason.ShouldBeNull();
    }

    [Fact]
    public async Task Persist_TopUp_StatusIsStoredAsString()
    {
        var topUp = await SeedTopUpAsync();
        _context.ChangeTracker.Clear();

        var raw = await _context.WalletTopUps
            .Where(t => t.Id == topUp.Id)
            .Select(t => EF.Property<string>(t, nameof(WalletTopUp.Status)))
            .FirstOrDefaultAsync();

        raw.ShouldBe(WalletTopUpStatus.Pending.ToString());
    }

    [Fact]
    public async Task Persist_TopUp_SucceededTransitionPersistsGatewayRefIdAndCompletedAt()
    {
        var topUp = await SeedTopUpAsync();
        topUp.MarkAuthorityIssued("auth-abc123");
        topUp.MarkSucceeded("gateway-ref-987");
        _context.WalletTopUps.Update(topUp);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.WalletTopUps.FirstAsync(t => t.Id == topUp.Id);

        loaded.Status.ShouldBe(WalletTopUpStatus.Succeeded);
        loaded.GatewayAuthority.ShouldBe("auth-abc123");
        loaded.GatewayRefId.ShouldBe("gateway-ref-987");
        loaded.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Persist_TopUp_FailedTransitionPersistsFailureReason()
    {
        var topUp = await SeedTopUpAsync();
        topUp.MarkFailed("gateway declined");
        _context.WalletTopUps.Update(topUp);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.WalletTopUps.FirstAsync(t => t.Id == topUp.Id);

        loaded.Status.ShouldBe(WalletTopUpStatus.Failed);
        loaded.FailureReason.ShouldBe("gateway declined");
        loaded.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Persist_TopUp_DuplicateGatewayAuthority_ThrowsDueToUniqueFilteredIndex()
    {
        var first = await SeedTopUpAsync();
        first.MarkAuthorityIssued("duplicate-authority");
        _context.WalletTopUps.Update(first);
        await _context.SaveChangesAsync();

        var second = new WalletTopUpBuilder().Build();
        second.ClearDomainEvents();
        second.MarkAuthorityIssued("duplicate-authority");
        await _context.WalletTopUps.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_TopUp_TwoNullGatewayAuthorities_AreAllowedByFilteredIndex()
    {
        var first = await SeedTopUpAsync();
        var second = new WalletTopUpBuilder().Build();
        second.ClearDomainEvents();
        await _context.WalletTopUps.AddAsync(second);

        await Should.NotThrowAsync(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_TopUp_GatewayLongerThan64Characters_ThrowsOnSave()
    {
        var topUp = new WalletTopUpBuilder().WithGateway(new string('g', 65)).Build();
        topUp.ClearDomainEvents();
        await _context.WalletTopUps.AddAsync(topUp);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_TopUp_FailureReasonLongerThan500Characters_ThrowsOnSave()
    {
        var topUp = await SeedTopUpAsync();
        topUp.MarkFailed(new string('r', 501));
        _context.WalletTopUps.Update(topUp);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_TopUp_XminConcurrencyTokenChangesAfterUpdate()
    {
        var topUp = await SeedTopUpAsync();
        var initialXmin = _context.Entry(topUp).Property<uint>("xmin").CurrentValue;

        topUp.MarkAuthorityIssued("some-authority");
        _context.WalletTopUps.Update(topUp);
        await _context.SaveChangesAsync();

        var updatedXmin = _context.Entry(topUp).Property<uint>("xmin").CurrentValue;
        updatedXmin.ShouldNotBe(initialXmin);
    }

    [Fact]
    public async Task Persist_TopUp_ConcurrencyConflict_ThrowsDbUpdateConcurrencyException()
    {
        var topUp = await SeedTopUpAsync();
        _context.ChangeTracker.Clear();

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var topUpA = await contextA.WalletTopUps.FirstAsync(t => t.Id == topUp.Id);
        var topUpB = await contextB.WalletTopUps.FirstAsync(t => t.Id == topUp.Id);

        topUpA.MarkAuthorityIssued("auth-a");
        contextA.WalletTopUps.Update(topUpA);
        await contextA.SaveChangesAsync();

        topUpB.MarkAuthorityIssued("auth-b");
        contextB.WalletTopUps.Update(topUpB);

        await Should.ThrowAsync<DbUpdateConcurrencyException>(async () => await contextB.SaveChangesAsync());
    }

    [Fact]
    public void Model_TopUp_UsesTableNameWalletTopUps()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletTopUp));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("WalletTopUps");
    }

    [Theory]
    [InlineData("IX_WalletTopUps_UserId")]
    [InlineData("IX_WalletTopUps_Status")]
    [InlineData("IX_WalletTopUps_CreatedAt")]
    [InlineData("IX_WalletTopUps_GatewayAuthority")]
    public void Model_TopUp_DeclaresExpectedIndex(string indexName)
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletTopUp));
        var index = entityType!.GetIndexes().FirstOrDefault(i => i.GetDatabaseName() == indexName);

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_TopUp_GatewayAuthorityIndexIsUnique()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletTopUp));
        var index = entityType!.GetIndexes()
            .First(i => i.GetDatabaseName() == "IX_WalletTopUps_GatewayAuthority");

        index.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void Model_TopUp_UserIdIsMappedToUserIdColumn()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletTopUp));
        var property = entityType!.FindProperty(nameof(WalletTopUp.UserId));
        var storeObject = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());

        property!.GetColumnName(storeObject).ShouldBe("UserId");
    }

    [Fact]
    public void Model_TopUp_DomainEventsAreIgnored()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletTopUp));
        var domainEventsProperty = entityType!.FindProperty(nameof(WalletTopUp.DomainEvents));
        var domainEventsNavigation = entityType.FindNavigation(nameof(WalletTopUp.DomainEvents));

        domainEventsProperty.ShouldBeNull();
        domainEventsNavigation.ShouldBeNull();
    }
}
