using Domain.User.ValueObjects;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Enums;
using Domain.Wallet.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Wallet.Repositories;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Wallet.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletFraudAlertRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private WalletFraudAlertRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new WalletFraudAlertRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    [RequiresDockerFact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsAggregateFromDatabase()
    {
        var alert = new WalletFraudAlertBuilder()
            .WithRuleName("HighAmountRule")
            .WithSeverity(FraudAlertSeverity.High)
            .WithDescription("Amount exceeds threshold")
            .WithMetadata("{\"threshold\":1000000}")
            .Build();
        alert.ClearDomainEvents();

        await _sut.AddAsync(alert);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(alert.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(alert.Id);
        loaded.RuleName.ShouldBe("HighAmountRule");
        loaded.Severity.ShouldBe(FraudAlertSeverity.High);
        loaded.Description.ShouldBe("Amount exceeds threshold");
        loaded.Metadata.ShouldBe("{\"threshold\":1000000}");
        loaded.Status.ShouldBe(FraudAlertStatus.Open);
        loaded.TriggeredAt.ShouldNotBe(default);
    }

    [RequiresDockerFact]
    public async Task GetByIdAsync_WhenAlertDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(WalletFraudAlertId.NewId());

        loaded.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task Update_AfterMarkAsReviewed_PersistsReviewedStateReviewerAndNote()
    {
        var alert = new WalletFraudAlertBuilder().Build();
        alert.ClearDomainEvents();
        await _sut.AddAsync(alert);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reviewer = UserId.NewId();
        var loaded = await _sut.GetByIdAsync(alert.Id);
        loaded.ShouldNotBeNull();
        loaded!.MarkAsReviewed(reviewer, "checked manually");

        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(alert.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.Status.ShouldBe(FraudAlertStatus.Reviewed);
        reloaded.ReviewedBy.ShouldBe(reviewer);
        reloaded.ReviewNote.ShouldBe("checked manually");
        reloaded.ReviewedAt.ShouldNotBeNull();
    }

    [RequiresDockerFact]
    public async Task Update_AfterDismiss_PersistsDismissedStateAndReviewNote()
    {
        var alert = new WalletFraudAlertBuilder().Build();
        alert.ClearDomainEvents();
        await _sut.AddAsync(alert);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reviewer = UserId.NewId();
        var loaded = await _sut.GetByIdAsync(alert.Id);
        loaded.ShouldNotBeNull();
        loaded!.Dismiss(reviewer, "false positive");

        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(alert.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.Status.ShouldBe(FraudAlertStatus.Dismissed);
        reloaded.ReviewedBy.ShouldBe(reviewer);
        reloaded.ReviewNote.ShouldBe("false positive");
    }

    [RequiresDockerFact]
    public async Task HasRecentAlertAsync_WhenOpenAlertForSameWalletAndRuleExistsWithinCooldown_ReturnsTrue()
    {
        var walletId = WalletId.NewId();
        var alert = new WalletFraudAlertBuilder()
            .WithWalletId(walletId)
            .WithRuleName("VelocityRule")
            .Build();
        alert.ClearDomainEvents();

        await _sut.AddAsync(alert);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.HasRecentAlertAsync(walletId, "VelocityRule", TimeSpan.FromMinutes(30));

        exists.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task HasRecentAlertAsync_WhenOnlyOlderThanCooldown_ReturnsFalse()
    {
        var walletId = WalletId.NewId();
        var alert = new WalletFraudAlertBuilder()
            .WithWalletId(walletId)
            .WithRuleName("VelocityRule")
            .Build();
        alert.ClearDomainEvents();

        await _sut.AddAsync(alert);
        await _context.SaveChangesAsync();

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"WalletFraudAlerts\" SET \"TriggeredAt\" = {0} WHERE \"Id\" = {1}",
            DateTime.UtcNow.AddHours(-2), alert.Id.Value);

        _context.ChangeTracker.Clear();

        var exists = await _sut.HasRecentAlertAsync(walletId, "VelocityRule", TimeSpan.FromMinutes(30));

        exists.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task HasRecentAlertAsync_WhenAlertIsNotOpen_ReturnsFalse()
    {
        var walletId = WalletId.NewId();
        var alert = new WalletFraudAlertBuilder()
            .WithWalletId(walletId)
            .WithRuleName("VelocityRule")
            .Build();
        alert.Dismiss(UserId.NewId(), "handled");
        alert.ClearDomainEvents();

        await _sut.AddAsync(alert);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.HasRecentAlertAsync(walletId, "VelocityRule", TimeSpan.FromMinutes(30));

        exists.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task HasRecentAlertAsync_WhenRuleNameDiffers_ReturnsFalse()
    {
        var walletId = WalletId.NewId();
        var alert = new WalletFraudAlertBuilder()
            .WithWalletId(walletId)
            .WithRuleName("VelocityRule")
            .Build();
        alert.ClearDomainEvents();

        await _sut.AddAsync(alert);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.HasRecentAlertAsync(walletId, "GeoAnomalyRule", TimeSpan.FromMinutes(30));

        exists.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task HasRecentAlertAsync_WhenWalletIdDiffers_ReturnsFalse()
    {
        var stored = new WalletFraudAlertBuilder()
            .WithRuleName("VelocityRule")
            .Build();
        stored.ClearDomainEvents();

        await _sut.AddAsync(stored);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.HasRecentAlertAsync(WalletId.NewId(), "VelocityRule", TimeSpan.FromMinutes(30));

        exists.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task AddAsync_PersistsSeverityAsIntegerAndRoundTripsEnum()
    {
        var alert = new WalletFraudAlertBuilder()
            .WithSeverity(FraudAlertSeverity.Critical)
            .Build();
        alert.ClearDomainEvents();

        await _sut.AddAsync(alert);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.Set<WalletFraudAlert>().FirstOrDefaultAsync(a => a.Id == alert.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.Severity.ShouldBe(FraudAlertSeverity.Critical);
    }
}
