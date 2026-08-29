using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Wallet.Repositories;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Wallet.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletTransferRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private WalletTransferRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new WalletTransferRepository(_context);
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
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsAggregateFromDatabase()
    {
        var from = UserId.NewId();
        var to = UserId.NewId();

        var transfer = new WalletTransferBuilder()
            .FromUser(from)
            .ToUser(to)
            .WithAmount(75_000m)
            .WithOtpHash("hash-abc")
            .WithDescription("gift")
            .Build();
        transfer.ClearDomainEvents();

        await _sut.AddAsync(transfer);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(transfer.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(transfer.Id);
        loaded.FromUserId.ShouldBe(from);
        loaded.ToUserId.ShouldBe(to);
        loaded.Amount.Amount.ShouldBe(75_000m);
        loaded.OtpHash.ShouldBe("hash-abc");
        loaded.Description.ShouldBe("gift");
        loaded.Status.ShouldBe(WalletTransferStatus.PendingOtp);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTransferDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(WalletTransferId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdForUpdateAsync_WhenTransferExists_ReturnsTransferAndSuppressesXminModification()
    {
        var transfer = new WalletTransferBuilder().Build();
        transfer.ClearDomainEvents();
        await _sut.AddAsync(transfer);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdForUpdateAsync(transfer.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(transfer.Id);
        _context.Entry(loaded).Property("xmin").IsModified.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdForUpdateAsync_WhenTransferDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdForUpdateAsync(WalletTransferId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task Update_AfterMarkingCompleted_PersistsStatusAndCompletedAt()
    {
        var transfer = new WalletTransferBuilder().Build();
        transfer.ClearDomainEvents();
        await _sut.AddAsync(transfer);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(transfer.Id);
        loaded.ShouldNotBeNull();
        loaded!.MarkCompleted();

        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(transfer.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.Status.ShouldBe(WalletTransferStatus.Completed);
        reloaded.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task AddAsync_TwoTransfersWithSameCorrelationId_ViolatesUniqueIndex()
    {
        var first = new WalletTransferBuilder().Build();
        first.ClearDomainEvents();
        await _sut.AddAsync(first);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var second = new WalletTransferBuilder().Build();
        second.ClearDomainEvents();
        await _sut.AddAsync(second);
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletTransfers\" SET \"CorrelationId\" = {0} WHERE \"Id\" = {1}",
            first.CorrelationId, second.Id.Value);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task SumCompletedAmountForDayAsync_SumsOnlyCompletedTransfersOfDayForGivenFromUser()
    {
        var target = UserId.NewId();
        var day = DateTime.UtcNow.Date;

        var completed1 = new WalletTransferBuilder().FromUser(target).WithAmount(20_000m).Build();
        completed1.MarkCompleted();
        completed1.ClearDomainEvents();

        var completed2 = new WalletTransferBuilder().FromUser(target).WithAmount(35_000m).Build();
        completed2.MarkCompleted();
        completed2.ClearDomainEvents();

        var pending = new WalletTransferBuilder().FromUser(target).WithAmount(99_000m).Build();
        pending.ClearDomainEvents();

        var otherUser = new WalletTransferBuilder().WithAmount(50_000m).Build();
        otherUser.MarkCompleted();
        otherUser.ClearDomainEvents();

        await _sut.AddAsync(completed1);
        await _sut.AddAsync(completed2);
        await _sut.AddAsync(pending);
        await _sut.AddAsync(otherUser);
        await _context.SaveChangesAsync();

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletTransfers\" SET \"CompletedAt\" = {0} WHERE \"Id\" = {1}",
            day.AddHours(10), completed1.Id.Value);
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletTransfers\" SET \"CompletedAt\" = {0} WHERE \"Id\" = {1}",
            day.AddHours(12), completed2.Id.Value);
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletTransfers\" SET \"CompletedAt\" = {0} WHERE \"Id\" = {1}",
            day.AddHours(11), otherUser.Id.Value);

        _context.ChangeTracker.Clear();

        var sum = await _sut.SumCompletedAmountForDayAsync(target, day);

        sum.ShouldBe(55_000m);
    }

    [Fact]
    public async Task SumCompletedAmountForDayAsync_WhenNoCompletedTransfers_ReturnsZero()
    {
        var target = UserId.NewId();

        var sum = await _sut.SumCompletedAmountForDayAsync(target, DateTime.UtcNow.Date);

        sum.ShouldBe(0m);
    }

    [Fact]
    public async Task CountRecentPendingByUserAsync_CountsOnlyPendingOtpTransfersWithinWindow()
    {
        var target = UserId.NewId();

        var inside1 = new WalletTransferBuilder().FromUser(target).Build();
        inside1.ClearDomainEvents();
        var inside2 = new WalletTransferBuilder().FromUser(target).Build();
        inside2.ClearDomainEvents();
        var completed = new WalletTransferBuilder().FromUser(target).Build();
        completed.MarkCompleted();
        completed.ClearDomainEvents();

        await _sut.AddAsync(inside1);
        await _sut.AddAsync(inside2);
        await _sut.AddAsync(completed);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var count = await _sut.CountRecentPendingByUserAsync(target, TimeSpan.FromMinutes(30));

        count.ShouldBe(2);
    }

    [Fact]
    public async Task CountRecentPendingByUserAsync_ExcludesEntriesOutsideWindow()
    {
        var target = UserId.NewId();

        var oldPending = new WalletTransferBuilder().FromUser(target).Build();
        oldPending.ClearDomainEvents();
        await _sut.AddAsync(oldPending);
        await _context.SaveChangesAsync();

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletTransfers\" SET \"CreatedAt\" = {0} WHERE \"Id\" = {1}",
            DateTime.UtcNow.AddHours(-2), oldPending.Id.Value);

        _context.ChangeTracker.Clear();

        var count = await _sut.CountRecentPendingByUserAsync(target, TimeSpan.FromMinutes(30));

        count.ShouldBe(0);
    }

    [Fact]
    public async Task CountRecentPendingByUserAsync_ExcludesOtherUsers()
    {
        var target = UserId.NewId();
        var other = UserId.NewId();

        var pendingOther = new WalletTransferBuilder().FromUser(other).Build();
        pendingOther.ClearDomainEvents();
        await _sut.AddAsync(pendingOther);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var count = await _sut.CountRecentPendingByUserAsync(target, TimeSpan.FromHours(1));

        count.ShouldBe(0);
    }
}
