using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;
using Infrastructure.Persistence.Context;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Wallet.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletWithdrawalRequestConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    private async Task<WalletWithdrawalRequest> SeedWithdrawalAsync(
        decimal amount = 100_000m,
        string? description = null,
        string accountHolder = "John Doe")
    {
        var request = new WalletWithdrawalRequestBuilder()
            .WithAmount(amount)
            .WithAccountHolder(accountHolder)
            .WithDescription(description)
            .Build();
        request.ClearDomainEvents();

        await _context.WalletWithdrawalRequests.AddAsync(request);
        await _context.SaveChangesAsync();
        return request;
    }

    [Fact]
    public async Task Persist_Withdrawal_RoundTripsAllScalarProperties()
    {
        var request = await SeedWithdrawalAsync(
            amount: 250_000m,
            description: "monthly payout",
            accountHolder: "Ali Rezaei");
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.WalletWithdrawalRequests.FirstOrDefaultAsync(w => w.Id == request.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(request.Id);
        loaded.UserId.ShouldBe(request.UserId);
        loaded.Amount.Amount.ShouldBe(250_000m);
        loaded.Amount.Currency.ShouldBe("IRT");
        loaded.Iban.Value.ShouldBe(request.Iban.Value);
        loaded.AccountHolder.ShouldBe("Ali Rezaei");
        loaded.Description.ShouldBe("monthly payout");
        loaded.ReservationId.ShouldBe(request.ReservationId);
        loaded.Status.ShouldBe(WalletWithdrawalStatus.Pending);
        loaded.ApprovedAt.ShouldBeNull();
        loaded.RejectedAt.ShouldBeNull();
        loaded.PaidAt.ShouldBeNull();
        loaded.CancelledAt.ShouldBeNull();
        loaded.RejectionReason.ShouldBeNull();
        loaded.BankReferenceNumber.ShouldBeNull();
    }

    [Fact]
    public async Task Persist_Withdrawal_StatusIsStoredAsString()
    {
        var request = await SeedWithdrawalAsync();
        _context.ChangeTracker.Clear();

        var raw = await _context.WalletWithdrawalRequests
            .Where(w => w.Id == request.Id)
            .Select(w => EF.Property<string>(w, nameof(WalletWithdrawalRequest.Status)))
            .FirstOrDefaultAsync();

        raw.ShouldBe(WalletWithdrawalStatus.Pending.ToString());
    }

    [Fact]
    public async Task Persist_Withdrawal_ApproveTransitionPersistsApprovedAt()
    {
        var request = await SeedWithdrawalAsync();
        var admin = UserId.NewId();
        request.Approve(admin);
        _context.WalletWithdrawalRequests.Update(request);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.WalletWithdrawalRequests.FirstAsync(w => w.Id == request.Id);

        loaded.Status.ShouldBe(WalletWithdrawalStatus.Approved);
        loaded.ApprovedAt.ShouldNotBeNull();
        loaded.ProcessedBy.ShouldBe(admin);
    }

    [Fact]
    public async Task Persist_Withdrawal_RejectTransitionPersistsRejectionReasonAndRejectedAt()
    {
        var request = await SeedWithdrawalAsync();
        var admin = UserId.NewId();
        request.Reject(admin, "documents missing");
        _context.WalletWithdrawalRequests.Update(request);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.WalletWithdrawalRequests.FirstAsync(w => w.Id == request.Id);

        loaded.Status.ShouldBe(WalletWithdrawalStatus.Rejected);
        loaded.RejectionReason.ShouldBe("documents missing");
        loaded.RejectedAt.ShouldNotBeNull();
        loaded.ProcessedBy.ShouldBe(admin);
    }

    [Fact]
    public async Task Persist_Withdrawal_PaidTransitionPersistsBankReferenceAndPaidAt()
    {
        var request = await SeedWithdrawalAsync();
        var admin = UserId.NewId();
        request.MarkPaid(admin, "BANK-REF-12345");
        _context.WalletWithdrawalRequests.Update(request);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.WalletWithdrawalRequests.FirstAsync(w => w.Id == request.Id);

        loaded.Status.ShouldBe(WalletWithdrawalStatus.Paid);
        loaded.BankReferenceNumber.ShouldBe("BANK-REF-12345");
        loaded.PaidAt.ShouldNotBeNull();
        loaded.ProcessedBy.ShouldBe(admin);
    }

    [Fact]
    public async Task Persist_Withdrawal_CancelTransitionPersistsCancelledAt()
    {
        var request = await SeedWithdrawalAsync();
        request.Cancel(request.UserId);
        _context.WalletWithdrawalRequests.Update(request);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.WalletWithdrawalRequests.FirstAsync(w => w.Id == request.Id);

        loaded.Status.ShouldBe(WalletWithdrawalStatus.Cancelled);
        loaded.CancelledAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Persist_Withdrawal_DuplicateReservationId_ThrowsDueToUniqueIndex()
    {
        var first = await SeedWithdrawalAsync();

        var second = new WalletWithdrawalRequestBuilder()
            .WithReservationId(first.ReservationId)
            .Build();
        second.ClearDomainEvents();
        await _context.WalletWithdrawalRequests.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_Withdrawal_AccountHolderLongerThan150Characters_ThrowsOnSave()
    {
        var request = new WalletWithdrawalRequestBuilder()
            .WithAccountHolder(new string('a', 151))
            .Build();
        request.ClearDomainEvents();
        await _context.WalletWithdrawalRequests.AddAsync(request);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_Withdrawal_DescriptionLongerThan500Characters_ThrowsOnSave()
    {
        var request = new WalletWithdrawalRequestBuilder()
            .WithDescription(new string('d', 501))
            .Build();
        request.ClearDomainEvents();
        await _context.WalletWithdrawalRequests.AddAsync(request);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_Withdrawal_RejectionReasonLongerThan500Characters_ThrowsOnSave()
    {
        var request = await SeedWithdrawalAsync();
        request.Reject(UserId.NewId(), new string('r', 501));
        _context.WalletWithdrawalRequests.Update(request);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_Withdrawal_BankReferenceLongerThan64Characters_ThrowsOnSave()
    {
        var request = await SeedWithdrawalAsync();
        request.MarkPaid(UserId.NewId(), new string('b', 65));
        _context.WalletWithdrawalRequests.Update(request);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_Withdrawal_XminConcurrencyTokenChangesAfterUpdate()
    {
        var request = await SeedWithdrawalAsync();
        var initialXmin = _context.Entry(request).Property<uint>("xmin").CurrentValue;

        request.Approve(UserId.NewId());
        _context.WalletWithdrawalRequests.Update(request);
        await _context.SaveChangesAsync();

        var updatedXmin = _context.Entry(request).Property<uint>("xmin").CurrentValue;
        updatedXmin.ShouldNotBe(initialXmin);
    }

    [Fact]
    public async Task Persist_Withdrawal_ConcurrencyConflict_ThrowsDbUpdateConcurrencyException()
    {
        var request = await SeedWithdrawalAsync();
        _context.ChangeTracker.Clear();

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var requestA = await contextA.WalletWithdrawalRequests.FirstAsync(w => w.Id == request.Id);
        var requestB = await contextB.WalletWithdrawalRequests.FirstAsync(w => w.Id == request.Id);

        requestA.Approve(UserId.NewId());
        contextA.WalletWithdrawalRequests.Update(requestA);
        await contextA.SaveChangesAsync();

        requestB.Reject(UserId.NewId(), "duplicated");
        contextB.WalletWithdrawalRequests.Update(requestB);

        await Should.ThrowAsync<DbUpdateConcurrencyException>(async () => await contextB.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_Withdrawal_IbanIsPersistedAsStringViaConversion()
    {
        var request = await SeedWithdrawalAsync();
        _context.ChangeTracker.Clear();

        var raw = await _context.WalletWithdrawalRequests
            .Where(w => w.Id == request.Id)
            .Select(w => EF.Property<string>(w, nameof(WalletWithdrawalRequest.Iban)))
            .FirstOrDefaultAsync();

        raw.ShouldBe(request.Iban.Value);
    }

    [Fact]
    public void Model_Withdrawal_UsesTableNameWalletWithdrawalRequests()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletWithdrawalRequest));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("WalletWithdrawalRequests");
    }

    [Theory]
    [InlineData("IX_WalletWithdrawalRequests_UserId")]
    [InlineData("IX_WalletWithdrawalRequests_Status")]
    [InlineData("IX_WalletWithdrawalRequests_CreatedAt")]
    [InlineData("IX_WalletWithdrawalRequests_ReservationId")]
    public void Model_Withdrawal_DeclaresExpectedIndex(string indexName)
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletWithdrawalRequest));
        var index = entityType!.GetIndexes().FirstOrDefault(i => i.GetDatabaseName() == indexName);

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_Withdrawal_ReservationIdIndexIsUnique()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletWithdrawalRequest));
        var index = entityType!.GetIndexes()
            .First(i => i.GetDatabaseName() == "IX_WalletWithdrawalRequests_ReservationId");

        index.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void Model_Withdrawal_IbanColumnMaxLengthIs32()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletWithdrawalRequest));
        var property = entityType!.FindProperty(nameof(WalletWithdrawalRequest.Iban));

        property!.GetMaxLength().ShouldBe(32);
    }

    [Fact]
    public void Model_Withdrawal_DomainEventsAreIgnored()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletWithdrawalRequest));
        var domainEventsProperty = entityType!.FindProperty(nameof(WalletWithdrawalRequest.DomainEvents));
        var domainEventsNavigation = entityType.FindNavigation(nameof(WalletWithdrawalRequest.DomainEvents));

        domainEventsProperty.ShouldBeNull();
        domainEventsNavigation.ShouldBeNull();
    }
}
