using Application.Cache.Contracts;
using Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.BackgroundJobs;

public class FraudDetectionJobTests : HandlerTestBase
{
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();

    public FraudDetectionJobTests()
    {
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        _scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(IAuditService)).Returns(AuditService);
    }

    [Fact]
    public async Task ExecuteAsync_OnStart_LogsServiceStartedAndWaitsForInitialDelay()
    {
        var job = new FraudDetectionJob(_scopeFactory, _distributedLock, DateTimeProvider);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await AuditService.Received(1).LogSystemEventAsync(
            "FraudDetection",
            "Fraud Detection Service started.",
            Arg.Any<CancellationToken>());
        await _distributedLock.DidNotReceiveWithAnyArgs().AcquireAsync(default!, default, default);
    }
}
