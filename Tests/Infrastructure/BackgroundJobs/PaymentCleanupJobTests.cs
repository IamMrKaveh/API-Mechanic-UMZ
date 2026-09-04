using Application.Cache.Contracts;
using Application.Payment.Features.Commands.ExpireStalePayments;
using Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.BackgroundJobs;

public class PaymentCleanupJobTests
{
    private sealed class TestContext
    {
        public IServiceScopeFactory ScopeFactory { get; init; } = null!;
        public IDistributedLock DistributedLock { get; init; } = null!;
        public IMediator Mediator { get; init; } = null!;
        public IAuditService AuditService { get; init; } = null!;
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

        var mediator = Substitute.For<IMediator>();
        var auditService = Substitute.For<IAuditService>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(FixedNow);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(IMediator)).Returns(mediator);
        provider.GetService(typeof(IAuditService)).Returns(auditService);

        return new TestContext
        {
            ScopeFactory = scopeFactory,
            DistributedLock = distributedLock,
            Mediator = mediator,
            AuditService = auditService,
            DateTimeProvider = dateTimeProvider,
            Cts = new CancellationTokenSource()
        };
    }

    private static async Task RunWithTimeoutAsync(PaymentCleanupJob job, CancellationToken token)
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
    public async Task ExecuteAsync_WhenLockNotAcquired_DoesNotSendCommand()
    {
        var ctx = BuildContext(lockAcquired: false);
        var job = new PaymentCleanupJob(ctx.ScopeFactory, ctx.DistributedLock, ctx.DateTimeProvider);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await RunWithTimeoutAsync(job, timeout.Token);

        await ctx.Mediator.DidNotReceiveWithAnyArgs().Send(default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStaleTransactionsExpired_LogsInformation()
    {
        var ctx = BuildContext();
        ctx.Mediator
            .Send(Arg.Any<ExpireStalePaymentsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ServiceResult<int>.Success(3)));
        ctx.AuditService
            .LogInformationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                ctx.Cts.Cancel();
                return Task.CompletedTask;
            });
        var job = new PaymentCleanupJob(ctx.ScopeFactory, ctx.DistributedLock, ctx.DateTimeProvider);

        try { await job.StartAsync(ctx.Cts.Token); }
        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await ctx.AuditService.Received(1).LogInformationAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenNothingExpired_DoesNotLog()
    {
        var ctx = BuildContext();
        ctx.Mediator
            .Send(Arg.Any<ExpireStalePaymentsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ServiceResult<int>.Success(0)));
        var job = new PaymentCleanupJob(ctx.ScopeFactory, ctx.DistributedLock, ctx.DateTimeProvider);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try { await job.StartAsync(timeout.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(15)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await ctx.AuditService.DidNotReceive().LogInformationAsync(
            Arg.Is<string>(s => s!.Contains("Expired")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SendsCommandWithTwentyMinuteCutoff()
    {
        var ctx = BuildContext();
        ExpireStalePaymentsCommand? captured = null;
        ctx.Mediator
            .Send(Arg.Do<ExpireStalePaymentsCommand>(c => captured = c), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ServiceResult<int>.Success(0)));
        var before = FixedNow;
        var job = new PaymentCleanupJob(ctx.ScopeFactory, ctx.DistributedLock, ctx.DateTimeProvider);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try { await job.StartAsync(timeout.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(15)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.CutOff.ShouldBeGreaterThan(before.AddMinutes(-21));
        captured.CutOff.ShouldBeLessThan(before.AddMinutes(-19));
    }

    [Fact]
    public async Task ExecuteAsync_AcquiresLockWithExpectedKeyAndExpiry()
    {
        var ctx = BuildContext();
        ctx.Mediator
            .Send(Arg.Any<ExpireStalePaymentsCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<int>.Success(0));
        var job = new PaymentCleanupJob(ctx.ScopeFactory, ctx.DistributedLock, ctx.DateTimeProvider);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await RunWithTimeoutAsync(job, timeout.Token);

        await ctx.DistributedLock.Received().AcquireAsync(
            "jobs:payment-cleanup",
            TimeSpan.FromMinutes(10),
            Arg.Any<CancellationToken>());
    }
}
