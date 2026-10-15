using Application.Cache.Contracts;
using Infrastructure.BackgroundJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.Interfaces;

namespace Tests.Infrastructure.BackgroundJobs;

public class ElasticsearchOutboxJobTests : HandlerTestBase
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly IConfiguration _configuration = new ConfigurationBuilder().Build();

    [Fact]
    public async Task ExecuteAsync_OnStart_LogsStartupWithConfiguredValues()
    {
        var job = new ElasticsearchOutboxJob(
            _scopeFactory, AuditService, _distributedLock, _configuration, DateTimeProvider);

        // .NET 10: StartAsync with an already-cancelled token never starts ExecuteAsync, so pass a live token.
        try { await job.StartAsync(CancellationToken.None); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await AuditService.Received(1).LogInformationAsync(
            Arg.Is<string>(s => s!.Contains("Elasticsearch outbox processor started.")),
            Arg.Any<CancellationToken>());
        await _distributedLock.DidNotReceiveWithAnyArgs().AcquireAsync(default!, default, default);
    }
}
