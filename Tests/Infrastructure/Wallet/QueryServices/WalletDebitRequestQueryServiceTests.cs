using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;
using Domain.Wallet.Enums;
using Infrastructure.Persistence.Context;
using Infrastructure.Wallet.QueryServices;
using Users = Domain.User.Aggregates.User;

namespace Tests.Infrastructure.Wallet.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletDebitRequestQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private WalletDebitRequestQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new WalletDebitRequestQueryService(_context);
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

    private async Task SeedDebitRequestAsync(UserId ownerId, UserId requestedBy, decimal amount = 100_000m)
    {
        var (wallet, _) = new WalletDebitRequestBuilder()
            .WithOwner(ownerId)
            .WithRequestedBy(requestedBy)
            .WithAmount(amount)
            .Build();
        wallet.ClearDomainEvents();
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetPageAsync_WhenNoDebitRequestsExist_ReturnsEmptyPaginatedResult()
    {
        var result = await _sut.GetPageAsync(1, 20, null, CancellationToken.None);

        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(20);
    }

    [Fact]
    public async Task GetPageAsync_WithSingleRequest_ReturnsMappedDtoWithUserFullNames()
    {
        var owner = await SeedUserAsync("Reza", "Kazemi", "09121110001");
        var admin = await SeedUserAsync("Sara", "Adminson", "09121110002");
        await SeedDebitRequestAsync(owner.Id, admin.Id, 250_000m);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetPageAsync(1, 20, null, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        var dto = result.Items.Single();
        dto.OwnerId.ShouldBe(owner.Id.Value);
        dto.OwnerFullName.ShouldBe("Reza Kazemi");
        dto.RequestedBy.ShouldBe(admin.Id.Value);
        dto.RequestedByFullName.ShouldBe("Sara Adminson");
        dto.Amount.ShouldBe(250_000m);
        dto.Status.ShouldBe(nameof(WalletDebitRequestStatus.Pending));
    }

    [Fact]
    public async Task GetPageAsync_OrdersItemsByCreatedAtDescending()
    {
        var owner = await SeedUserAsync(phone: "09122220001");
        var admin = await SeedUserAsync(phone: "09122220002");

        await SeedDebitRequestAsync(owner.Id, admin.Id, 100_000m);
        await Task.Delay(20);
        await SeedDebitRequestAsync(owner.Id, admin.Id, 200_000m);
        await Task.Delay(20);
        await SeedDebitRequestAsync(owner.Id, admin.Id, 300_000m);

        _context.ChangeTracker.Clear();

        var result = await _sut.GetPageAsync(1, 20, null, CancellationToken.None);

        result.TotalCount.ShouldBe(3);
        result.Items[0].Amount.ShouldBe(300_000m);
        result.Items[1].Amount.ShouldBe(200_000m);
        result.Items[2].Amount.ShouldBe(100_000m);
    }

    [Fact]
    public async Task GetPageAsync_WithOwnerFilter_ReturnsOnlyMatchingOwnerRequests()
    {
        var ownerA = await SeedUserAsync(phone: "09123330001");
        var ownerB = await SeedUserAsync(phone: "09123330002");
        var admin = await SeedUserAsync(phone: "09123330003");
        await SeedDebitRequestAsync(ownerA.Id, admin.Id);
        await SeedDebitRequestAsync(ownerB.Id, admin.Id);
        _context.ChangeTracker.Clear();

        var filter = new WalletDebitRequestFilter { OwnerId = ownerA.Id.Value };

        var result = await _sut.GetPageAsync(1, 20, filter, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].OwnerId.ShouldBe(ownerA.Id.Value);
    }

    [Fact]
    public async Task GetPageAsync_WithRequestedByFilter_ReturnsOnlyMatchingRequesterRequests()
    {
        var owner = await SeedUserAsync(phone: "09124440001");
        var adminA = await SeedUserAsync(phone: "09124440002");
        var adminB = await SeedUserAsync(phone: "09124440003");
        await SeedDebitRequestAsync(owner.Id, adminA.Id);
        await SeedDebitRequestAsync(owner.Id, adminB.Id);
        _context.ChangeTracker.Clear();

        var filter = new WalletDebitRequestFilter { RequestedBy = adminA.Id.Value };

        var result = await _sut.GetPageAsync(1, 20, filter, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].RequestedBy.ShouldBe(adminA.Id.Value);
    }

    [Fact]
    public async Task GetPageAsync_WithStatusFilterPending_ReturnsOnlyPendingRequests()
    {
        var owner = await SeedUserAsync(phone: "09125550001");
        var admin = await SeedUserAsync(phone: "09125550002");
        await SeedDebitRequestAsync(owner.Id, admin.Id);
        _context.ChangeTracker.Clear();

        var filter = new WalletDebitRequestFilter { Status = "Pending" };

        var result = await _sut.GetPageAsync(1, 20, filter, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Status.ShouldBe(nameof(WalletDebitRequestStatus.Pending));
    }

    [Fact]
    public async Task GetPageAsync_WithStatusFilterMatchingNothing_ReturnsEmpty()
    {
        var owner = await SeedUserAsync(phone: "09126660001");
        var admin = await SeedUserAsync(phone: "09126660002");
        await SeedDebitRequestAsync(owner.Id, admin.Id);
        _context.ChangeTracker.Clear();

        var filter = new WalletDebitRequestFilter { Status = "Approved" };

        var result = await _sut.GetPageAsync(1, 20, filter, CancellationToken.None);

        result.TotalCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPageAsync_WithPaging_ReturnsCorrectPage()
    {
        var owner = await SeedUserAsync(phone: "09127770001");
        var admin = await SeedUserAsync(phone: "09127770002");
        for (var i = 0; i < 5; i++)
        {
            await SeedDebitRequestAsync(owner.Id, admin.Id, 10_000m * (i + 1));
            await Task.Delay(5);
        }
        _context.ChangeTracker.Clear();

        var page1 = await _sut.GetPageAsync(1, 2, null, CancellationToken.None);
        var page2 = await _sut.GetPageAsync(2, 2, null, CancellationToken.None);
        var page3 = await _sut.GetPageAsync(3, 2, null, CancellationToken.None);

        page1.TotalCount.ShouldBe(5);
        page1.Items.Count.ShouldBe(2);
        page2.Items.Count.ShouldBe(2);
        page3.Items.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(0, 0, 1, 20)]
    [InlineData(-1, -5, 1, 20)]
    [InlineData(2, 500, 2, 200)]
    public async Task GetPageAsync_WithInvalidPagingBounds_ClampsToSafeDefaults(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var result = await _sut.GetPageAsync(page, pageSize, null, CancellationToken.None);

        result.Page.ShouldBe(expectedPage);
        result.PageSize.ShouldBe(expectedPageSize);
    }

    [Fact]
    public async Task GetPageAsync_WithDateRangeFilter_ReturnsOnlyRequestsInsideRange()
    {
        var owner = await SeedUserAsync(phone: "09128880001");
        var admin = await SeedUserAsync(phone: "09128880002");

        await SeedDebitRequestAsync(owner.Id, admin.Id, 100_000m);
        await SeedDebitRequestAsync(owner.Id, admin.Id, 200_000m);
        await SeedDebitRequestAsync(owner.Id, admin.Id, 300_000m);

        var boundary = DateTime.UtcNow.AddHours(-1);
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletDebitRequests\" SET \"CreatedAt\" = {0} WHERE \"Amount\" = {1}",
            DateTime.UtcNow.AddHours(-3), 100_000m);
        _context.ChangeTracker.Clear();

        var filter = new WalletDebitRequestFilter { FromDate = boundary };

        var result = await _sut.GetPageAsync(1, 20, filter, CancellationToken.None);

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldNotContain(i => i.Amount == 100_000m);
    }
}
