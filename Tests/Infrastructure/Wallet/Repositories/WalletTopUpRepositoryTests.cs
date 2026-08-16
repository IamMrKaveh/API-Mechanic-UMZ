using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Wallet.Repositories;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Wallet.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletTopUpRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private WalletTopUpRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new WalletTopUpRepository(_context);
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
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsAggregateFromDatabase()
    {
        var userId = UserId.NewId();
        var topUp = new WalletTopUpBuilder()
            .WithUserId(userId)
            .WithAmount(120_000m, "IRT")
            .WithGateway("zarinpal")
            .Build();
        topUp.ClearDomainEvents();

        await _sut.AddAsync(topUp);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(topUp.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(topUp.Id);
        loaded.UserId.ShouldBe(userId);
        loaded.Amount.Amount.ShouldBe(120_000m);
        loaded.Amount.Currency.ShouldBe("IRT");
        loaded.Gateway.ShouldBe("zarinpal");
        loaded.Status.ShouldBe(WalletTopUpStatus.Pending);
    }

    [SkippableFact]
    public async Task GetByIdAsync_WhenTopUpDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(WalletTopUpId.NewId());

        loaded.ShouldBeNull();
    }

    [SkippableFact]
    public async Task GetByAuthorityAsync_WhenAuthorityIsSet_ReturnsMatchingTopUp()
    {
        var topUp = new WalletTopUpBuilder().Build();
        topUp.MarkAuthorityIssued("AUTH-XYZ-1");
        topUp.ClearDomainEvents();

        await _sut.AddAsync(topUp);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByAuthorityAsync("AUTH-XYZ-1");

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(topUp.Id);
        loaded.GatewayAuthority.ShouldBe("AUTH-XYZ-1");
    }

    [SkippableFact]
    public async Task GetByAuthorityAsync_WhenAuthorityDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByAuthorityAsync("MISSING-AUTH");

        loaded.ShouldBeNull();
    }

    [SkippableFact]
    public async Task AddAsync_TwoTopUpsWithSameAuthority_ViolatesUniqueFilteredIndex()
    {
        const string authority = "AUTH-DUP-1";

        var first = new WalletTopUpBuilder().Build();
        first.MarkAuthorityIssued(authority);
        first.ClearDomainEvents();

        var second = new WalletTopUpBuilder().Build();
        second.MarkAuthorityIssued(authority);
        second.ClearDomainEvents();

        await _sut.AddAsync(first);
        await _sut.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task GetPendingOlderThanAsync_ReturnsOnlyPendingEntriesCreatedBeforeCutoff()
    {
        var older = new WalletTopUpBuilder().Build();
        older.ClearDomainEvents();
        var newer = new WalletTopUpBuilder().Build();
        newer.ClearDomainEvents();

        await _sut.AddAsync(older);
        await _sut.AddAsync(newer);
        await _context.SaveChangesAsync();

        var olderCreatedAt = older.CreatedAt;
        var cutoff = olderCreatedAt.AddMilliseconds(1);

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletTopUps\" SET \"CreatedAt\" = {0} WHERE \"Id\" = {1}",
            olderCreatedAt.AddHours(-2), older.Id.Value);

        _context.ChangeTracker.Clear();

        var results = await _sut.GetPendingOlderThanAsync(cutoff, batchSize: 50);

        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(older.Id);
    }

    [SkippableFact]
    public async Task GetPendingOlderThanAsync_ExcludesNonPendingStates()
    {
        var pending = new WalletTopUpBuilder().Build();
        pending.ClearDomainEvents();

        var succeeded = new WalletTopUpBuilder().Build();
        succeeded.MarkSucceeded("BANK-REF-1");
        succeeded.ClearDomainEvents();

        var failed = new WalletTopUpBuilder().Build();
        failed.MarkFailed("gateway timeout");
        failed.ClearDomainEvents();

        await _sut.AddAsync(pending);
        await _sut.AddAsync(succeeded);
        await _sut.AddAsync(failed);
        await _context.SaveChangesAsync();

        var cutoff = DateTime.UtcNow.AddMinutes(5);
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletTopUps\" SET \"CreatedAt\" = {0}",
            DateTime.UtcNow.AddHours(-1));

        _context.ChangeTracker.Clear();

        var results = await _sut.GetPendingOlderThanAsync(cutoff, batchSize: 50);

        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(pending.Id);
    }

    [SkippableFact]
    public async Task GetPendingOlderThanAsync_RespectsBatchSize()
    {
        for (var i = 0; i < 5; i++)
        {
            var t = new WalletTopUpBuilder().Build();
            t.ClearDomainEvents();
            await _sut.AddAsync(t);
        }

        await _context.SaveChangesAsync();

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletTopUps\" SET \"CreatedAt\" = {0}",
            DateTime.UtcNow.AddHours(-1));

        _context.ChangeTracker.Clear();

        var results = await _sut.GetPendingOlderThanAsync(DateTime.UtcNow, batchSize: 2);

        results.Count.ShouldBe(2);
    }

    [SkippableFact]
    public async Task GetByUserIdAsync_ReturnsOnlyEntriesForThatUserOrderedByCreatedAtDescending()
    {
        var target = UserId.NewId();
        var other = UserId.NewId();

        var t1 = new WalletTopUpBuilder().WithUserId(target).Build();
        t1.ClearDomainEvents();
        var t2 = new WalletTopUpBuilder().WithUserId(target).Build();
        t2.ClearDomainEvents();
        var tOther = new WalletTopUpBuilder().WithUserId(other).Build();
        tOther.ClearDomainEvents();

        await _sut.AddAsync(t1);
        await _sut.AddAsync(t2);
        await _sut.AddAsync(tOther);
        await _context.SaveChangesAsync();

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletTopUps\" SET \"CreatedAt\" = {0} WHERE \"Id\" = {1}",
            DateTime.UtcNow.AddMinutes(-10), t1.Id.Value);
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletTopUps\" SET \"CreatedAt\" = {0} WHERE \"Id\" = {1}",
            DateTime.UtcNow.AddMinutes(-1), t2.Id.Value);

        _context.ChangeTracker.Clear();

        var results = await _sut.GetByUserIdAsync(target, page: 1, pageSize: 20);

        results.Count.ShouldBe(2);
        results[0].Id.ShouldBe(t2.Id);
        results[1].Id.ShouldBe(t1.Id);
    }

    [Theory]
    [InlineData(0, 5, 5)]
    [InlineData(-1, 5, 5)]
    [InlineData(1, 0, 20)]
    [InlineData(1, 101, 20)]
    public async Task GetByUserIdAsync_NormalizesInvalidPagingParameters(int page, int pageSize, int expectedMaxItems)
    {
        var target = UserId.NewId();

        for (var i = 0; i < expectedMaxItems + 3; i++)
        {
            var t = new WalletTopUpBuilder().WithUserId(target).Build();
            t.ClearDomainEvents();
            await _sut.AddAsync(t);
        }
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var results = await _sut.GetByUserIdAsync(target, page, pageSize);

        results.Count.ShouldBeLessThanOrEqualTo(expectedMaxItems);
    }

    [SkippableFact]
    public async Task Update_AfterMarkingSucceeded_PersistsStatusAndGatewayRefId()
    {
        var topUp = new WalletTopUpBuilder().Build();
        topUp.ClearDomainEvents();
        await _sut.AddAsync(topUp);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(topUp.Id);
        loaded.ShouldNotBeNull();
        loaded!.MarkSucceeded("BANK-REF-99");

        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(topUp.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.Status.ShouldBe(WalletTopUpStatus.Succeeded);
        reloaded.GatewayRefId.ShouldBe("BANK-REF-99");
        reloaded.CompletedAt.ShouldNotBeNull();
    }

    [SkippableFact]
    public async Task Update_ForDetachedTopUp_AttachesAndPersistsChanges()
    {
        var topUp = new WalletTopUpBuilder().Build();
        topUp.ClearDomainEvents();
        await _sut.AddAsync(topUp);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        topUp.MarkFailed("connectivity error");
        topUp.ClearDomainEvents();

        _sut.Update(topUp);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(topUp.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.Status.ShouldBe(WalletTopUpStatus.Failed);
        reloaded.FailureReason.ShouldBe("connectivity error");
    }
}
