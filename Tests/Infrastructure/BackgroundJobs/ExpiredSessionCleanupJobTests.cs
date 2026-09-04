using Application.Cache.Contracts;
using Domain.Security.Interfaces;
using Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.BackgroundJobs;

public class ExpiredSessionCleanupJobTests
{
    private sealed class TestContext
    {
        public IServiceScopeFactory ScopeFactory { get; init; } = null!;
        public IDistributedLock DistributedLock { get; init; } = null!;
        public ISessionRepository SessionRepository { get; init; } = null!;
        public IUnitOfWork UnitOfWork { get; init; } = null!;
        public IAuditService AuditService { get; init; } = null!;
        public CancellationTokenSource Cts { get; init; } = null!;
    }

    private static TestContext BuildContext(bool lockAcquired = true)
    {
        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(lockAcquired);
        var distributedLock = Substitute.For<IDistributedLock>();
        distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(handle);

        var sessionRepository = Substitute.For<ISessionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var auditService = Substitute.For<IAuditService>();

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(ISessionRepository)).Returns(sessionRepository);
        provider.GetService(typeof(IUnitOfWork)).Returns(unitOfWork);
        provider.GetService(typeof(IAuditService)).Returns(auditService);

        return new TestContext
        {
            ScopeFactory = scopeFactory,
            DistributedLock = distributedLock,
            SessionRepository = sessionRepository,
            UnitOfWork = unitOfWork,
            AuditService = auditService,
            Cts = new CancellationTokenSource()
        };
    }

    private static async Task RunWithTimeoutAsync(ExpiredSessionCleanupJob job, CancellationToken token)
    {
        try { await job.StartAsync(token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLockNotAcquired_DoesNothing()
    {
        var ctx = BuildContext(lockAcquired: false);
        var job = new ExpiredSessionCleanupJob(ctx.ScopeFactory, ctx.DistributedLock, Substitute.For<IDateTimeProvider>());

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await RunWithTimeoutAsync(job, timeout.Token);

        await ctx.SessionRepository.DidNotReceiveWithAnyArgs().GetExpiredActiveSessionsAsync(default, default);
        await ctx.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExpiredSessionsExist_MarksExpiredSavesAndAudits()
    {
        var ctx = BuildContext();
        var session = new UserSessionBuilder().Build();
        ctx.SessionRepository
            .GetExpiredActiveSessionsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([session]);
        var job = new ExpiredSessionCleanupJob(ctx.ScopeFactory, ctx.DistributedLock, Substitute.For<IDateTimeProvider>());

        ctx.UnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                ctx.Cts.Cancel();
                return Task.CompletedTask;
            });

        try { await job.StartAsync(ctx.Cts.Token); }
        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        session.IsRevoked.ShouldBeTrue();
        session.RevocationReason.ShouldBe(global::Domain.Security.Enums.SessionRevocationReason.Expired);
        await ctx.AuditService.Received(1).LogSystemEventAsync(
            "ExpiredSessionCleanup",
            Arg.Is<string>(s => s!.Contains("1")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_AcquiresLockWithExpectedKeyAndExpiry()
    {
        var ctx = BuildContext();
        ctx.SessionRepository
            .GetExpiredActiveSessionsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var job = new ExpiredSessionCleanupJob(ctx.ScopeFactory, ctx.DistributedLock, Substitute.For<IDateTimeProvider>());
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await RunWithTimeoutAsync(job, timeout.Token);

        await ctx.DistributedLock.Received().AcquireAsync(
            "jobs:expired-session-cleanup",
            TimeSpan.FromMinutes(30),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoExpiredSessions_DoesNotSaveOrAudit()
    {
        var ctx = BuildContext();
        ctx.SessionRepository
            .GetExpiredActiveSessionsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var job = new ExpiredSessionCleanupJob(ctx.ScopeFactory, ctx.DistributedLock, Substitute.For<IDateTimeProvider>());

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await RunWithTimeoutAsync(job, timeout.Token);

        await ctx.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await ctx.AuditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepositoryThrows_LogsErrorAndContinues()
    {
        var ctx = BuildContext();
        ctx.SessionRepository
            .When(r => r.GetExpiredActiveSessionsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()))
            .Do(_ => ctx.Cts.Cancel());
        ctx.SessionRepository
            .GetExpiredActiveSessionsAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<global::Domain.Security.Aggregates.UserSession>>(
                new InvalidOperationException("db down")));
        var job = new ExpiredSessionCleanupJob(ctx.ScopeFactory, ctx.DistributedLock, Substitute.For<IDateTimeProvider>());

        try { await job.StartAsync(ctx.Cts.Token); }
        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await ctx.AuditService.Received().LogSystemEventAsync(
            "ExpiredSessionCleanupError",
            Arg.Is<string>(s => s!.Contains("db down")),
            Arg.Any<CancellationToken>());
    }
}
