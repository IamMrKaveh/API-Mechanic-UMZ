using Application.Cache.Contracts;
using Infrastructure.BackgroundJobs;
using Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Infrastructure.BackgroundJobs;

public class OutboxProcessingJobTests
{
    private sealed class TestContext
    { public IServiceScopeFactory ScopeFactory { get; init; } = null!; public IDistributedLock DistributedLock { get; init; } = null!; public IOutboxProcessor OutboxProcessor { get; init; } = null!; public ILogger<OutboxProcessingJob> Logger { get; init; } = null!; public CancellationTokenSource Cts { get; init; } = null!; }

    private static TestContext BuildContext()
    {
        var distributedLock = Substitute.For<IDistributedLock>();
        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(true);
        distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<ILockHandle?>(handle));

        var outboxProcessor = Substitute.For<IOutboxProcessor>();

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(IDistributedLock)).Returns(distributedLock);
        provider.GetService(typeof(IOutboxProcessor)).Returns(outboxProcessor);

        return new TestContext
        {
            ScopeFactory = scopeFactory,
            DistributedLock = distributedLock,
            OutboxProcessor = outboxProcessor,
            Logger = Substitute.For<ILogger<OutboxProcessingJob>>(),
            Cts = new CancellationTokenSource()
        };
    }

    private static async Task RunUntilFirstProcessAsync(OutboxProcessingJob job, TestContext ctx)
    {
        ctx.OutboxProcessor
            .ProcessAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                ctx.Cts.Cancel();
                return Task.CompletedTask;
            });

        await job.StartAsync(ctx.Cts.Token);
        if (job.ExecuteTask is not null)
            await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(5));
        await job.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLockAcquired_CallsOutboxProcessor()
    {
        var ctx = BuildContext();
        var job = new OutboxProcessingJob(ctx.ScopeFactory, ctx.Logger);

        await RunUntilFirstProcessAsync(job, ctx);

        await ctx.OutboxProcessor.ReceivedWithAnyArgs()
            .ProcessAsync(default, default);
    }

    [Fact]
    public async Task ExecuteAsync_AcquiresLockWithExpectedKeyAndExpiry()
    {
        var ctx = BuildContext();
        var job = new OutboxProcessingJob(ctx.ScopeFactory, ctx.Logger);

        await RunUntilFirstProcessAsync(job, ctx);

        await ctx.DistributedLock.Received().AcquireAsync(
            "jobs:outbox-processing",
            TimeSpan.FromMinutes(2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenLockNotAcquired_DoesNotCallOutboxProcessor()
    {
        var ctx = BuildContext();
        ctx.DistributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<ILockHandle?>(null));

        var job = new OutboxProcessingJob(ctx.ScopeFactory, ctx.Logger);

        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            ctx.Cts.Cancel();
        });

        await job.StartAsync(ctx.Cts.Token);
        if (job.ExecuteTask is not null)
            await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(5));
        await job.StopAsync(CancellationToken.None);

        await ctx.OutboxProcessor.DidNotReceiveWithAnyArgs()
            .ProcessAsync(default, default);
    }
}
