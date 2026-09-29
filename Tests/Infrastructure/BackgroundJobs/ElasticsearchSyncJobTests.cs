using Application.Cache.Contracts;
using Infrastructure.BackgroundJobs;
using Infrastructure.Search.Options;
using Microsoft.Extensions.Options;

namespace Tests.Infrastructure.BackgroundJobs;

public class ElasticsearchSyncJobTests : HandlerTestBase
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_LogsDisabledAndReturnsWithoutDelay()
    {
        var job = new ElasticsearchSyncJob(
            _serviceProvider,
            AuditService,
            _distributedLock,
            Microsoft.Extensions.Options.Options.Create(new ElasticsearchOptions { IsEnabled = false }));

        try { await job.StartAsync(CancellationToken.None); }
        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
            await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(10));
        await job.StopAsync(CancellationToken.None);

        await AuditService.Received(1).LogInformationAsync(
            "Elasticsearch sync is disabled",
            Arg.Any<CancellationToken>());
        await _distributedLock.DidNotReceiveWithAnyArgs().AcquireAsync(default!, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_WaitsBeforeFirstIteration()
    {
        var job = new ElasticsearchSyncJob(
            _serviceProvider,
            AuditService,
            _distributedLock,
            Microsoft.Extensions.Options.Options.Create(new ElasticsearchOptions { IsEnabled = true }));

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await _distributedLock.DidNotReceiveWithAnyArgs().AcquireAsync(default!, default, default);
    }
}
