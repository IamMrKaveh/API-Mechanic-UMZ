using Application.Auth.EventHandlers;
using Application.Common.Events;
using Domain.User.Events;
using Domain.User.ValueObjects;
using Domain.Wallet.Interfaces;
using SharedKernel.Abstractions.Interfaces;
using Wallets = Domain.Wallet.Aggregates.Wallet;

namespace Tests.Application.Auth.EventHandlers;

public class UserCreatedEventHandlerTests : HandlerTestBase
{
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly ILogger<UserCreatedEventHandler> _logger = Substitute.For<ILogger<UserCreatedEventHandler>>();
    private readonly UserCreatedEventHandler _sut;

    public UserCreatedEventHandlerTests()
    {

        _sut = new UserCreatedEventHandler(_walletRepository, UnitOfWork, AuditService, DateTimeProvider, _logger);
    }

    private static DomainEventNotification<UserRegisteredEvent> BuildNotification(UserId? userId = null)
    {
        var evt = new UserRegisteredEvent(
            userId ?? UserId.NewId(),
            Email.Create($"user{Guid.NewGuid():N}@example.com"),
            "Ali",
            "Rezaei");
        return new DomainEventNotification<UserRegisteredEvent>(evt);
    }

    [Fact]
    public async Task Handle_WhenValid_AddsWalletToRepositoryWithMatchingOwnerId()
    {
        var userId = UserId.NewId();
        var notification = BuildNotification(userId);

        await _sut.Handle(notification, CancellationToken.None);

        await _walletRepository.Received(1).AddAsync(
            Arg.Is<Wallets>(w => w!.OwnerId == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesWalletWithIrrCurrency()
    {
        var notification = BuildNotification();

        await _sut.Handle(notification, CancellationToken.None);

        await _walletRepository.Received(1).AddAsync(
            Arg.Is<Wallets>(w => w!.Balance.Currency == "IRR"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValid_SavesChangesOnUnitOfWork()
    {
        var notification = BuildNotification();

        await _sut.Handle(notification, CancellationToken.None);

        await UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValid_LogsSystemEventWithUserId()
    {
        var userId = UserId.NewId();
        var notification = BuildNotification(userId);

        await _sut.Handle(notification, CancellationToken.None);

        await AuditService.Received(1).LogSystemEventAsync(
            "Wallet creation",
            Arg.Is<string>(s => s!.Contains(userId.Value.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrows_LogsFailureViaAuditServiceAndDoesNotRethrow()
    {
        var userId = UserId.NewId();
        var notification = BuildNotification(userId);
        var exception = new InvalidOperationException("db failure");

        _walletRepository
            .AddAsync(Arg.Any<Wallets>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

        await Should.NotThrowAsync(() => _sut.Handle(notification, CancellationToken.None));

        await AuditService.Received(1).LogSystemEventAsync(
            exception.Message,
            Arg.Is<string>(s => s!.Contains(userId.Value.ToString())),
            Arg.Any<CancellationToken>());
        await UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSaveChangesThrows_LogsFailureAndDoesNotRethrow()
    {
        var userId = UserId.NewId();
        var notification = BuildNotification(userId);
        var exception = new InvalidOperationException("save changes failed");

        UnitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

        await Should.NotThrowAsync(() => _sut.Handle(notification, CancellationToken.None));

        await AuditService.Received(1).LogSystemEventAsync(
            exception.Message,
            Arg.Is<string>(s => s!.Contains(userId.Value.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenDownstream()
    {
        using var cts = new CancellationTokenSource();
        var notification = BuildNotification();

        await _sut.Handle(notification, cts.Token);

        await _walletRepository.Received(1).AddAsync(Arg.Any<Wallets>(), cts.Token);
        await UnitOfWork.Received(1).SaveChangesAsync(cts.Token);
    }

    [Fact]
    public async Task Handle_OrdersAddBeforeSaveChanges()
    {
        var notification = BuildNotification();

        await _sut.Handle(notification, CancellationToken.None);

        Received.InOrder(() =>
        {
            _walletRepository.AddAsync(Arg.Any<Wallets>(), Arg.Any<CancellationToken>());
            UnitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }
}
