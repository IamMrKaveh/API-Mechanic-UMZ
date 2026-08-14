using Application.Cache.Contracts;
using Infrastructure.BackgroundJobs.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Infrastructure.BackgroundJobs.Common;

public class DistributedLockedBackgroundServiceTests
{
    private sealed class TestableJob(IServiceScopeFactory scopeFactory, ILogger logger, Func<IServiceProvider, CancellationToken, Task> body, string lockKey, TimeSpan interval, TimeSpan lockExpiry) : DistributedLockedBackgroundService(scopeFactory, logger)
    {
        private int _callCount; public int CallCount => _callCount;

        protected override string LockKey => lockKey;
        protected override TimeSpan Interval => interval;
        protected override TimeSpan LockExpiry => lockExpiry;

        protected override async Task ExecuteInsideLockAsync(IServiceProvider services, CancellationToken ct)
        {
            _callCount++;
            await body(services, ct);
        }

        public Task InvokeExecuteAsync(CancellationToken ct) => ExecuteAsync(ct);
    }

    private static (IServiceScopeFactory factory, IServiceProvider provider, IServiceScope scope)
        BuildScopeChain(IDistributedLock distributedLock)
    {
        var factory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();

        factory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(IDistributedLock)).Returns(distributedLock);

        return (factory, provider, scope);
    }

    private static ILockHandle AcquiredHandle()
    {
        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(true);
        handle.Resource.Returns("test:lock");
        return handle;
    }

    private static ILockHandle NotAcquiredHandle()
    {
        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(false);
        return handle;
    }

    [Fact]
    public async Task ExecuteAsync_WhenLockAcquired_InvokesInsideLockBody()
    {
        var distributedLock = Substitute.For<IDistributedLock>();
        var acquiredHandle = AcquiredHandle();
        distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(acquiredHandle);

        var (factory, _, _) = BuildScopeChain(distributedLock);
        var logger = Substitute.For<ILogger>();
        using var cts = new CancellationTokenSource();

        var job = new TestableJob(
            factory,
            logger,
            body: (_, _) =>
            {
                cts.Cancel();
                return Task.CompletedTask;
            },
            lockKey: "test:lock",
            interval: TimeSpan.FromMilliseconds(1),
            lockExpiry: TimeSpan.FromMinutes(1));

        await job.InvokeExecuteAsync(cts.Token);

        job.CallCount.ShouldBe(1);
        await distributedLock.Received().AcquireAsync(
            "test:lock",
            TimeSpan.FromMinutes(1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenAcquireReturnsNullHandle_DoesNotInvokeBody()
    {
        var distributedLock = Substitute.For<IDistributedLock>();
        var callCount = 0;
        distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<ILockHandle?>(null));

        var (factory, _, _) = BuildScopeChain(distributedLock);
        var logger = Substitute.For<ILogger>();
        using var cts = new CancellationTokenSource();

        var job = new TestableJob(
            factory,
            logger,
            body: (_, _) =>
            {
                callCount++;
                return Task.CompletedTask;
            },
            lockKey: "test:lock",
            interval: TimeSpan.FromMilliseconds(1),
            lockExpiry: TimeSpan.FromMinutes(1));

        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            cts.Cancel();
        });

        await job.InvokeExecuteAsync(cts.Token);

        callCount.ShouldBe(0);
        job.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHandleIsNotAcquired_DoesNotInvokeBody()
    {
        var distributedLock = Substitute.For<IDistributedLock>();
        var notAcquiredHandle = NotAcquiredHandle();
        distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(notAcquiredHandle);

        var (factory, _, _) = BuildScopeChain(distributedLock);
        var logger = Substitute.For<ILogger>();
        using var cts = new CancellationTokenSource();

        var job = new TestableJob(
            factory,
            logger,
            body: (_, _) => Task.CompletedTask,
            lockKey: "test:lock",
            interval: TimeSpan.FromMilliseconds(1),
            lockExpiry: TimeSpan.FromMinutes(1));

        _ = Task.Run(async () =>
        {
            await Task.Delay(50);
            cts.Cancel();
        });

        await job.InvokeExecuteAsync(cts.Token);

        job.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_WhenBodyThrows_LogsErrorAndContinuesLoop()
    {
        var distributedLock = Substitute.For<IDistributedLock>();
        distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<ILockHandle?>(AcquiredHandle()));

        var (factory, _, _) = BuildScopeChain(distributedLock);
        var logger = Substitute.For<ILogger>();
        using var cts = new CancellationTokenSource();

        var iterationsSeen = 0;

        var job = new TestableJob(
            factory,
            logger,
            body: (_, _) =>
            {
                iterationsSeen++;
                if (iterationsSeen == 1)
                    throw new InvalidOperationException("boom");
                cts.Cancel();
                return Task.CompletedTask;
            },
            lockKey: "test:lock",
            interval: TimeSpan.FromMilliseconds(1),
            lockExpiry: TimeSpan.FromMinutes(1));

        await job.InvokeExecuteAsync(cts.Token);

        iterationsSeen.ShouldBeGreaterThanOrEqualTo(2);
        logger.ReceivedWithAnyArgs().Log(
            default,
            default,
            default!,
            default,
            default!);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStoppingTokenCancelled_ExitsCleanly()
    {
        var distributedLock = Substitute.For<IDistributedLock>();
        distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<ILockHandle?>(AcquiredHandle()));

        var (factory, _, _) = BuildScopeChain(distributedLock);
        var logger = Substitute.For<ILogger>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var job = new TestableJob(
            factory,
            logger,
            body: (_, _) => Task.CompletedTask,
            lockKey: "test:lock",
            interval: TimeSpan.FromMilliseconds(1),
            lockExpiry: TimeSpan.FromMinutes(1));

        await Should.NotThrowAsync(() => job.InvokeExecuteAsync(cts.Token));

        job.CallCount.ShouldBe(0);
    }
}
