using Application.Cache.Contracts;
using Domain.Order.Interfaces;
using Domain.User.ValueObjects;
using Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Infrastructure.BackgroundJobs;

public class ExpiredOrderCleanupJobTests
{
    private sealed class TestContext
    {
        public IServiceScopeFactory ScopeFactory { get; init; } = null!;
        public IDistributedLock DistributedLock { get; init; } = null!;
        public IOrderRepository OrderRepository { get; init; } = null!;
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

        var orderRepository = Substitute.For<IOrderRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var auditService = Substitute.For<IAuditService>();

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(IOrderRepository)).Returns(orderRepository);
        provider.GetService(typeof(IUnitOfWork)).Returns(unitOfWork);
        provider.GetService(typeof(IAuditService)).Returns(auditService);

        return new TestContext
        {
            ScopeFactory = scopeFactory,
            DistributedLock = distributedLock,
            OrderRepository = orderRepository,
            UnitOfWork = unitOfWork,
            AuditService = auditService,
            Cts = new CancellationTokenSource()
        };
    }

    private static async Task RunUntilSavedAsync(ExpiredOrderCleanupJob job, TestContext ctx)
    {
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
    }

    private static global::Domain.Order.Aggregates.Order NewOrder() =>
        new OrderBuilder().WithUserId(UserId.NewId()).Build();

    [Fact]
    public async Task ExecuteAsync_WhenLockNotAcquired_DoesNothing()
    {
        var ctx = BuildContext(lockAcquired: false);
        var job = new ExpiredOrderCleanupJob(ctx.ScopeFactory, ctx.DistributedLock);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try { await job.StartAsync(timeout.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await ctx.OrderRepository.DidNotReceiveWithAnyArgs().FindPendingExpiredAsync(default);
        await ctx.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenExpiredOrdersExist_ExpiresUpdatesSavesAndAudits()
    {
        var ctx = BuildContext();
        var order = NewOrder();
        ctx.OrderRepository
            .FindPendingExpiredAsync(Arg.Any<CancellationToken>())
            .Returns([order]);
        var job = new ExpiredOrderCleanupJob(ctx.ScopeFactory, ctx.DistributedLock);

        await RunUntilSavedAsync(job, ctx);

        ctx.OrderRepository.Received(1).Update(order);
        await ctx.AuditService.Received(1).LogSystemEventAsync(
            "ExpiredOrderCleanup",
            Arg.Is<string>(s => s!.Contains("1")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_AcquiresLockWithExpectedKeyAndExpiry()
    {
        var ctx = BuildContext();
        ctx.OrderRepository
            .FindPendingExpiredAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        var job = new ExpiredOrderCleanupJob(ctx.ScopeFactory, ctx.DistributedLock);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try { await job.StartAsync(timeout.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await ctx.DistributedLock.Received().AcquireAsync(
            "jobs:expired-order-cleanup",
            TimeSpan.FromMinutes(10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoExpiredOrders_DoesNotSaveOrAudit()
    {
        var ctx = BuildContext();
        ctx.OrderRepository
            .FindPendingExpiredAsync(Arg.Any<CancellationToken>())
            .Returns([]);
        var job = new ExpiredOrderCleanupJob(ctx.ScopeFactory, ctx.DistributedLock);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try { await job.StartAsync(timeout.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await ctx.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
        await ctx.AuditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRepositoryThrows_LogsErrorAndContinues()
    {
        var ctx = BuildContext();
        ctx.OrderRepository
            .When(r => r.FindPendingExpiredAsync(Arg.Any<CancellationToken>()))
            .Do(_ => ctx.Cts.Cancel());
        ctx.OrderRepository
            .FindPendingExpiredAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<global::Domain.Order.Aggregates.Order>>(
                new InvalidOperationException("db down")));
        var job = new ExpiredOrderCleanupJob(ctx.ScopeFactory, ctx.DistributedLock);

        try { await job.StartAsync(ctx.Cts.Token); }
        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await ctx.AuditService.Received().LogSystemEventAsync(
            "ExpiredOrderCleanupError",
            Arg.Is<string>(s => s!.Contains("db down")),
            Arg.Any<CancellationToken>());
    }
}
