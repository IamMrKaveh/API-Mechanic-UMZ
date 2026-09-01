using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;

namespace Tests.Infrastructure.Wallet.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletFraudAlertConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    private async Task<WalletFraudAlert> SeedAlertAsync(
        FraudAlertSeverity severity = FraudAlertSeverity.Medium,
        string ruleName = "HighAmountRule",
        string description = "unusual amount detected",
        string? metadata = null)
    {
        var alert = new WalletFraudAlertBuilder()
            .WithSeverity(severity)
            .WithRuleName(ruleName)
            .WithDescription(description)
            .WithMetadata(metadata)
            .Build();
        alert.ClearDomainEvents();

        await _context.WalletFraudAlerts.AddAsync(alert);
        await _context.SaveChangesAsync();
        return alert;
    }

    [Fact]
    public async Task Persist_FraudAlert_RoundTripsAllScalarProperties()
    {
        var alert = await SeedAlertAsync(
            severity: FraudAlertSeverity.High,
            ruleName: "VelocityRule",
            description: "too many txns in 1 min",
            metadata: "{\"count\":42}");
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.WalletFraudAlerts.FirstOrDefaultAsync(a => a.Id == alert.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(alert.Id);
        loaded.WalletId.ShouldBe(alert.WalletId);
        loaded.UserId.ShouldBe(alert.UserId);
        loaded.RuleName.ShouldBe("VelocityRule");
        loaded.Severity.ShouldBe(FraudAlertSeverity.High);
        loaded.Description.ShouldBe("too many txns in 1 min");
        loaded.Metadata.ShouldBe("{\"count\":42}");
        loaded.Status.ShouldBe(FraudAlertStatus.Open);
        loaded.ReviewedBy.ShouldBeNull();
        loaded.ReviewedAt.ShouldBeNull();
        loaded.ReviewNote.ShouldBeNull();
    }

    [Fact]
    public async Task Persist_FraudAlert_SeverityIsPersistedAsInteger()
    {
        var alert = await SeedAlertAsync(severity: FraudAlertSeverity.Critical);
        _context.ChangeTracker.Clear();

        var raw = await _context.WalletFraudAlerts
            .Where(a => a.Id == alert.Id)
            .Select(a => (int)a.Severity)
            .FirstOrDefaultAsync();

        raw.ShouldBe((int)FraudAlertSeverity.Critical);
    }

    [Fact]
    public async Task Persist_FraudAlert_StatusIsPersistedAsInteger()
    {
        var alert = await SeedAlertAsync();
        _context.ChangeTracker.Clear();

        var raw = await _context.WalletFraudAlerts
            .Where(a => a.Id == alert.Id)
            .Select(a => (int)a.Status)
            .FirstOrDefaultAsync();

        raw.ShouldBe((int)FraudAlertStatus.Open);
    }

    [Fact]
    public async Task Persist_FraudAlert_ReviewedTransitionIsPersisted()
    {
        var alert = await SeedAlertAsync();
        var reviewer = UserId.NewId();
        alert.MarkAsReviewed(reviewer, "confirmed suspicious");
        _context.WalletFraudAlerts.Update(alert);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.WalletFraudAlerts.FirstAsync(a => a.Id == alert.Id);

        loaded.Status.ShouldBe(FraudAlertStatus.Reviewed);
        loaded.ReviewedBy.ShouldBe(reviewer);
        loaded.ReviewedAt.ShouldNotBeNull();
        loaded.ReviewNote.ShouldBe("confirmed suspicious");
    }

    [Fact]
    public async Task Persist_FraudAlert_DismissedTransitionIsPersisted()
    {
        var alert = await SeedAlertAsync();
        var dismisser = UserId.NewId();
        alert.Dismiss(dismisser, "false positive");
        _context.WalletFraudAlerts.Update(alert);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _context.WalletFraudAlerts.FirstAsync(a => a.Id == alert.Id);

        loaded.Status.ShouldBe(FraudAlertStatus.Dismissed);
        loaded.ReviewedBy.ShouldBe(dismisser);
        loaded.ReviewedAt.ShouldNotBeNull();
        loaded.ReviewNote.ShouldBe("false positive");
    }

    [Fact]
    public async Task Persist_FraudAlert_RuleNameLongerThan128Characters_ThrowsOnSave()
    {
        var alert = new WalletFraudAlertBuilder().WithRuleName(new string('r', 129)).Build();
        alert.ClearDomainEvents();
        await _context.WalletFraudAlerts.AddAsync(alert);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_FraudAlert_DescriptionLongerThan1000Characters_ThrowsOnSave()
    {
        var alert = new WalletFraudAlertBuilder().WithDescription(new string('d', 1001)).Build();
        alert.ClearDomainEvents();
        await _context.WalletFraudAlerts.AddAsync(alert);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_FraudAlert_MetadataLongerThan2000Characters_ThrowsOnSave()
    {
        var alert = new WalletFraudAlertBuilder().WithMetadata(new string('m', 2001)).Build();
        alert.ClearDomainEvents();
        await _context.WalletFraudAlerts.AddAsync(alert);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_FraudAlert_ReviewNoteLongerThan500Characters_ThrowsOnSave()
    {
        var alert = await SeedAlertAsync();
        alert.MarkAsReviewed(UserId.NewId(), new string('n', 501));
        _context.WalletFraudAlerts.Update(alert);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_FraudAlert_XminConcurrencyTokenChangesAfterUpdate()
    {
        var alert = await SeedAlertAsync();
        var initialXmin = _context.Entry(alert).Property<uint>("xmin").CurrentValue;

        alert.MarkAsReviewed(UserId.NewId(), "note");
        _context.WalletFraudAlerts.Update(alert);
        await _context.SaveChangesAsync();

        var updatedXmin = _context.Entry(alert).Property<uint>("xmin").CurrentValue;
        updatedXmin.ShouldNotBe(initialXmin);
    }

    [Fact]
    public async Task Persist_FraudAlert_ConcurrencyConflict_ThrowsDbUpdateConcurrencyException()
    {
        var alert = await SeedAlertAsync();
        _context.ChangeTracker.Clear();

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var alertA = await contextA.WalletFraudAlerts.FirstAsync(a => a.Id == alert.Id);
        var alertB = await contextB.WalletFraudAlerts.FirstAsync(a => a.Id == alert.Id);

        alertA.MarkAsReviewed(UserId.NewId(), "reviewer-a");
        contextA.WalletFraudAlerts.Update(alertA);
        await contextA.SaveChangesAsync();

        alertB.Dismiss(UserId.NewId(), "dismisser-b");
        contextB.WalletFraudAlerts.Update(alertB);

        await Should.ThrowAsync<DbUpdateConcurrencyException>(async () => await contextB.SaveChangesAsync());
    }

    [Fact]
    public void Model_FraudAlert_UsesTableNameWalletFraudAlerts()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletFraudAlert));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("WalletFraudAlerts");
    }

    [Theory]
    [InlineData("IX_WalletFraudAlerts_Status")]
    [InlineData("IX_WalletFraudAlerts_Severity")]
    [InlineData("IX_WalletFraudAlerts_UserId")]
    [InlineData("IX_WalletFraudAlerts_WalletId")]
    [InlineData("IX_WalletFraudAlerts_TriggeredAt")]
    [InlineData("IX_WalletFraudAlerts_Wallet_Rule_Time")]
    public void Model_FraudAlert_DeclaresExpectedIndex(string indexName)
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletFraudAlert));
        var index = entityType!.GetIndexes().FirstOrDefault(i => i.GetDatabaseName() == indexName);

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_FraudAlert_DomainEventsAreIgnored()
    {
        var entityType = _context.Model.FindEntityType(typeof(WalletFraudAlert));
        var domainEventsProperty = entityType!.FindProperty(nameof(WalletFraudAlert.DomainEvents));
        var domainEventsNavigation = entityType.FindNavigation(nameof(WalletFraudAlert.DomainEvents));

        domainEventsProperty.ShouldBeNull();
        domainEventsNavigation.ShouldBeNull();
    }
}
