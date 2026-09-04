using Application.Cache.Contracts;
using Application.Wallet.Features.Commands.ReleaseWalletReservation;
using Domain.User.ValueObjects;
using Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.Interfaces;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.BackgroundJobs;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletReservationExpiryJobTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    private WalletReservationExpiryJob BuildJob()
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
        provider.GetService(typeof(IMediator)).Returns(_mediator);
        provider.GetService(typeof(IAuditService)).Returns(_auditService);

        return new WalletReservationExpiryJob(scopeFactory, _distributedLock, Substitute.For<IDateTimeProvider>());
    }

    [Fact]
    public async Task ExecuteAsync_WithLedgerEntries_SendsReleaseCommandPerUser()
    {
        var user = await SeedUserAsync();
        var wallet = new WalletBuilder().WithOwnerId(user.Id).Build();
        wallet.ClearDomainEvents();
        Context.Wallets.Add(wallet);
        await Context.SaveChangesAsync();
        var entry = new WalletLedgerEntryBuilder()
            .WithWalletId(wallet.Id)
            .WithOwnerId(user.Id)
            .Build();
        Context.WalletLedgerEntries.Add(entry);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        ReleaseWalletReservationCommand? captured = null;
        _mediator
            .Send(Arg.Do<ReleaseWalletReservationCommand>(c => captured = c), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));
        var job = BuildJob();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await job.StartAsync(cts.Token); }        catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.UserId.ShouldBe(user.Id.Value);
        await _mediator.Received(1).Send(
            Arg.Any<ReleaseWalletReservationCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenMediatorThrows_LogsItemErrorAndContinues()
    {
        var user = await SeedUserAsync();
        var wallet = new WalletBuilder().WithOwnerId(user.Id).Build();
        wallet.ClearDomainEvents();
        Context.Wallets.Add(wallet);
        await Context.SaveChangesAsync();
        var entry = new WalletLedgerEntryBuilder()
            .WithWalletId(wallet.Id)
            .WithOwnerId(user.Id)
            .Build();
        Context.WalletLedgerEntries.Add(entry);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        _mediator
            .Send(Arg.Any<ReleaseWalletReservationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ServiceResult<Unit>>(new InvalidOperationException("handler down")));
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
            "WalletReservationExpiryItemError",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
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
            "jobs:wallet-reservation-expiry",
            TimeSpan.FromMinutes(10),
            Arg.Any<CancellationToken>());
    }
}
