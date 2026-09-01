using Domain.Wallet.Entities;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;
using Users = Domain.User.Aggregates.User;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Infrastructure.Wallet.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletDebitRequestConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    private async Task<Users> SeedActiveUserAsync()
    {
        var user = new UserBuilder().Build();
        user.ClearDomainEvents();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<(Wallets wallet, WalletDebitRequest request)> SeedPendingRequestAsync(
        decimal openingBalance = 500_000m,
        decimal amount = 100_000m,
        string reason = "policy review",
        string? description = "please review",
        TimeSpan? expiry = null)
    {
        var owner = await SeedActiveUserAsync();
        var wallet = new WalletBuilder().WithOwnerId(owner.Id).Build();
        wallet.Credit(Money.Create(openingBalance, "IRT"), "seed", "seed-ref");
        var requestId = WalletDebitRequestId.NewId();
        wallet.CreateDebitRequest(
            requestId,
            Money.Create(amount, "IRT"),
            reason,
            description,
            owner.Id,
            expiry ?? TimeSpan.FromHours(24));
        wallet.ClearDomainEvents();

        await _context.Wallets.AddAsync(wallet);
        await _context.SaveChangesAsync();

        var request = wallet.DebitRequests.Single(r => r.Id == requestId);
        return (wallet, request);
    }

    [Fact]
    public async Task Persist_DebitRequest_RoundTripsAllScalarProperties()
    {
        var (wallet, request) = await SeedPendingRequestAsync(
            openingBalance: 500_000m,
            amount: 150_000m,
            reason: "test-reason",
            description: "test-description");
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Set<WalletDebitRequest>()
            .FirstOrDefaultAsync(r => r.Id == request.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(request.Id);
        loaded.WalletId.ShouldBe(wallet.Id);
        loaded.OwnerId.ShouldBe(wallet.OwnerId);
        loaded.Amount.Amount.ShouldBe(150_000m);
        loaded.Amount.Currency.ShouldBe("IRT");
        loaded.Reason.ShouldBe("test-reason");
        loaded.Description.ShouldBe("test-description");
        loaded.Status.ShouldBe(WalletDebitRequestStatus.Pending);
        loaded.RequestedBy.ShouldBe(wallet.OwnerId);
        loaded.ReservationId.ShouldNotBeNull();
        loaded.RespondedAt.ShouldBeNull();
        loaded.RespondedBy.ShouldBeNull();
        loaded.RejectionReason.ShouldBeNull();
    }

    [Fact]
    public async Task Persist_DebitRequest_StatusIsPersistedAsInteger()
    {
        var (_, request) = await SeedPendingRequestAsync();
        _context.ChangeTracker.Clear();

        var rawStatus = await _context.Set<WalletDebitRequest>()
            .Where(r => r.Id == request.Id)
            .Select(r => (int)r.Status)
            .FirstOrDefaultAsync();

        rawStatus.ShouldBe((int)WalletDebitRequestStatus.Pending);
    }

    [Fact]
    public async Task Persist_DebitRequest_AmountStoredInOwnedAmountAndCurrencyColumns()
    {
        var (_, request) = await SeedPendingRequestAsync(amount: 250_000m);
        _context.ChangeTracker.Clear();

        var raw = await _context.Set<WalletDebitRequest>()
            .Where(r => r.Id == request.Id)
            .Select(r => new { r.Amount.Amount, r.Amount.Currency })
            .FirstOrDefaultAsync();

        raw.ShouldNotBeNull();
        raw!.Amount.ShouldBe(250_000m);
        raw.Currency.ShouldBe("IRT");
    }

    [Fact]
    public async Task Persist_DebitRequest_ApproveTransitionPersistsRespondedFields()
    {
        var (wallet, request) = await SeedPendingRequestAsync();
        var loadedWallet = await _context.Wallets.FirstAsync(w => w.Id == wallet.Id);
        loadedWallet.ApproveDebitRequest(request.Id, loadedWallet.OwnerId);
        _context.Wallets.Update(loadedWallet);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.Set<WalletDebitRequest>().FirstAsync(r => r.Id == request.Id);

        loaded.Status.ShouldBe(WalletDebitRequestStatus.Approved);
        loaded.RespondedBy.ShouldBe(wallet.OwnerId);
        loaded.RespondedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Persist_DebitRequest_RejectionReasonIsPersisted()
    {
        var (wallet, request) = await SeedPendingRequestAsync();
        var loadedWallet = await _context.Wallets.FirstAsync(w => w.Id == wallet.Id);
        loadedWallet.RejectDebitRequest(request.Id, loadedWallet.OwnerId, "not authorized");
        _context.Wallets.Update(loadedWallet);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.Set<WalletDebitRequest>().FirstAsync(r => r.Id == request.Id);

        loaded.Status.ShouldBe(WalletDebitRequestStatus.Rejected);
        loaded.RejectionReason.ShouldBe("not authorized");
    }

    [Fact]
    public async Task Persist_DebitRequest_RejectionReasonLongerThan500Characters_ThrowsOnSave()
    {
        var (wallet, request) = await SeedPendingRequestAsync();
        var loadedWallet = await _context.Wallets.FirstAsync(w => w.Id == wallet.Id);
        loadedWallet.RejectDebitRequest(request.Id, loadedWallet.OwnerId, new string('x', 501));
        _context.Wallets.Update(loadedWallet);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_DebitRequest_ReasonLongerThan500Characters_ThrowsOnSave()
    {
        var owner = await SeedActiveUserAsync();
        var wallet = new WalletBuilder().WithOwnerId(owner.Id).Build();
        wallet.Credit(Money.Create(500_000m, "IRT"), "seed", "seed-ref");
        wallet.CreateDebitRequest(
            WalletDebitRequestId.NewId(),
            Money.Create(100_000m, "IRT"),
            new string('r', 501),
            null,
            owner.Id,
            TimeSpan.FromHours(1));
        wallet.ClearDomainEvents();

        await _context.Wallets.AddAsync(wallet);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_DebitRequest_DescriptionLongerThan1000Characters_ThrowsOnSave()
    {
        var owner = await SeedActiveUserAsync();
        var wallet = new WalletBuilder().WithOwnerId(owner.Id).Build();
        wallet.Credit(Money.Create(500_000m, "IRT"), "seed", "seed-ref");
        wallet.CreateDebitRequest(
            WalletDebitRequestId.NewId(),
            Money.Create(100_000m, "IRT"),
            "reason",
            new string('d', 1001),
            owner.Id,
            TimeSpan.FromHours(1));
        wallet.ClearDomainEvents();

        await _context.Wallets.AddAsync(wallet);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public void Model_DebitRequest_UsesTableNameWalletDebitRequests()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletDebitRequest));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("WalletDebitRequests");
    }

    [Fact]
    public void Model_DebitRequest_HasCompositeIndexOnOwnerIdAndStatus()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletDebitRequest));
        var index = entityType!.GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == "IX_WalletDebitRequests_Owner_Status");

        index.ShouldNotBeNull();
        index!.Properties.Select(p => p.Name)
            .ShouldBe(new[] { nameof(WalletDebitRequest.OwnerId), nameof(WalletDebitRequest.Status) });
    }

    [Fact]
    public void Model_DebitRequest_HasIndexOnExpiresAt()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletDebitRequest));
        var index = entityType!.GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == "IX_WalletDebitRequests_ExpiresAt");

        index.ShouldNotBeNull();
    }

    [Fact]
    public async Task Persist_DebitRequest_IsRemovedWhenParentWalletIsDeleted()
    {
        var (wallet, request) = await SeedPendingRequestAsync();
        _context.ChangeTracker.Clear();

        var toDelete = await _context.Wallets.FirstAsync(w => w.Id == wallet.Id);
        _context.Wallets.Remove(toDelete);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var exists = await freshContext.Set<WalletDebitRequest>().AnyAsync(r => r.Id == request.Id);

        exists.ShouldBeFalse();
    }
}
