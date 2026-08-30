using Application.Wallet.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Wallet.QueryServices;
using Users = Domain.User.Aggregates.User;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Infrastructure.Wallet.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private WalletQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new WalletQueryService(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable) return;
        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<(Users user, Wallets wallet)> SeedUserAndWalletAsync(
        string firstName = "Ali",
        string lastName = "Rezaei",
        string? phone = null,
        decimal initialCredit = 0m)
    {
        var user = new UserBuilder()
            .WithFullName(FullName.Create(firstName, lastName))
            .WithEmail(Email.Create($"u-{Guid.NewGuid():N}@example.com"))
            .WithPhoneNumber(PhoneNumber.Create(phone ?? "09121234567"))
            .Build();
        user.ClearDomainEvents();
        _context.Users.Add(user);

        var wallet = new WalletBuilder().WithOwnerId(user.Id).Build();
        if (initialCredit > 0)
            wallet.Credit(Money.Create(initialCredit, "IRT"), "seed", Guid.NewGuid().ToString());
        wallet.ClearDomainEvents();
        _context.Wallets.Add(wallet);

        await _context.SaveChangesAsync();
        return (user, wallet);
    }

    private async Task AddLedgerEntryAsync(
        Wallets wallet,
        UserId ownerId,
        decimal amount,
        decimal balanceAfter,
        bool isCredit = true,
        string? description = null)
    {
        var builder = new WalletLedgerEntryBuilder()
            .WithWalletId(wallet.Id)
            .WithOwnerId(ownerId)
            .WithAmount(amount)
            .WithBalanceAfter(balanceAfter)
            .WithDescription(description);

        var entry = (isCredit ? builder.AsCredit() : builder.AsDebit()).Build();
        _context.WalletLedgerEntries.Add(entry);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetLedgerPageAsync_WhenNoEntries_ReturnsEmpty()
    {
        var (user, _) = await SeedUserAndWalletAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetLedgerPageAsync(user.Id, 1, 20, null, false, CancellationToken.None);

        result.TotalCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLedgerPageAsync_WithMultipleEntries_ReturnsMappedDtosOrderedByOccurredAtDescending()
    {
        var (user, wallet) = await SeedUserAndWalletAsync();
        await AddLedgerEntryAsync(wallet, user.Id, 100_000m, 100_000m, isCredit: true, description: "first");
        await Task.Delay(20);
        await AddLedgerEntryAsync(wallet, user.Id, 30_000m, 70_000m, isCredit: false, description: "second");
        await Task.Delay(20);
        await AddLedgerEntryAsync(wallet, user.Id, 50_000m, 120_000m, isCredit: true, description: "third");
        _context.ChangeTracker.Clear();

        var result = await _sut.GetLedgerPageAsync(user.Id, 1, 20, null, false, CancellationToken.None);

        result.TotalCount.ShouldBe(3);
        result.Items[0].Description.ShouldBe("third");
        result.Items[1].Description.ShouldBe("second");
        result.Items[2].Description.ShouldBe("first");
    }

    [Fact]
    public async Task GetLedgerPageAsync_WithTransactionTypeFilterCredit_ReturnsOnlyCreditEntries()
    {
        var (user, wallet) = await SeedUserAndWalletAsync();
        await AddLedgerEntryAsync(wallet, user.Id, 100_000m, 100_000m, isCredit: true);
        await AddLedgerEntryAsync(wallet, user.Id, 30_000m, 70_000m, isCredit: false);
        _context.ChangeTracker.Clear();

        var filter = new WalletLedgerFilter { TransactionType = nameof(WalletTransactionType.Credit) };

        var result = await _sut.GetLedgerPageAsync(user.Id, 1, 20, filter, false, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].TransactionType.ShouldBe(nameof(WalletTransactionType.Credit));
    }

    [Fact]
    public async Task GetLedgerPageAsync_WithMinAmountFilter_ExcludesEntriesBelowThreshold()
    {
        var (user, wallet) = await SeedUserAndWalletAsync();
        await AddLedgerEntryAsync(wallet, user.Id, 10_000m, 10_000m);
        await AddLedgerEntryAsync(wallet, user.Id, 50_000m, 60_000m);
        await AddLedgerEntryAsync(wallet, user.Id, 100_000m, 160_000m);
        _context.ChangeTracker.Clear();

        var filter = new WalletLedgerFilter { MinAmount = 50_000m };

        var result = await _sut.GetLedgerPageAsync(user.Id, 1, 20, filter, false, CancellationToken.None);

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldNotContain(i => i.AmountDelta == 10_000m);
    }

    [Fact]
    public async Task GetLedgerPageAsync_WithSearchTermFilter_ReturnsEntriesWithMatchingDescription()
    {
        var (user, wallet) = await SeedUserAndWalletAsync();
        await AddLedgerEntryAsync(wallet, user.Id, 10_000m, 10_000m, description: "order-xyz");
        await AddLedgerEntryAsync(wallet, user.Id, 20_000m, 30_000m, description: "topup-abc");
        _context.ChangeTracker.Clear();

        var filter = new WalletLedgerFilter { SearchTerm = "xyz" };

        var result = await _sut.GetLedgerPageAsync(user.Id, 1, 20, filter, false, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Description!.ShouldContain("xyz");
    }

    [Fact]
    public async Task GetLedgerPageAsync_ReturnsOnlyEntriesForRequestedUser()
    {
        var (userA, walletA) = await SeedUserAndWalletAsync(phone: "09121110001");
        var (userB, walletB) = await SeedUserAndWalletAsync(phone: "09121110002");

        await AddLedgerEntryAsync(walletA, userA.Id, 10_000m, 10_000m);
        await AddLedgerEntryAsync(walletB, userB.Id, 20_000m, 20_000m);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetLedgerPageAsync(userA.Id, 1, 20, null, false, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].UserId.ShouldBe(userA.Id.Value);
    }

    [Fact]
    public async Task GetLedgerPageAsync_WhenOwnerDeactivatedAndIncludeInactiveFalse_ReturnsEmpty()
    {
        var (user, wallet) = await SeedUserAndWalletAsync();
        await AddLedgerEntryAsync(wallet, user.Id, 10_000m, 10_000m);

        user.Deactivate();
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetLedgerPageAsync(user.Id, 1, 20, null, includeInactiveUsers: false, CancellationToken.None);

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetLedgerPageAsync_WhenOwnerDeactivatedAndIncludeInactiveTrue_ReturnsEntries()
    {
        var (user, wallet) = await SeedUserAndWalletAsync();
        await AddLedgerEntryAsync(wallet, user.Id, 10_000m, 10_000m);

        user.Deactivate();
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetLedgerPageAsync(user.Id, 1, 20, null, includeInactiveUsers: true, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetLedgerPageAsync_FlagsAdminAdjustmentBasedOnDescriptionPrefix()
    {
        var (user, wallet) = await SeedUserAndWalletAsync();
        await AddLedgerEntryAsync(wallet, user.Id, 10_000m, 10_000m, description: "[ADMIN-CREDIT] manual credit");
        await AddLedgerEntryAsync(wallet, user.Id, 20_000m, 30_000m, description: "regular top up");
        _context.ChangeTracker.Clear();

        var result = await _sut.GetLedgerPageAsync(user.Id, 1, 20, null, false, CancellationToken.None);

        var adminEntry = result.Items.Single(i => i.Description!.StartsWith("[ADMIN-"));
        adminEntry.IsAdminAdjustment.ShouldBeTrue();
        var regular = result.Items.Single(i => i.Description == "regular top up");
        regular.IsAdminAdjustment.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, 0, 1, 10)]
    [InlineData(-1, -1, 1, 10)]
    [InlineData(1, 500, 1, 200)]
    public async Task GetLedgerPageAsync_WithInvalidPagingBounds_ClampsValues(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var (user, _) = await SeedUserAndWalletAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetLedgerPageAsync(user.Id, page, pageSize, null, false, CancellationToken.None);

        result.Page.ShouldBe(expectedPage);
        result.PageSize.ShouldBe(expectedPageSize);
    }

    [Fact]
    public async Task ExportLedgerAsync_ReturnsAllEntriesMatchingFilterWithoutPagination()
    {
        var (user, wallet) = await SeedUserAndWalletAsync();
        for (var i = 1; i <= 25; i++)
            await AddLedgerEntryAsync(wallet, user.Id, i * 1_000m, i * 1_000m);
        _context.ChangeTracker.Clear();

        var filter = new WalletLedgerFilter();

        var result = await _sut.ExportLedgerAsync(user.Id, filter, false, CancellationToken.None);

        result.Count.ShouldBe(25);
    }

    [Fact]
    public async Task GetOrderPaymentLedgerEntryAsync_WhenDescriptionContainsOrderId_ReturnsEntry()
    {
        var (user, wallet) = await SeedUserAndWalletAsync();
        var orderId = OrderId.NewId();
        await AddLedgerEntryAsync(wallet, user.Id, 50_000m, 50_000m, description: $"payment for order {orderId.Value}");
        _context.ChangeTracker.Clear();

        var result = await _sut.GetOrderPaymentLedgerEntryAsync(user.Id, orderId, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Description!.ShouldContain(orderId.Value.ToString());
    }

    [Fact]
    public async Task GetOrderPaymentLedgerEntryAsync_WhenNoMatchingEntry_ReturnsNull()
    {
        var (user, _) = await SeedUserAndWalletAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetOrderPaymentLedgerEntryAsync(user.Id, OrderId.NewId(), CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetOverviewPageAsync_ReturnsUserWalletsWithBalancesReservationsAndFullName()
    {
        var (user, wallet) = await SeedUserAndWalletAsync("Reza", "Kazemi", "09121213000", initialCredit: 500_000m);
        wallet.CreateReservation(WalletReservationId.NewId(), Money.Create(100_000m, "IRT"), "test");
        wallet.ClearDomainEvents();
        _context.Wallets.Update(wallet);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetOverviewPageAsync(1, 20, null, false, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        var dto = result.Items.Single();
        dto.WalletId.ShouldBe(wallet.Id.Value);
        dto.UserId.ShouldBe(user.Id.Value);
        dto.UserFullName.ShouldBe("Reza Kazemi");
        dto.CurrentBalance.ShouldBe(500_000m);
        dto.ReservedBalance.ShouldBe(100_000m);
        dto.AvailableBalance.ShouldBe(400_000m);
        dto.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task GetOverviewPageAsync_WithSearchFilterOnFirstName_ReturnsMatchingWallets()
    {
        var (targetUser, _) = await SeedUserAndWalletAsync("Mohammad", "Bagheri", "09121214001");
        await SeedUserAndWalletAsync("Sara", "Kazemi", "09121214002");
        _context.ChangeTracker.Clear();

        var filter = new WalletOverviewFilter { Search = "Mohammad" };

        var result = await _sut.GetOverviewPageAsync(1, 20, filter, false, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].UserId.ShouldBe(targetUser.Id.Value);
    }

    [Fact]
    public async Task GetOverviewPageAsync_WithMinBalanceFilter_ReturnsWalletsAboveThreshold()
    {
        await SeedUserAndWalletAsync(phone: "09121215001", initialCredit: 10_000m);
        await SeedUserAndWalletAsync(phone: "09121215002", initialCredit: 500_000m);
        _context.ChangeTracker.Clear();

        var filter = new WalletOverviewFilter { MinBalance = 100_000m };

        var result = await _sut.GetOverviewPageAsync(1, 20, filter, false, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].CurrentBalance.ShouldBe(500_000m);
    }

    [Fact]
    public async Task GetOverviewPageAsync_WithSortByBalanceDesc_ReturnsWalletsOrderedByBalanceDescending()
    {
        await SeedUserAndWalletAsync(phone: "09121216001", initialCredit: 100_000m);
        await SeedUserAndWalletAsync(phone: "09121216002", initialCredit: 300_000m);
        await SeedUserAndWalletAsync(phone: "09121216003", initialCredit: 200_000m);
        _context.ChangeTracker.Clear();

        var filter = new WalletOverviewFilter { SortBy = "balance_desc" };

        var result = await _sut.GetOverviewPageAsync(1, 20, filter, false, CancellationToken.None);

        result.Items[0].CurrentBalance.ShouldBe(300_000m);
        result.Items[1].CurrentBalance.ShouldBe(200_000m);
        result.Items[2].CurrentBalance.ShouldBe(100_000m);
    }

    [Fact]
    public async Task GetOverviewPageAsync_WithIsFrozenFilter_ReturnsOnlyFrozenWallets()
    {
        var (activeUser, _) = await SeedUserAndWalletAsync(phone: "09121217001");
        var (frozenUser, frozenWallet) = await SeedUserAndWalletAsync(phone: "09121217002");
        frozenWallet.Freeze("suspicious", UserId.NewId());
        _context.Wallets.Update(frozenWallet);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var filter = new WalletOverviewFilter { IsFrozen = true };

        var result = await _sut.GetOverviewPageAsync(1, 20, filter, false, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].UserId.ShouldBe(frozenUser.Id.Value);
        result.Items[0].IsActive.ShouldBeFalse();
        result.Items[0].FreezeReason.ShouldBe("suspicious");
    }

    [Fact]
    public async Task GetStatisticsAsync_WhenNoWalletsExist_ReturnsZeroedStatistics()
    {
        var result = await _sut.GetStatisticsAsync(CancellationToken.None);

        result.TotalWalletsCount.ShouldBe(0);
        result.TotalSystemBalance.ShouldBe(0m);
        result.ActiveWalletsCount.ShouldBe(0);
        result.FrozenWalletsCount.ShouldBe(0);
        result.OpenFraudAlertsCount.ShouldBe(0);
        result.PendingWithdrawalsCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetStatisticsAsync_AggregatesBalancesActiveAndFrozenCounts()
    {
        var (_, wallet1) = await SeedUserAndWalletAsync(phone: "09121218001", initialCredit: 100_000m);
        var (_, wallet2) = await SeedUserAndWalletAsync(phone: "09121218002", initialCredit: 200_000m);
        var (_, wallet3) = await SeedUserAndWalletAsync(phone: "09121218003", initialCredit: 50_000m);
        wallet3.Freeze("audit", UserId.NewId());
        _context.Wallets.Update(wallet3);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetStatisticsAsync(CancellationToken.None);

        result.TotalWalletsCount.ShouldBe(3);
        result.TotalSystemBalance.ShouldBe(350_000m);
        result.ActiveWalletsCount.ShouldBe(2);
        result.FrozenWalletsCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetStatisticsAsync_CountsOpenFraudAlerts()
    {
        var (user, wallet) = await SeedUserAndWalletAsync(phone: "09121219001");

        var openAlert = new WalletFraudAlertBuilder().WithUserId(user.Id).WithWalletId(wallet.Id).Build();
        openAlert.ClearDomainEvents();
        var reviewedAlert = new WalletFraudAlertBuilder().WithUserId(user.Id).WithWalletId(wallet.Id).Build();
        reviewedAlert.MarkAsReviewed(UserId.NewId(), "ok");
        reviewedAlert.ClearDomainEvents();

        _context.Set<WalletFraudAlert>().AddRange(openAlert, reviewedAlert);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetStatisticsAsync(CancellationToken.None);

        result.OpenFraudAlertsCount.ShouldBe(1);
    }
}
