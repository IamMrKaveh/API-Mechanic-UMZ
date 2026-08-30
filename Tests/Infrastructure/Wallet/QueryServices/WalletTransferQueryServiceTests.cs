using Application.Wallet.Features.Shared;
using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Infrastructure.Persistence.Context;
using Infrastructure.Wallet.QueryServices;
using Users = Domain.User.Aggregates.User;

namespace Tests.Infrastructure.Wallet.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletTransferQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private WalletTransferQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new WalletTransferQueryService(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable) return;
        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<Users> SeedUserAsync(string firstName, string lastName, string phone)
    {
        var user = new UserBuilder()
            .WithFullName(FullName.Create(firstName, lastName))
            .WithEmail(Email.Create($"u-{Guid.NewGuid():N}@example.com"))
            .WithPhoneNumber(PhoneNumber.Create(phone))
            .Build();
        user.ClearDomainEvents();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<WalletTransfer> SeedTransferAsync(
        UserId fromUserId,
        UserId toUserId,
        decimal amount = 50_000m,
        WalletTransferStatus? finalStatus = null)
    {
        var transfer = new WalletTransferBuilder()
            .FromUser(fromUserId)
            .ToUser(toUserId)
            .WithAmount(amount)
            .WithDescription("test transfer")
            .Build();

        if (finalStatus == WalletTransferStatus.Completed)
            transfer.MarkCompleted();
        else if (finalStatus == WalletTransferStatus.Cancelled)
            transfer.Cancel(fromUserId);
        else if (finalStatus == WalletTransferStatus.Failed)
            transfer.MarkFailed("simulated failure");

        transfer.ClearDomainEvents();
        _context.Set<WalletTransfer>().Add(transfer);
        await _context.SaveChangesAsync();
        return transfer;
    }

    [Fact]
    public async Task GetTransfersPageAsync_WhenNoTransfers_ReturnsEmptyPage()
    {
        var result = await _sut.GetTransfersPageAsync(1, 20, null, CancellationToken.None);

        result.TotalCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetTransfersPageAsync_WithSingleTransfer_MapsFromAndToFullNames()
    {
        var sender = await SeedUserAsync("Reza", "Kazemi", "09121230001");
        var receiver = await SeedUserAsync("Sara", "Farahani", "09121230002");
        var transfer = await SeedTransferAsync(sender.Id, receiver.Id, 75_000m);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetTransfersPageAsync(1, 20, null, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        var dto = result.Items.Single();
        dto.Id.ShouldBe(transfer.Id.Value);
        dto.FromUserId.ShouldBe(sender.Id.Value);
        dto.FromUserFullName.ShouldBe("Reza Kazemi");
        dto.ToUserId.ShouldBe(receiver.Id.Value);
        dto.ToUserFullName.ShouldBe("Sara Farahani");
        dto.Amount.ShouldBe(75_000m);
        dto.Status.ShouldBe(nameof(WalletTransferStatus.PendingOtp));
    }

    [Fact]
    public async Task GetTransfersPageAsync_OrdersByCreatedAtDescending()
    {
        var sender = await SeedUserAsync("A", "One", "09121231001");
        var receiver = await SeedUserAsync("B", "Two", "09121231002");

        var older = await SeedTransferAsync(sender.Id, receiver.Id, 10_000m);
        await Task.Delay(20);
        var middle = await SeedTransferAsync(sender.Id, receiver.Id, 20_000m);
        await Task.Delay(20);
        var newest = await SeedTransferAsync(sender.Id, receiver.Id, 30_000m);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetTransfersPageAsync(1, 20, null, CancellationToken.None);

        result.Items[0].Id.ShouldBe(newest.Id.Value);
        result.Items[1].Id.ShouldBe(middle.Id.Value);
        result.Items[2].Id.ShouldBe(older.Id.Value);
    }

    [Fact]
    public async Task GetTransfersPageAsync_WithUserIdFilter_ReturnsTransfersWhereUserIsSenderOrReceiver()
    {
        var alice = await SeedUserAsync("Alice", "Test", "09121232001");
        var bob = await SeedUserAsync("Bob", "Test", "09121232002");
        var charlie = await SeedUserAsync("Charlie", "Test", "09121232003");

        var aliceToBob = await SeedTransferAsync(alice.Id, bob.Id);
        var charlieToAlice = await SeedTransferAsync(charlie.Id, alice.Id);
        await SeedTransferAsync(bob.Id, charlie.Id);
        _context.ChangeTracker.Clear();

        var filter = new WalletTransferFilter { UserId = alice.Id.Value };

        var result = await _sut.GetTransfersPageAsync(1, 20, filter, CancellationToken.None);

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldContain(t => t.Id == aliceToBob.Id.Value);
        result.Items.ShouldContain(t => t.Id == charlieToAlice.Id.Value);
    }

    [Fact]
    public async Task GetTransfersPageAsync_WithStatusFilterCompleted_ReturnsOnlyCompletedTransfers()
    {
        var sender = await SeedUserAsync("S", "F", "09121233001");
        var receiver = await SeedUserAsync("R", "F", "09121233002");
        var completed = await SeedTransferAsync(sender.Id, receiver.Id, finalStatus: WalletTransferStatus.Completed);
        await SeedTransferAsync(sender.Id, receiver.Id);
        _context.ChangeTracker.Clear();

        var filter = new WalletTransferFilter { Status = "Completed" };

        var result = await _sut.GetTransfersPageAsync(1, 20, filter, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(completed.Id.Value);
    }

    [Fact]
    public async Task GetTransfersPageAsync_WithDateRangeFilter_ReturnsOnlyTransfersInsideRange()
    {
        var sender = await SeedUserAsync("S", "D", "09121234001");
        var receiver = await SeedUserAsync("R", "D", "09121234002");

        var recent = await SeedTransferAsync(sender.Id, receiver.Id, 10_000m);
        var older = await SeedTransferAsync(sender.Id, receiver.Id, 20_000m);

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletTransfers\" SET \"CreatedAt\" = {0} WHERE \"Id\" = {1}",
            DateTime.UtcNow.AddDays(-5), older.Id.Value);
        _context.ChangeTracker.Clear();

        var filter = new WalletTransferFilter { FromDate = DateTime.UtcNow.AddDays(-1) };

        var result = await _sut.GetTransfersPageAsync(1, 20, filter, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(recent.Id.Value);
    }

    [Fact]
    public async Task GetTransfersPageAsync_WithPaging_ReturnsCorrectSlice()
    {
        var sender = await SeedUserAsync("S", "P", "09121235001");
        var receiver = await SeedUserAsync("R", "P", "09121235002");

        for (var i = 0; i < 5; i++)
        {
            await SeedTransferAsync(sender.Id, receiver.Id, 10_000m * (i + 1));
            await Task.Delay(5);
        }
        _context.ChangeTracker.Clear();

        var page1 = await _sut.GetTransfersPageAsync(1, 2, null, CancellationToken.None);
        var page3 = await _sut.GetTransfersPageAsync(3, 2, null, CancellationToken.None);

        page1.TotalCount.ShouldBe(5);
        page1.Items.Count.ShouldBe(2);
        page3.Items.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(0, 0, 1, 20)]
    [InlineData(-3, -1, 1, 20)]
    [InlineData(1, 500, 1, 200)]
    public async Task GetTransfersPageAsync_WithInvalidPagingBounds_ClampsValues(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var result = await _sut.GetTransfersPageAsync(page, pageSize, null, CancellationToken.None);

        result.Page.ShouldBe(expectedPage);
        result.PageSize.ShouldBe(expectedPageSize);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTransferExists_ReturnsMappedDtoWithFullNames()
    {
        var sender = await SeedUserAsync("Hassan", "Nazari", "09121236001");
        var receiver = await SeedUserAsync("Zahra", "Amini", "09121236002");
        var transfer = await SeedTransferAsync(sender.Id, receiver.Id, 90_000m);
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByIdAsync(transfer.Id.Value, CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(transfer.Id.Value);
        result.FromUserFullName.ShouldBe("Hassan Nazari");
        result.ToUserFullName.ShouldBe("Zahra Amini");
        result.Amount.ShouldBe(90_000m);
    }

    [Fact]
    public async Task GetByIdAsync_WhenTransferDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenUsersAreMissing_ReturnsDtoWithNullFullNames()
    {
        var transfer = new WalletTransferBuilder()
            .FromUser(UserId.NewId())
            .ToUser(UserId.NewId())
            .WithAmount(15_000m)
            .Build();
        transfer.ClearDomainEvents();
        _context.Set<WalletTransfer>().Add(transfer);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByIdAsync(transfer.Id.Value, CancellationToken.None);

        result.ShouldNotBeNull();
        result!.FromUserFullName.ShouldBeNull();
        result.ToUserFullName.ShouldBeNull();
    }
}
