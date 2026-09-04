using Application.Cache.Contracts;
using Domain.Wallet.Aggregates;
using Domain.Wallet.Interfaces;
using Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.BackgroundJobs;

public class WalletTopUpCleanupJobTests
{
    private sealed class TestContext
    {
        public IServiceScopeFactory ScopeFactory { get; init; } = null!;
        public IDistributedLock DistributedLock { get; init; } = null!;
        public IWalletTopUpRepository Repository { get; init; } = null!;
        public IUnitOfWork UnitOfWork { get; init; } = null!;
        public ILogger<WalletTopUpCleanupJob> Logger { get; init; } = null!;
        public IDateTimeProvider DateTimeProvider { get; init; } = null!;
        public CancellationTokenSource Cts { get; init; } = null!;
    }

    private static readonly DateTime FixedNow = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private static TestContext BuildContext(bool lockAcquired = true)
    {
        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(lockAcquired);
        var distributedLock = Substitute.For<IDistributedLock>();
        distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(handle);

        var repository = Substitute.For<IWalletTopUpRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(IWalletTopUpRepository)).Returns(repository);
        provider.GetService(typeof(IUnitOfWork)).Returns(unitOfWork);

        return new TestContext
        {
            ScopeFactory = scopeFactory,
            DistributedLock = distributedLock,
            Repository = repository,
            UnitOfWork = unitOfWork,
            Logger = Substitute.For<ILogger<WalletTopUpCleanupJob>>(),
            DateTimeProvider = dateTimeProvider,
            Cts = new CancellationTokenSource()
        };
    }

    private static WalletTopUpCleanupJob BuildJob(TestContext ctx) =>
        new(ctx.ScopeFactory, ctx.DistributedLock, ctx.Logger, ctx.DateTimeProvider);

    private static async Task RunWithTimeoutAsync(WalletTopUpCleanupJob job, CancellationToken token)
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
        var job = BuildJob(ctx);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await RunWithTimeoutAsync(job, timeout.Token);

        await ctx.Repository.DidNotReceiveWithAnyArgs().GetPendingOlderThanAsync(default, default, default);
        await ctx.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStaleTopUpsExist_MarksFailedUpdatesSavesAndLogs()
    {
        var ctx = BuildContext();
        var topUp = new WalletTopUpBuilder().Build();
        ctx.Repository
            .GetPendingOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([topUp]);
        var job = BuildJob(ctx);

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

        topUp.Status.ShouldBe(global::Domain.Wallet.Enums.WalletTopUpStatus.Failed);
        ctx.Repository.Received(1).Update(topUp);
        await ctx.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoStaleTopUps_DoesNotSave()
    {
        var ctx = BuildContext();
        ctx.Repository
            .GetPendingOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var job = BuildJob(ctx);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await RunWithTimeoutAsync(job, timeout.Token);

        ctx.Repository.DidNotReceiveWithAnyArgs().Update(default!);
        await ctx.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_QueriesWithThirtyMinuteCutoffAndBatchOf100()
    {
        var ctx = BuildContext();
        DateTime capturedCutoff = default;
        int capturedBatch = 0;
        ctx.Repository
            .GetPendingOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedCutoff = call.Arg<DateTime>();
                capturedBatch = call.Arg<int>();
                ctx.Cts.Cancel();
                return Task.FromResult<IReadOnlyList<WalletTopUp>>([]);
            });
        var before = FixedNow;
        var job = BuildJob(ctx);

        try { await job.StartAsync(ctx.Cts.Token); }
        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        capturedBatch.ShouldBe(100);
        capturedCutoff.ShouldBeGreaterThan(before.AddMinutes(-31));
        capturedCutoff.ShouldBeLessThan(before.AddMinutes(-29));
    }

    [Fact]
    public async Task ExecuteAsync_AcquiresLockWithExpectedKeyAndExpiry()
    {
        var ctx = BuildContext();
        ctx.Repository
            .GetPendingOlderThanAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var job = BuildJob(ctx);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await RunWithTimeoutAsync(job, timeout.Token);

        await ctx.DistributedLock.Received().AcquireAsync(
            "jobs:wallet-topup-cleanup",
            TimeSpan.FromMinutes(4),
            Arg.Any<CancellationToken>());
    }
}
