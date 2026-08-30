using Application.Common.Events;
using Application.Notification.Contracts;
using Application.Wallet.EventHandlers;
using Domain.User.ValueObjects;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;

namespace Tests.Application.Wallet.EventHandlers;

public sealed class SendWalletUnfreezeNotificationHandlerTests
{
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly SendWalletUnfreezeNotificationHandler _sut;

    public SendWalletUnfreezeNotificationHandlerTests()
    {
        _sut = new SendWalletUnfreezeNotificationHandler(_notificationService, _auditService);
    }

    private static WalletUnfrozenEvent BuildEvent(string reason = "review passed")
    {
        return new WalletUnfrozenEvent(
            WalletId.NewId(),
            UserId.NewId(),
            UserId.NewId(),
            reason);
    }

    private static DomainEventNotification<WalletUnfrozenEvent> Wrap(WalletUnfrozenEvent evt) => new(evt);

    [Fact]
    public async Task Handle_WhenInvoked_CreatesNotificationWithExpectedFields()
    {
        var evt = BuildEvent();

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            evt.OwnerId,
            "رفع مسدودی کیف پول",
            Arg.Is<string>(m => m!.Contains("پشتیبانی")
                             && m.Contains("رفع مسدود")),
            "WalletUnfrozen",
            "/wallet",
            evt.WalletId.Value,
            "Wallet",
            Arg.Any<CancellationToken>());
        await _auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenNotificationServiceThrows_LogsSystemEventAndSwallows()
    {
        var evt = BuildEvent();
        _notificationService
            .CreateNotificationAsync(
                Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("broker error"));

        await Should.NotThrowAsync(async () =>
            await _sut.Handle(Wrap(evt), CancellationToken.None));

        await _auditService.Received(1).LogSystemEventAsync(
            "WalletUnfreezeNotificationFailed",
            Arg.Is<string>(s => s!.Contains(evt.OwnerId.Value.ToString())
                             && s.Contains("broker error")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        var evt = BuildEvent();

        await _sut.Handle(Wrap(evt), cts.Token);

        await _notificationService.Received(1).CreateNotificationAsync(
            Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), cts.Token);
    }
}
