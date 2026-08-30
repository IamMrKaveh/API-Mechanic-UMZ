using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Wallet.QueryServices;
using Users = Domain.User.Aggregates.User;

namespace Tests.Infrastructure.Wallet.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletFraudAlertQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private WalletFraudAlertQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new WalletFraudAlertQueryService(_context);
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

    private async Task<WalletFraudAlert> SeedAlertAsync(
        UserId userId,
        WalletId? walletId = null,
        FraudAlertSeverity severity = FraudAlertSeverity.Medium,
        string ruleName = "HighAmountRule",
        FraudAlertStatus? finalStatus = null)
    {
        var alert = new WalletFraudAlertBuilder()
            .WithUserId(userId)
            .WithWalletId(walletId ?? WalletId.NewId())
            .WithRuleName(ruleName)
            .WithSeverity(severity)
            .Build();

        if (finalStatus == FraudAlertStatus.Reviewed)
            alert.MarkAsReviewed(UserId.NewId(), "handled");
        else if (finalStatus == FraudAlertStatus.Dismissed)
            alert.Dismiss(UserId.NewId(), "false positive");

        alert.ClearDomainEvents();
        _context.Set<WalletFraudAlert>().Add(alert);
        await _context.SaveChangesAsync();
        return alert;
    }

    [Fact]
    public async Task GetAlertsPageAsync_WhenNoAlerts_ReturnsEmpty()
    {
        var result = await _sut.GetAlertsPageAsync(null, null, null, 1, 20, null, null, CancellationToken.None);

        result.TotalCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAlertsPageAsync_WithSingleAlert_ReturnsMappedDtoWithUserFullName()
    {
        var user = await SeedUserAsync("Neda", "Sharifi", "09121112001");
        var alert = await SeedAlertAsync(user.Id, severity: FraudAlertSeverity.Critical, ruleName: "VelocityRule");
        _context.ChangeTracker.Clear();

        var result = await _sut.GetAlertsPageAsync(null, null, null, 1, 20, null, null, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        var dto = result.Items.Single();
        dto.Id.ShouldBe(alert.Id.Value);
        dto.UserId.ShouldBe(user.Id.Value);
        dto.UserFullName.ShouldBe("Neda Sharifi");
        dto.RuleName.ShouldBe("VelocityRule");
        dto.Severity.ShouldBe(nameof(FraudAlertSeverity.Critical));
        dto.Status.ShouldBe(nameof(FraudAlertStatus.Open));
    }

    [Fact]
    public async Task GetAlertsPageAsync_OrderedByTriggeredAtDescending()
    {
        var user = await SeedUserAsync(phone: "09121112101");
        var first = await SeedAlertAsync(user.Id);
        await Task.Delay(20);
        var second = await SeedAlertAsync(user.Id);
        await Task.Delay(20);
        var third = await SeedAlertAsync(user.Id);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetAlertsPageAsync(null, null, null, 1, 20, null, null, CancellationToken.None);

        result.Items[0].Id.ShouldBe(third.Id.Value);
        result.Items[1].Id.ShouldBe(second.Id.Value);
        result.Items[2].Id.ShouldBe(first.Id.Value);
    }

    [Fact]
    public async Task GetAlertsPageAsync_WithStatusFilterOpen_ReturnsOnlyOpen()
    {
        var user = await SeedUserAsync(phone: "09121112201");
        var open = await SeedAlertAsync(user.Id);
        await SeedAlertAsync(user.Id, finalStatus: FraudAlertStatus.Reviewed);
        await SeedAlertAsync(user.Id, finalStatus: FraudAlertStatus.Dismissed);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetAlertsPageAsync(FraudAlertStatus.Open, null, null, 1, 20, null, null, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(open.Id.Value);
    }

    [Fact]
    public async Task GetAlertsPageAsync_WithSeverityFilter_ReturnsOnlyMatchingSeverity()
    {
        var user = await SeedUserAsync(phone: "09121112301");
        await SeedAlertAsync(user.Id, severity: FraudAlertSeverity.Low);
        var high = await SeedAlertAsync(user.Id, severity: FraudAlertSeverity.High);
        await SeedAlertAsync(user.Id, severity: FraudAlertSeverity.Medium);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetAlertsPageAsync(null, FraudAlertSeverity.High, null, 1, 20, null, null, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(high.Id.Value);
    }

    [Fact]
    public async Task GetAlertsPageAsync_WithUserIdFilter_ReturnsOnlyAlertsForThatUser()
    {
        var userA = await SeedUserAsync(phone: "09121112401");
        var userB = await SeedUserAsync(phone: "09121112402");
        var target = await SeedAlertAsync(userA.Id);
        await SeedAlertAsync(userB.Id);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetAlertsPageAsync(null, null, userA.Id.Value, 1, 20, null, null, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(target.Id.Value);
    }

    [Fact]
    public async Task GetAlertsPageAsync_WithDateRangeFilter_ReturnsOnlyAlertsWithinRange()
    {
        var user = await SeedUserAsync(phone: "09121112501");
        var recent = await SeedAlertAsync(user.Id);
        var older = await SeedAlertAsync(user.Id);

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletFraudAlerts\" SET \"TriggeredAt\" = {0} WHERE \"Id\" = {1}",
            DateTime.UtcNow.AddDays(-5), older.Id.Value);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetAlertsPageAsync(
            null, null, null, 1, 20,
            DateTime.UtcNow.AddDays(-1), null, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(recent.Id.Value);
    }

    [Fact]
    public async Task GetAlertsPageAsync_WithPaging_ReturnsCorrectSlice()
    {
        var user = await SeedUserAsync(phone: "09121112601");
        for (var i = 0; i < 5; i++)
        {
            await SeedAlertAsync(user.Id, ruleName: $"Rule-{i}");
            await Task.Delay(5);
        }
        _context.ChangeTracker.Clear();

        var page1 = await _sut.GetAlertsPageAsync(null, null, null, 1, 2, null, null, CancellationToken.None);
        var page3 = await _sut.GetAlertsPageAsync(null, null, null, 3, 2, null, null, CancellationToken.None);

        page1.TotalCount.ShouldBe(5);
        page1.Items.Count.ShouldBe(2);
        page3.Items.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(0, 0, 1, 20)]
    [InlineData(-2, -1, 1, 20)]
    [InlineData(1, 500, 1, 200)]
    public async Task GetAlertsPageAsync_WithInvalidPagingBounds_ClampsValues(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var result = await _sut.GetAlertsPageAsync(null, null, null, page, pageSize, null, null, CancellationToken.None);

        result.Page.ShouldBe(expectedPage);
        result.PageSize.ShouldBe(expectedPageSize);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAlertExists_ReturnsMappedDto()
    {
        var user = await SeedUserAsync("Hassan", "Karimi", "09121112701");
        var alert = await SeedAlertAsync(user.Id, ruleName: "GeoAnomalyRule");
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByIdAsync(alert.Id.Value, CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(alert.Id.Value);
        result.UserFullName.ShouldBe("Hassan Karimi");
        result.RuleName.ShouldBe("GeoAnomalyRule");
    }

    [Fact]
    public async Task GetByIdAsync_WhenAlertDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetOpenAlertsCountAsync_ReturnsCountOfOnlyOpenAlerts()
    {
        var user = await SeedUserAsync(phone: "09121112801");
        await SeedAlertAsync(user.Id);
        await SeedAlertAsync(user.Id);
        await SeedAlertAsync(user.Id, finalStatus: FraudAlertStatus.Reviewed);
        await SeedAlertAsync(user.Id, finalStatus: FraudAlertStatus.Dismissed);
        _context.ChangeTracker.Clear();

        var count = await _sut.GetOpenAlertsCountAsync(CancellationToken.None);

        count.ShouldBe(2);
    }

    [Fact]
    public async Task GetOpenAlertsCountAsync_WhenNoAlerts_ReturnsZero()
    {
        var count = await _sut.GetOpenAlertsCountAsync(CancellationToken.None);

        count.ShouldBe(0);
    }
}
