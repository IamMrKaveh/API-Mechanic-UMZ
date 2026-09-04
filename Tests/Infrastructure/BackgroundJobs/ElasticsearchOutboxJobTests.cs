using Application.Cache.Contracts;
using Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.BackgroundJobs;

public class ElasticsearchOutboxJobTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly IConfiguration _configuration = new ConfigurationBuilder().Build();

    [Fact]
    public async Task ExecuteAsync_OnStart_LogsStartupWithConfiguredValues()
    {
        var job = new ElasticsearchOutboxJob(
            _scopeFactory, _auditService, _distributedLock, _configuration, Substitute.For<IDateTimeProvider>());

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await _auditService.Received(1).LogInformationAsync(
            Arg.Is<string>(s => s!.Contains("Elasticsearch outbox processor started.")),
            Arg.Any<CancellationToken>());
        await _distributedLock.DidNotReceiveWithAnyArgs().AcquireAsync(default!, default, default);
    }
}
