using Application.Cache.Contracts;
using Application.Wallet.Features.Commands.ReleaseWalletReservation;
using Domain.Wallet.ValueObjects;
using Infrastructure.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions.Interfaces;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.BackgroundJobs;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WalletReservationExpiryJobTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private static readonly DateTime FixedNow = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly IDistributedLock _distributedLock = Substitute.For<IDistributedLock>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private WalletReservationExpiryJob BuildJob()
    {
        var handle = Substitute.For<ILockHandle>();
        handle.IsAcquired.Returns(true);
        _distributedLock
            .AcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(handle);

        _dateTimeProvider.UtcNow.Returns(FixedNow);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var provider = Substitute.For<IServiceProvider>();
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(provider);
        provider.GetService(typeof(DBContext)).Returns(Context);
        provider.GetService(typeof(IDistributedLock)).Returns(_distributedLock);
        provider.GetService(typeof(IMediator)).Returns(_mediator);
        provider.GetService(typeof(IAuditService)).Returns(_auditService);

        return new WalletReservationExpiryJob(scopeFactory, _distributedLock, _dateTimeProvider);
    }

    private async Task<(Guid UserId, Guid ReservationId)> SeedReservationAsync(
        DateTime? expiresAt,
        bool releaseBeforeSave = false)
    {
        var user = await SeedUserAsync();
        var wallet = new WalletBuilder().WithOwnerId(user.Id).Build();
        wallet.Credit(Money.Create(1_000_000m, "IRT"), "seed", $"seed-{Guid.NewGuid():N}");
        var reservationId = WalletReservationId.NewId();
        wallet.CreateReservation(reservationId, Money.Create(100_000m, "IRT"), "test-hold", expiresAt);
        if (releaseBeforeSave)
            wallet.ReleaseReservation(reservationId);
        wallet.ClearDomainEvents();
        Context.Wallets.Add(wallet);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        return (user.Id.Value, reservationId.Value);
    }

    private static async Task RunJobOnceAsync(WalletReservationExpiryJob job)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try { await job.StartAsync(cts.Token); } catch (OperationCanceledException) { }
        if (job.ExecuteTask is not null)
        {
            try { await job.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(30)); }
            catch (Exception ex) when (ex is OperationCanceledException || ex is TimeoutException) { }
        }
        await job.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WithExpiredActiveReservation_SendsReleaseCommandWithRealIds()
    {
        var (userId, reservationId) = await SeedReservationAsync(FixedNow.AddMinutes(-1));

        ReleaseWalletReservationCommand? captured = null;
        _mediator
            .Send(Arg.Do<ReleaseWalletReservationCommand>(c => captured = c), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        await RunJobOnceAsync(BuildJob());

        captured.ShouldNotBeNull();
        captured!.UserId.ShouldBe(userId);
        captured!.WalletReservationId.ShouldBe(reservationId);
        captured!.WalletReservationId.ShouldNotBe(Guid.Empty);
        await _mediator.Received(1).Send(
            Arg.Any<ReleaseWalletReservationCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExpiredReservation_DoesNotSendReleaseCommand()
    {
        await SeedReservationAsync(FixedNow.AddMinutes(10));

        _mediator
            .Send(Arg.Any<ReleaseWalletReservationCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        await RunJobOnceAsync(BuildJob());

        await _mediator.DidNotReceive().Send(
            Arg.Any<ReleaseWalletReservationCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WithAlreadyReleasedReservation_DoesNotSendReleaseCommand()
    {
        await SeedReservationAsync(FixedNow.AddMinutes(-1), releaseBeforeSave: true);

        _mediator
            .Send(Arg.Any<ReleaseWalletReservationCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<Unit>.Success(Unit.Value));

        await RunJobOnceAsync(BuildJob());

        await _mediator.DidNotReceive().Send(
            Arg.Any<ReleaseWalletReservationCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenMediatorThrows_LogsItemErrorAndContinues()
    {
        await SeedReservationAsync(FixedNow.AddMinutes(-1));

        _mediator
            .Send(Arg.Any<ReleaseWalletReservationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ServiceResult<Unit>>(new InvalidOperationException("handler down")));

        await RunJobOnceAsync(BuildJob());

        await _auditService.Received(1).LogSystemEventAsync(
            "WalletReservationExpiryItemError",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_AcquiresLockWithExpectedKeyAndExpiry()
    {
        await RunJobOnceAsync(BuildJob());

        await _distributedLock.Received().AcquireAsync(
            "jobs:wallet-reservation-expiry",
            TimeSpan.FromMinutes(10),
            Arg.Any<CancellationToken>());
    }
}
