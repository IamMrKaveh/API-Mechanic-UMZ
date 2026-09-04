using Application.Cache.Contracts;
using Infrastructure.BackgroundJobs;
using Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.BackgroundJobs;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OutboxArchiveJobTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly ILogger<OutboxArchiveJob> _logger = Substitute.For<ILogger<OutboxArchiveJob>>();

    private OutboxArchiveJob BuildJob()
    {
        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(true);
        _distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(handle);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(DBContext)).Returns(Context);
        provider.GetService(typeof(IDistributedLock)).Returns(_distributedLock);

        return new OutboxArchiveJob(scopeFactory, _logger);
    }

    private static OutboxMessage OldProcessedMessage(DateTime createdAt, DateTime processedAt) =>
        OutboxMessage.Rehydrate(
            OutboxMessageId.NewId(), "OrderCreated", "{\"id\":1}",
            createdAt, processedAt, null, 0, false, null, null);

    private static OutboxMessage PendingMessage() =>
        OutboxMessage.Create("OrderCreated", "{\"id\":2}", DateTime.UtcNow);

    [Fact]
    public async Task ExecuteAsync_WithOldProcessedMessage_ArchivesAndRemovesIt()
    {
        var oldDate = DateTime.UtcNow.AddDays(-40);
        var oldProcessed = OldProcessedMessage(oldDate, oldDate.AddHours(1));
        var recentProcessed = OldProcessedMessage(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-5).AddHours(1));
        var pending = PendingMessage();
        Context.OutboxMessages.AddRange(oldProcessed, recentProcessed, pending);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var job = BuildJob();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);
        Context.ChangeTracker.Clear();

        await using var verify = Fixture.CreateContext();
        (await verify.OutboxMessages.FindAsync(oldProcessed.Id)).ShouldBeNull();
        (await verify.OutboxMessages.FindAsync(recentProcessed.Id)).ShouldNotBeNull();
        (await verify.OutboxMessages.FindAsync(pending.Id)).ShouldNotBeNull();
        var archived = await verify.OutboxArchiveMessages.FirstOrDefaultAsync(a => a.Id == oldProcessed.Id);
        archived.ShouldNotBeNull();
        archived!.Type.ShouldBe("OrderCreated");
    }

    [Fact]
    public async Task ExecuteAsync_WithPoisonedOldMessage_KeepsIt()
    {
        var oldDate = DateTime.UtcNow.AddDays(-40);
        var poisoned = OutboxMessage.Rehydrate(
            OutboxMessageId.NewId(), "OrderCreated", "{\"id\":3}",
            oldDate, oldDate.AddHours(1), "boom", 5, true, null, null);
        Context.OutboxMessages.Add(poisoned);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var job = BuildJob();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);
        Context.ChangeTracker.Clear();

        await using var verify = Fixture.CreateContext();
        (await verify.OutboxMessages.FindAsync(poisoned.Id)).ShouldNotBeNull();
        (await verify.OutboxArchiveMessages.FirstOrDefaultAsync(a => a.Id == poisoned.Id)).ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_AcquiresLockWithExpectedKeyAndExpiry()
    {
        var job = BuildJob();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await _distributedLock.Received().AcquireAsync(
            "jobs:outbox-archive",
            TimeSpan.FromMinutes(15),
            Arg.Any<CancellationToken>());
    }
}
