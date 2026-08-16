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
public class WalletWithdrawalRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private WalletWithdrawalRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new WalletWithdrawalRepository(_context);
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
        var reservationId = WalletReservationId.NewId();

        var withdrawal = new WalletWithdrawalRequestBuilder()
            .WithUserId(userId)
            .WithAmount(500_000m)
            .WithAccountHolder("Ali Rezaei")
            .WithReservationId(reservationId)
            .WithDescription("monthly withdrawal")
            .Build();
        withdrawal.ClearDomainEvents();

        await _sut.AddAsync(withdrawal);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(withdrawal.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(withdrawal.Id);
        loaded.UserId.ShouldBe(userId);
        loaded.Amount.Amount.ShouldBe(500_000m);
        loaded.AccountHolder.ShouldBe("Ali Rezaei");
        loaded.ReservationId.ShouldBe(reservationId);
        loaded.Description.ShouldBe("monthly withdrawal");
        loaded.Status.ShouldBe(WalletWithdrawalStatus.Pending);
        loaded.Iban.Value.ShouldNotBeNullOrWhiteSpace();
    }

    [SkippableFact]
    public async Task GetByIdAsync_WhenWithdrawalDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(WalletWithdrawalRequestId.NewId());

        loaded.ShouldBeNull();
    }

    [SkippableFact]
    public async Task GetByIdForUpdateAsync_WhenWithdrawalExists_ReturnsAndSuppressesXminModification()
    {
        var withdrawal = new WalletWithdrawalRequestBuilder().Build();
        withdrawal.ClearDomainEvents();
        await _sut.AddAsync(withdrawal);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdForUpdateAsync(withdrawal.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(withdrawal.Id);
        _context.Entry(loaded).Property("xmin").IsModified.ShouldBeFalse();
    }

    [SkippableFact]
    public async Task GetByIdForUpdateAsync_WhenWithdrawalDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdForUpdateAsync(WalletWithdrawalRequestId.NewId());

        loaded.ShouldBeNull();
    }

    [SkippableFact]
    public async Task AddAsync_TwoWithdrawalsWithSameReservationId_ViolatesUniqueIndex()
    {
        var reservationId = WalletReservationId.NewId();

        var first = new WalletWithdrawalRequestBuilder().WithReservationId(reservationId).Build();
        first.ClearDomainEvents();
        var second = new WalletWithdrawalRequestBuilder().WithReservationId(reservationId).Build();
        second.ClearDomainEvents();

        await _sut.AddAsync(first);
        await _sut.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [SkippableFact]
    public async Task Update_AfterApprovingWithdrawal_PersistsStatusApprovedAtAndProcessedBy()
    {
        var withdrawal = new WalletWithdrawalRequestBuilder().Build();
        withdrawal.ClearDomainEvents();
        await _sut.AddAsync(withdrawal);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var admin = UserId.NewId();
        var loaded = await _sut.GetByIdAsync(withdrawal.Id);
        loaded.ShouldNotBeNull();
        loaded!.Approve(admin);

        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(withdrawal.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.Status.ShouldBe(WalletWithdrawalStatus.Approved);
        reloaded.ApprovedAt.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(WalletWithdrawalStatus.Pending, 2)]
    [InlineData(WalletWithdrawalStatus.Approved, 1)]
    [InlineData(WalletWithdrawalStatus.Rejected, 0)]
    public async Task CountByUserAndStatusAsync_CountsOnlyMatchingUserAndStatus(WalletWithdrawalStatus status, int expected)
    {
        var target = UserId.NewId();
        var other = UserId.NewId();
        var admin = UserId.NewId();

        var pending1 = new WalletWithdrawalRequestBuilder().WithUserId(target).Build();
        pending1.ClearDomainEvents();
        var pending2 = new WalletWithdrawalRequestBuilder().WithUserId(target).Build();
        pending2.ClearDomainEvents();
        var approved = new WalletWithdrawalRequestBuilder().WithUserId(target).Build();
        approved.Approve(admin);
        approved.ClearDomainEvents();

        var otherUserPending = new WalletWithdrawalRequestBuilder().WithUserId(other).Build();
        otherUserPending.ClearDomainEvents();

        await _sut.AddAsync(pending1);
        await _sut.AddAsync(pending2);
        await _sut.AddAsync(approved);
        await _sut.AddAsync(otherUserPending);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var count = await _sut.CountByUserAndStatusAsync(target, status);

        count.ShouldBe(expected);
    }

    [SkippableFact]
    public async Task CountByUserAndStatusAsync_WhenNoMatchingEntries_ReturnsZero()
    {
        var count = await _sut.CountByUserAndStatusAsync(UserId.NewId(), WalletWithdrawalStatus.Pending);

        count.ShouldBe(0);
    }

    [SkippableFact]
    public async Task Update_AfterRejectingWithdrawal_PersistsStatusAndRejectionReason()
    {
        var withdrawal = new WalletWithdrawalRequestBuilder().Build();
        withdrawal.ClearDomainEvents();
        await _sut.AddAsync(withdrawal);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var admin = UserId.NewId();
        var loaded = await _sut.GetByIdAsync(withdrawal.Id);
        loaded.ShouldNotBeNull();
        loaded!.Reject(admin, "invalid IBAN owner");

        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(withdrawal.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.Status.ShouldBe(WalletWithdrawalStatus.Rejected);
        reloaded.RejectionReason.ShouldBe("invalid IBAN owner");
        reloaded.RejectedAt.ShouldNotBeNull();
    }

    [SkippableFact]
    public async Task Update_AfterMarkingPaid_PersistsBankReferenceNumberAndPaidAt()
    {
        var withdrawal = new WalletWithdrawalRequestBuilder().Build();
        withdrawal.ClearDomainEvents();
        await _sut.AddAsync(withdrawal);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var admin = UserId.NewId();
        var loaded = await _sut.GetByIdAsync(withdrawal.Id);
        loaded.ShouldNotBeNull();
        loaded!.Approve(admin);
        loaded.MarkPaid(admin, "BANK-TXN-100200");

        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(withdrawal.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.Status.ShouldBe(WalletWithdrawalStatus.Paid);
        reloaded.BankReferenceNumber.ShouldBe("BANK-TXN-100200");
        reloaded.PaidAt.ShouldNotBeNull();
    }
}
