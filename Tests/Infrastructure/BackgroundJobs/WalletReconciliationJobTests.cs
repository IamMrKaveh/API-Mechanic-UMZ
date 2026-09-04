using Application.Cache.Contracts;
using Domain.User.ValueObjects;
using Domain.Wallet.ValueObjects;
using Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.BackgroundJobs;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletReconciliationJobTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    private WalletReconciliationJob BuildJob()
    {
        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(true);
        _distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(handle);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(DBContext)).Returns(Context);
        provider.GetService(typeof(IDistributedLock)).Returns(_distributedLock);
        provider.GetService(typeof(IAuditService)).Returns(_auditService);

        return new WalletReconciliationJob(scopeFactory, _distributedLock);
    }

    private async Task<global::Domain.Wallet.Aggregates.Wallet> SeedWalletWithLedgerAsync(
        decimal balance, decimal ledgerSum, CancellationToken ct = default)
    {
        var user = await SeedUserAsync(ct: ct);
        var wallet = new WalletBuilder().WithOwnerId(user.Id).Build();
        wallet.Credit(Money.Create(balance), "seed credit", Guid.NewGuid().ToString("N"));
        wallet.ClearDomainEvents();
        Context.Wallets.Add(wallet);
        await Context.SaveChangesAsync(ct);

        var entry = new WalletLedgerEntryBuilder()
            .WithWalletId(wallet.Id)
            .WithOwnerId(wallet.OwnerId)
            .WithAmount(ledgerSum)
            .WithBalanceAfter(ledgerSum)
            .Build();
        Context.WalletLedgerEntries.Add(entry);
        await Context.SaveChangesAsync(ct);
        Context.ChangeTracker.Clear();

        return wallet;
    }

    [Fact]
    public async Task ExecuteAsync_WhenBalanceMismatchesLedger_LogsDiscrepancy()
    {
        await SeedWalletWithLedgerAsync(balance: 250_000m, ledgerSum: 100_000m);
        var job = BuildJob();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "WalletReconciliationDiscrepancy",
            Arg.Is<string>(s => s!.Contains("150000")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenBalanceMatchesLedger_DoesNotLog()
    {
        await SeedWalletWithLedgerAsync(balance: 100_000m, ledgerSum: 100_000m);
        var job = BuildJob();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await _auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenLockNotAcquired_DoesNothing()
    {
        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(false);
        _distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(handle);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var job = new WalletReconciliationJob(scopeFactory, _distributedLock);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        scopeFactory.DidNotReceiveWithAnyArgs().CreateScope();
    }

    [Fact]
    public async Task ExecuteAsync_AcquiresLockWithExpectedKeyAndExpiry()
    {
        var job = BuildJob();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        await _distributedLock.Received().AcquireAsync(
            "jobs:wallet-reconciliation",
            TimeSpan.FromMinutes(45),
            Arg.Any<CancellationToken>());
    }
}
