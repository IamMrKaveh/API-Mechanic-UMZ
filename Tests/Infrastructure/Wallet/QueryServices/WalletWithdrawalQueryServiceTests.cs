using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Infrastructure.Persistence.Context;
using Infrastructure.Wallet.QueryServices;
using Users = Domain.User.Aggregates.User;

namespace Tests.Infrastructure.Wallet.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletWithdrawalQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private WalletWithdrawalQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new WalletWithdrawalQueryService(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable) return;
        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<Users> SeedUserAsync(string firstName = "Ali", string lastName = "Rezaei", string? phone = null)
    {
        var user = new UserBuilder()
            .WithFullName(FullName.Create(firstName, lastName))
            .WithEmail(Email.Create($"u-{Guid.NewGuid():N}@example.com"))
            .WithPhoneNumber(PhoneNumber.Create(phone ?? "09121234567"))
            .Build();
        user.ClearDomainEvents();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<WalletWithdrawalRequest> SeedWithdrawalAsync(
        UserId userId,
        decimal amount = 100_000m,
        WalletWithdrawalStatus? finalStatus = null,
        UserId? adminId = null)
    {
        var withdrawal = new WalletWithdrawalRequestBuilder()
            .WithUserId(userId)
            .WithAmount(amount)
            .Build();

        var admin = adminId ?? UserId.NewId();
        if (finalStatus == WalletWithdrawalStatus.Approved)
            withdrawal.Approve(admin);
        else if (finalStatus == WalletWithdrawalStatus.Rejected)
            withdrawal.Reject(admin, "invalid iban");
        else if (finalStatus == WalletWithdrawalStatus.Paid)
        {
            withdrawal.Approve(admin);
            withdrawal.MarkPaid(admin, "BANK-REF-123");
        }
        else if (finalStatus == WalletWithdrawalStatus.Cancelled)
            withdrawal.Cancel(userId);

        withdrawal.ClearDomainEvents();
        _context.Set<WalletWithdrawalRequest>().Add(withdrawal);
        await _context.SaveChangesAsync();
        return withdrawal;
    }

    [Fact]
    public async Task GetByUserAsync_WhenNoRequests_ReturnsEmpty()
    {
        var user = await SeedUserAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByUserAsync(user.Id, 1, 10, CancellationToken.None);

        result.TotalCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByUserAsync_WithRequests_MapsAmountIbanStatusAndUserFullName()
    {
        var user = await SeedUserAsync("Reza", "Karimi", "09121240001");
        var withdrawal = await SeedWithdrawalAsync(user.Id, 250_000m);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByUserAsync(user.Id, 1, 10, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        var dto = result.Items.Single();
        dto.Id.ShouldBe(withdrawal.Id.Value);
        dto.UserId.ShouldBe(user.Id.Value);
        dto.UserFullName.ShouldBe("Reza Karimi");
        dto.Amount.ShouldBe(250_000m);
        dto.Status.ShouldBe(nameof(WalletWithdrawalStatus.Pending));
    }

    [Fact]
    public async Task GetByUserAsync_OrderedByCreatedAtDescending()
    {
        var user = await SeedUserAsync(phone: "09121241001");
        var older = await SeedWithdrawalAsync(user.Id, 100_000m);
        await Task.Delay(20);
        var newer = await SeedWithdrawalAsync(user.Id, 200_000m);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByUserAsync(user.Id, 1, 10, CancellationToken.None);

        result.Items[0].Id.ShouldBe(newer.Id.Value);
        result.Items[1].Id.ShouldBe(older.Id.Value);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyRequestsForRequestedUser()
    {
        var userA = await SeedUserAsync(phone: "09121242001");
        var userB = await SeedUserAsync(phone: "09121242002");
        var forA = await SeedWithdrawalAsync(userA.Id);
        await SeedWithdrawalAsync(userB.Id);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByUserAsync(userA.Id, 1, 10, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(forA.Id.Value);
    }

    [Theory]
    [InlineData(0, 0, 1, 10)]
    [InlineData(-3, -1, 1, 10)]
    [InlineData(1, 500, 1, 10)]
    public async Task GetByUserAsync_WithInvalidPagingBounds_ClampsValues(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var user = await SeedUserAsync(phone: "09121243001");
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByUserAsync(user.Id, page, pageSize, CancellationToken.None);

        result.Page.ShouldBe(expectedPage);
        result.PageSize.ShouldBe(expectedPageSize);
    }

    [Fact]
    public async Task GetByStatusAsync_WhenStatusIsPending_ReturnsOnlyPendingRequests()
    {
        var user = await SeedUserAsync(phone: "09121244001");
        var pending = await SeedWithdrawalAsync(user.Id, 100_000m);
        await SeedWithdrawalAsync(user.Id, 200_000m, WalletWithdrawalStatus.Approved);
        await SeedWithdrawalAsync(user.Id, 300_000m, WalletWithdrawalStatus.Rejected);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByStatusAsync(WalletWithdrawalStatus.Pending, 1, 20, null, null, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(pending.Id.Value);
        result.Items[0].Status.ShouldBe(nameof(WalletWithdrawalStatus.Pending));
    }

    [Fact]
    public async Task GetByStatusAsync_WhenStatusIsNull_ReturnsAllRequests()
    {
        var user = await SeedUserAsync(phone: "09121245001");
        await SeedWithdrawalAsync(user.Id, 100_000m);
        await SeedWithdrawalAsync(user.Id, 200_000m, WalletWithdrawalStatus.Approved);
        await SeedWithdrawalAsync(user.Id, 300_000m, WalletWithdrawalStatus.Rejected);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByStatusAsync(null, 1, 20, null, null, CancellationToken.None);

        result.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task GetByStatusAsync_OrdersByCreatedAtAscending()
    {
        var user = await SeedUserAsync(phone: "09121246001");
        var first = await SeedWithdrawalAsync(user.Id, 100_000m);
        await Task.Delay(20);
        var second = await SeedWithdrawalAsync(user.Id, 200_000m);
        await Task.Delay(20);
        var third = await SeedWithdrawalAsync(user.Id, 300_000m);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByStatusAsync(null, 1, 20, null, null, CancellationToken.None);

        result.Items[0].Id.ShouldBe(first.Id.Value);
        result.Items[1].Id.ShouldBe(second.Id.Value);
        result.Items[2].Id.ShouldBe(third.Id.Value);
    }

    [Fact]
    public async Task GetByStatusAsync_WithDateRangeFilter_ReturnsOnlyRequestsInsideRange()
    {
        var user = await SeedUserAsync(phone: "09121247001");
        var recent = await SeedWithdrawalAsync(user.Id, 100_000m);
        var older = await SeedWithdrawalAsync(user.Id, 200_000m);

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletWithdrawalRequests\" SET \"CreatedAt\" = {0} WHERE \"Id\" = {1}",
            DateTime.UtcNow.AddDays(-10), older.Id.Value);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByStatusAsync(null, 1, 20, DateTime.UtcNow.AddDays(-1), null, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(recent.Id.Value);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRequestExists_ReturnsMappedDto()
    {
        var user = await SeedUserAsync("Sara", "Amini", "09121248001");
        var withdrawal = await SeedWithdrawalAsync(user.Id, 500_000m);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByIdAsync(withdrawal.Id.Value, CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(withdrawal.Id.Value);
        result.UserFullName.ShouldBe("Sara Amini");
        result.Amount.ShouldBe(500_000m);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRequestDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenRequestIsApproved_MapsApprovedAtTimestamp()
    {
        var user = await SeedUserAsync(phone: "09121249001");
        var withdrawal = await SeedWithdrawalAsync(user.Id, 100_000m, WalletWithdrawalStatus.Approved);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByIdAsync(withdrawal.Id.Value, CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Status.ShouldBe(nameof(WalletWithdrawalStatus.Approved));
        result.ApprovedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetPendingCountAsync_WhenNoRequests_ReturnsZero()
    {
        var count = await _sut.GetPendingCountAsync(CancellationToken.None);

        count.ShouldBe(0);
    }

    [Fact]
    public async Task GetPendingCountAsync_ReturnsCountOfOnlyPendingRequests()
    {
        var user = await SeedUserAsync(phone: "09121250001");
        await SeedWithdrawalAsync(user.Id, 100_000m);
        await SeedWithdrawalAsync(user.Id, 200_000m);
        await SeedWithdrawalAsync(user.Id, 300_000m, WalletWithdrawalStatus.Approved);
        await SeedWithdrawalAsync(user.Id, 400_000m, WalletWithdrawalStatus.Rejected);
        _context.ChangeTracker.Clear();

        var count = await _sut.GetPendingCountAsync(CancellationToken.None);

        count.ShouldBe(2);
    }
}
