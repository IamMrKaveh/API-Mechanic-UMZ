using Application.Cache.Contracts;
using Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.BackgroundJobs;

public class AuditRetentionJobTests
{
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();

    public AuditRetentionJobTests()
    {
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        _scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(IAuditService)).Returns(_auditService);
    }

    [Fact]
    public async Task ExecuteAsync_OnStart_LogsServiceStartedAndWaitsForInitialDelay()
    {
        var job = new AuditRetentionJob(_scopeFactory, _distributedLock, _dateTimeProvider);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "Audit Retention",
            "Audit Retention Service started.",
            Arg.Any<CancellationToken>());
        await _distributedLock.DidNotReceiveWithAnyArgs().AcquireAsync(default!, default, default);
    }
}
