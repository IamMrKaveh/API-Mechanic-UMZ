using Domain.User.ValueObjects;
using Domain.Wallet.Entities;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Wallet.Repositories;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;
using Users = Domain.User.Aggregates.User;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Infrastructure.Wallet.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletDebitRequestRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private WalletDebitRequestRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new WalletDebitRequestRepository(_context);
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
    public async Task GetByIdAsync_WhenRequestExists_ReturnsRequestWithExpectedProperties()
    {
        var (_, wallet, request) = await SeedWalletAndDebitRequestAsync(amount: 100_000m, reason: "test-reason");

        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(request.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(request.Id);
        loaded.WalletId.ShouldBe(wallet.Id);
        loaded.Amount.Amount.ShouldBe(100_000m);
        loaded.Reason.ShouldBe("test-reason");
        loaded.Status.ShouldBe(WalletDebitRequestStatus.Pending);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRequestDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(WalletDebitRequestId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetByOwnerAsync_WithoutStatusFilter_ReturnsAllRequestsForOwnerOrderedByCreatedAtDescending()
    {
        var (owner, wallet, first) = await SeedWalletAndDebitRequestAsync(amount: 20_000m);
        await SeedAdditionalCreditAsync(wallet, 300_000m);
        var second = SeedAdditionalDebitRequestForWallet(wallet, owner.Id, amount: 30_000m);
        var third = SeedAdditionalDebitRequestForWallet(wallet, owner.Id, amount: 40_000m);

        await _context.SaveChangesAsync();

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletDebitRequests\" SET \"CreatedAt\" = {0} WHERE \"Id\" = {1}",
            DateTime.UtcNow.AddMinutes(-30), first.Id.Value);
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletDebitRequests\" SET \"CreatedAt\" = {0} WHERE \"Id\" = {1}",
            DateTime.UtcNow.AddMinutes(-15), second.Id.Value);
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletDebitRequests\" SET \"CreatedAt\" = {0} WHERE \"Id\" = {1}",
            DateTime.UtcNow.AddMinutes(-1), third.Id.Value);

        _context.ChangeTracker.Clear();

        var results = await _sut.GetByOwnerAsync(owner.Id);

        results.Count.ShouldBe(3);
        results[0].Id.ShouldBe(third.Id);
        results[1].Id.ShouldBe(second.Id);
        results[2].Id.ShouldBe(first.Id);
    }

    [Fact]
    public async Task GetByOwnerAsync_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        var (owner, wallet, pending) = await SeedWalletAndDebitRequestAsync(amount: 20_000m);
        await SeedAdditionalCreditAsync(wallet, 200_000m);
        var toApprove = SeedAdditionalDebitRequestForWallet(wallet, owner.Id, amount: 30_000m);

        wallet.ApproveDebitRequest(toApprove.Id, owner.Id);
        _context.Wallets.Update(wallet);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var pendingResults = await _sut.GetByOwnerAsync(owner.Id, WalletDebitRequestStatus.Pending);
        var approvedResults = await _sut.GetByOwnerAsync(owner.Id, WalletDebitRequestStatus.Approved);

        pendingResults.Count.ShouldBe(1);
        pendingResults[0].Id.ShouldBe(pending.Id);
        approvedResults.Count.ShouldBe(1);
        approvedResults[0].Id.ShouldBe(toApprove.Id);
    }

    [Fact]
    public async Task GetByOwnerAsync_WhenOwnerHasNoRequests_ReturnsEmptyList()
    {
        var results = await _sut.GetByOwnerAsync(UserId.NewId());

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPendingByOwnerAsync_ReturnsOnlyPendingRequestsOrderedByCreatedAtDescending()
    {
        var (owner, wallet, oldPending) = await SeedWalletAndDebitRequestAsync(amount: 20_000m);
        await SeedAdditionalCreditAsync(wallet, 400_000m);

        var newPending = SeedAdditionalDebitRequestForWallet(wallet, owner.Id, amount: 30_000m);
        var toApprove = SeedAdditionalDebitRequestForWallet(wallet, owner.Id, amount: 40_000m);
        wallet.ApproveDebitRequest(toApprove.Id, owner.Id);
        _context.Wallets.Update(wallet);
        await _context.SaveChangesAsync();

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletDebitRequests\" SET \"CreatedAt\" = {0} WHERE \"Id\" = {1}",
            DateTime.UtcNow.AddMinutes(-30), oldPending.Id.Value);
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletDebitRequests\" SET \"CreatedAt\" = {0} WHERE \"Id\" = {1}",
            DateTime.UtcNow.AddMinutes(-1), newPending.Id.Value);

        _context.ChangeTracker.Clear();

        var results = await _sut.GetPendingByOwnerAsync(owner.Id);

        results.Count.ShouldBe(2);
        results[0].Id.ShouldBe(newPending.Id);
        results[1].Id.ShouldBe(oldPending.Id);
    }

    [Fact]
    public async Task GetPendingByOwnerAsync_WhenOnlyNonPendingExist_ReturnsEmpty()
    {
        var (owner, wallet, request) = await SeedWalletAndDebitRequestAsync(amount: 20_000m);
        wallet.CancelDebitRequest(request.Id, owner.Id);
        _context.Wallets.Update(wallet);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var results = await _sut.GetPendingByOwnerAsync(owner.Id);

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByOwnerAsync_DoesNotReturnRequestsBelongingToOtherOwners()
    {
        var (_, _, requestA) = await SeedWalletAndDebitRequestAsync(amount: 10_000m);
        var (ownerB, _, _) = await SeedWalletAndDebitRequestAsync(amount: 15_000m);

        _context.ChangeTracker.Clear();

        var results = await _sut.GetByOwnerAsync(ownerB.Id);

        results.ShouldNotContain(r => r.Id == requestA.Id);
    }

    private async Task<(Users owner, Wallets wallet, WalletDebitRequest request)>
        SeedWalletAndDebitRequestAsync(decimal amount, string reason = "default-reason")
    {
        var owner = new UserBuilder().Build();
        owner.ClearDomainEvents();
        await _context.Users.AddAsync(owner);

        var wallet = new WalletBuilder().WithOwnerId(owner.Id).Build();
        wallet.Credit(Money.Create(amount + 1_000_000m, "IRT"), "seed-credit", $"seed-{Guid.NewGuid():N}");
        wallet.ClearDomainEvents();
        await _context.Wallets.AddAsync(wallet);
        await _context.SaveChangesAsync();

        var request = wallet.CreateDebitRequest(
            WalletDebitRequestId.NewId(),
            Money.Create(amount, "IRT"),
            reason,
            description: null,
            requestedBy: owner.Id,
            expiryDuration: TimeSpan.FromHours(1));

        wallet.ClearDomainEvents();
        _context.Wallets.Update(wallet);
        await _context.SaveChangesAsync();

        return (owner, wallet, request);
    }

    private WalletDebitRequest SeedAdditionalDebitRequestForWallet(
        Wallets wallet,
        UserId requestedBy,
        decimal amount)
    {
        var request = wallet.CreateDebitRequest(
            WalletDebitRequestId.NewId(),
            Money.Create(amount, "IRT"),
            "extra-reason",
            description: null,
            requestedBy: requestedBy,
            expiryDuration: TimeSpan.FromHours(1));

        wallet.ClearDomainEvents();
        _context.Wallets.Update(wallet);
        return request;
    }

    private Task SeedAdditionalCreditAsync(Wallets wallet, decimal amount)
    {
        wallet.Credit(Money.Create(amount, "IRT"), "extra-credit", $"seed-{Guid.NewGuid():N}");
        wallet.ClearDomainEvents();
        _context.Wallets.Update(wallet);
        return Task.CompletedTask;
    }
}
