using Domain.Audit.Entities;
using Domain.Audit.Interfaces;
using Domain.Audit.ValueObjects;
using Infrastructure.Audit.Repositories;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Audit.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class AuditRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IAuditRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new AuditRepository(_context);
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
    public async Task AddAuditLogAsync_ValidLog_PersistsAcrossContexts()
    {
        var auditLog = new AuditLogBuilder()
            .WithEventType("Security")
            .WithAction("Login")
            .WithIpAddress("10.0.0.1")
            .WithEntityType("User")
            .WithEntityId(Guid.NewGuid().ToString())
            .WithDetails("Successful login attempt")
            .WithUserAgent("Mozilla/5.0")
            .Build();
        auditLog.ClearDomainEvents();

        await _sut.AddAuditLogAsync(auditLog);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new AuditRepository(freshContext);
        var loaded = await freshRepo.GetByIdAsync(auditLog.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(auditLog.Id);
        loaded.EventType.ShouldBe("Security");
        loaded.Action.ShouldBe("Login");
        loaded.IpAddress.ShouldBe("10.0.0.1");
        loaded.EntityType.ShouldBe("User");
        loaded.Details.ShouldBe("Successful login attempt");
        loaded.UserAgent.ShouldBe("Mozilla/5.0");
        loaded.IntegrityHash.ShouldNotBeNullOrWhiteSpace();
        loaded.IsArchived.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(AuditLogId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task VerifyIntegrity_AfterRoundTrip_ReturnsTrue()
    {
        var auditLog = new AuditLogBuilder()
            .WithEventType("Order")
            .WithAction("Paid")
            .WithIpAddress("192.168.1.10")
            .Build();
        auditLog.ClearDomainEvents();

        await _sut.AddAuditLogAsync(auditLog);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(auditLog.Id);
        loaded.ShouldNotBeNull();
        loaded!.VerifyIntegrity().ShouldBeTrue();
    }

    [Fact]
    public async Task GetForArchiveAsync_ReturnsOnlyLogsBeforeCutoff()
    {
        var oldLog = new AuditLogBuilder().WithEventType("Order").WithAction("Old").Build();
        var newLog = new AuditLogBuilder().WithEventType("Order").WithAction("New").Build();
        oldLog.ClearDomainEvents();
        newLog.ClearDomainEvents();

        await _sut.AddAuditLogAsync(oldLog);
        await _context.SaveChangesAsync();

        await Task.Delay(50);
        var cutoff = DateTime.UtcNow;
        await Task.Delay(50);

        await _sut.AddAuditLogAsync(newLog);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var results = await _sut.GetForArchiveAsync(
            cutoff,
            includeEventTypes: null,
            excludeEventTypes: null,
            onlyNonArchived: true,
            batchSize: 100);

        results.ShouldContain(l => l.Id == oldLog.Id);
        results.ShouldNotContain(l => l.Id == newLog.Id);
    }

    [Fact]
    public async Task GetForArchiveAsync_WithIncludeEventTypes_FiltersByEventType()
    {
        var securityLog = new AuditLogBuilder().WithEventType("Security").WithAction("Login").Build();
        var orderLog = new AuditLogBuilder().WithEventType("Order").WithAction("Created").Build();
        securityLog.ClearDomainEvents();
        orderLog.ClearDomainEvents();

        await _sut.AddAuditLogAsync(securityLog);
        await _sut.AddAuditLogAsync(orderLog);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var cutoff = DateTime.UtcNow.AddMinutes(1);
        var results = await _sut.GetForArchiveAsync(
            cutoff,
            includeEventTypes: new HashSet<string> { "Security" },
            excludeEventTypes: null,
            onlyNonArchived: true,
            batchSize: 100);

        results.ShouldContain(l => l.Id == securityLog.Id);
        results.ShouldNotContain(l => l.Id == orderLog.Id);
    }

    [Fact]
    public async Task GetForArchiveAsync_WithExcludeEventTypes_ExcludesMatchingEventTypes()
    {
        var securityLog = new AuditLogBuilder().WithEventType("Security").WithAction("Login").Build();
        var orderLog = new AuditLogBuilder().WithEventType("Order").WithAction("Created").Build();
        securityLog.ClearDomainEvents();
        orderLog.ClearDomainEvents();

        await _sut.AddAuditLogAsync(securityLog);
        await _sut.AddAuditLogAsync(orderLog);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var cutoff = DateTime.UtcNow.AddMinutes(1);
        var results = await _sut.GetForArchiveAsync(
            cutoff,
            includeEventTypes: null,
            excludeEventTypes: new HashSet<string> { "Security" },
            onlyNonArchived: true,
            batchSize: 100);

        results.ShouldNotContain(l => l.Id == securityLog.Id);
        results.ShouldContain(l => l.Id == orderLog.Id);
    }

    [Fact]
    public async Task GetForArchiveAsync_WithBatchSize_LimitsReturnedItems()
    {
        for (var i = 0; i < 5; i++)
        {
            var log = new AuditLogBuilder()
                .WithEventType("Order")
                .WithAction("Action" + i)
                .Build();
            log.ClearDomainEvents();
            await _sut.AddAuditLogAsync(log);
        }
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var cutoff = DateTime.UtcNow.AddMinutes(1);
        var results = await _sut.GetForArchiveAsync(
            cutoff,
            includeEventTypes: null,
            excludeEventTypes: null,
            onlyNonArchived: true,
            batchSize: 3);

        results.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetForArchiveAsync_OnlyNonArchivedTrue_ExcludesArchivedLogs()
    {
        var archived = new AuditLogBuilder().WithEventType("Order").WithAction("Archived").Build();
        archived.MarkAsArchived();
        archived.ClearDomainEvents();

        var active = new AuditLogBuilder().WithEventType("Order").WithAction("Active").Build();
        active.ClearDomainEvents();

        await _sut.AddAuditLogAsync(archived);
        await _sut.AddAuditLogAsync(active);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var cutoff = DateTime.UtcNow.AddMinutes(1);
        var results = await _sut.GetForArchiveAsync(
            cutoff,
            includeEventTypes: null,
            excludeEventTypes: null,
            onlyNonArchived: true,
            batchSize: 100);

        results.ShouldNotContain(l => l.Id == archived.Id);
        results.ShouldContain(l => l.Id == active.Id);
    }

    [Fact]
    public async Task RemoveRangeAsync_ExistingLogs_DeletesFromDatabase()
    {
        var log1 = new AuditLogBuilder().WithEventType("Order").WithAction("A1").Build();
        var log2 = new AuditLogBuilder().WithEventType("Order").WithAction("A2").Build();
        log1.ClearDomainEvents();
        log2.ClearDomainEvents();

        await _sut.AddAuditLogAsync(log1);
        await _sut.AddAuditLogAsync(log2);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var toRemove1 = await _sut.GetByIdAsync(log1.Id);
        var toRemove2 = await _sut.GetByIdAsync(log2.Id);
        toRemove1.ShouldNotBeNull();
        toRemove2.ShouldNotBeNull();

        await _sut.RemoveRangeAsync(new[] { toRemove1!, toRemove2! });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        (await _sut.GetByIdAsync(log1.Id)).ShouldBeNull();
        (await _sut.GetByIdAsync(log2.Id)).ShouldBeNull();
    }
}

