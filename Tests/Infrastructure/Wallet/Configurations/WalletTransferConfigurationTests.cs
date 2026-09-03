using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;

namespace Tests.Infrastructure.Wallet.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletTransferConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    private async Task<WalletTransfer> SeedTransferAsync(
        decimal amount = 50_000m,
        string otpHash = "0123456789abcdef0123456789abcdef",
        string? description = null)
    {
        var transfer = new WalletTransferBuilder()
            .WithAmount(amount)
            .WithOtpHash(otpHash)
            .WithDescription(description)
            .Build();
        transfer.ClearDomainEvents();

        await _context.Set<WalletTransfer>().AddAsync(transfer);
        await _context.SaveChangesAsync();
        return transfer;
    }

    [Fact]
    public async Task Persist_Transfer_RoundTripsAllScalarProperties()
    {
        var transfer = await SeedTransferAsync(amount: 75_000m, description: "birthday gift");
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Set<WalletTransfer>().FirstOrDefaultAsync(t => t.Id == transfer.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(transfer.Id);
        loaded.FromUserId.ShouldBe(transfer.FromUserId);
        loaded.ToUserId.ShouldBe(transfer.ToUserId);
        loaded.Amount.Amount.ShouldBe(75_000m);
        loaded.Amount.Currency.ShouldBe("IRT");
        loaded.Description.ShouldBe("birthday gift");
        loaded.Status.ShouldBe(WalletTransferStatus.PendingOtp);
        loaded.OtpHash.ShouldBe(transfer.OtpHash);
        loaded.OtpAttempts.ShouldBe(0);
        loaded.CorrelationId.ShouldBe(transfer.CorrelationId);
        loaded.CompletedAt.ShouldBeNull();
        loaded.CancelledAt.ShouldBeNull();
        loaded.FailureReason.ShouldBeNull();
    }

    [Fact]
    public async Task Persist_Transfer_StatusIsStoredAsString()
    {
        var transfer = await SeedTransferAsync();
        _context.ChangeTracker.Clear();

        var transferId = transfer.Id.Value;
        var raw = await _context.Database
            .SqlQuery<string>($"SELECT \"Status\" AS \"Value\" FROM \"WalletTransfers\" WHERE \"Id\" = {transferId}")
            .FirstOrDefaultAsync();

        raw.ShouldBe(WalletTransferStatus.PendingOtp.ToString());
    }

    [Fact]
    public async Task Persist_Transfer_CompletedTransitionPersistsCompletedAt()
    {
        var transfer = await SeedTransferAsync();
        transfer.MarkCompleted();
        _context.Set<WalletTransfer>().Update(transfer);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.Set<WalletTransfer>().FirstAsync(t => t.Id == transfer.Id);

        loaded.Status.ShouldBe(WalletTransferStatus.Completed);
        loaded.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Persist_Transfer_CancelTransitionPersistsCancelledAt()
    {
        var transfer = await SeedTransferAsync();
        transfer.Cancel(transfer.FromUserId);
        _context.Set<WalletTransfer>().Update(transfer);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.Set<WalletTransfer>().FirstAsync(t => t.Id == transfer.Id);

        loaded.Status.ShouldBe(WalletTransferStatus.Cancelled);
        loaded.CancelledAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Persist_Transfer_DuplicateCorrelationId_ThrowsDueToUniqueIndex()
    {
        var first = await SeedTransferAsync();
        _context.ChangeTracker.Clear();

        var second = new WalletTransferBuilder().Build();
        second.ClearDomainEvents();
        typeof(WalletTransfer)
            .GetProperty(nameof(WalletTransfer.CorrelationId))!
            .SetValue(second, first.CorrelationId);
        await _context.Set<WalletTransfer>().AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_Transfer_OtpHashLongerThan128Characters_ThrowsOnSave()
    {
        var transfer = new WalletTransferBuilder().WithOtpHash(new string('h', 129)).Build();
        transfer.ClearDomainEvents();
        await _context.Set<WalletTransfer>().AddAsync(transfer);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_Transfer_DescriptionLongerThan500Characters_ThrowsOnSave()
    {
        var transfer = new WalletTransferBuilder().WithDescription(new string('d', 501)).Build();
        transfer.ClearDomainEvents();
        await _context.Set<WalletTransfer>().AddAsync(transfer);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_Transfer_XminConcurrencyTokenChangesAfterUpdate()
    {
        var transfer = await SeedTransferAsync();
        var initialXmin = _context.Entry(transfer).Property<uint>("xmin").CurrentValue;

        transfer.MarkCompleted();
        _context.Set<WalletTransfer>().Update(transfer);
        await _context.SaveChangesAsync();

        var updatedXmin = _context.Entry(transfer).Property<uint>("xmin").CurrentValue;
        updatedXmin.ShouldNotBe(initialXmin);
    }

    [Fact]
    public async Task Persist_Transfer_ConcurrencyConflict_ThrowsDbUpdateConcurrencyException()
    {
        var transfer = await SeedTransferAsync();
        _context.ChangeTracker.Clear();

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var transferA = await contextA.Set<WalletTransfer>().FirstAsync(t => t.Id == transfer.Id);
        var transferB = await contextB.Set<WalletTransfer>().FirstAsync(t => t.Id == transfer.Id);

        transferA.MarkCompleted();
        contextA.Set<WalletTransfer>().Update(transferA);
        await contextA.SaveChangesAsync();

        transferB.Cancel(transferB.FromUserId);
        contextB.Set<WalletTransfer>().Update(transferB);

        await Should.ThrowAsync<DbUpdateConcurrencyException>(async () => await contextB.SaveChangesAsync());
    }

    [Fact]
    public void Model_Transfer_UsesTableNameWalletTransfers()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletTransfer));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("WalletTransfers");
    }

    [Theory]
    [InlineData("IX_WalletTransfers_FromUserId")]
    [InlineData("IX_WalletTransfers_ToUserId")]
    [InlineData("IX_WalletTransfers_Status")]
    [InlineData("IX_WalletTransfers_CreatedAt")]
    [InlineData("IX_WalletTransfers_CorrelationId")]
    public void Model_Transfer_DeclaresExpectedIndex(string indexName)
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletTransfer));
        var index = entityType!.GetIndexes().FirstOrDefault(i => i.GetDatabaseName() == indexName);

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_Transfer_CorrelationIdIndexIsUnique()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletTransfer));
        var index = entityType!.GetIndexes()
            .First(i => i.GetDatabaseName() == "IX_WalletTransfers_CorrelationId");

        index.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void Model_Transfer_FromUserIdAndToUserIdAreMappedToDeclaredColumns()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletTransfer));
        var storeObject = StoreObjectIdentifier.Table(entityType!.GetTableName()!, entityType.GetSchema());

        entityType.FindProperty(nameof(WalletTransfer.FromUserId))!
            .GetColumnName(storeObject).ShouldBe("FromUserId");
        entityType.FindProperty(nameof(WalletTransfer.ToUserId))!
            .GetColumnName(storeObject).ShouldBe("ToUserId");
    }

    [Fact]
    public void Model_Transfer_DomainEventsAreIgnored()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletTransfer));
        var domainEventsProperty = entityType!.FindProperty(nameof(WalletTransfer.DomainEvents));
        var domainEventsNavigation = entityType.FindNavigation(nameof(WalletTransfer.DomainEvents));

        domainEventsProperty.ShouldBeNull();
        domainEventsNavigation.ShouldBeNull();
    }
}
