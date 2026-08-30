using Application.Common.Events;
using Application.Notification.Contracts;
using Application.Wallet.EventHandlers;
using Domain.User.ValueObjects;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;

namespace Tests.Application.Wallet.EventHandlers;

public sealed class SendWalletDebitRequestCreatedNotificationHandlerTests
{
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly SendWalletDebitRequestCreatedNotificationHandler _sut;

    public SendWalletDebitRequestCreatedNotificationHandlerTests()
    {
        _sut = new SendWalletDebitRequestCreatedNotificationHandler(_notificationService, _auditService);
    }

    private static WalletDebitRequestCreatedEvent BuildEvent(
        decimal amount = 150_000m,
        string currency = "IRT",
        string reason = "manual adjustment")
    {
        return new WalletDebitRequestCreatedEvent(
            WalletId.NewId(),
            UserId.NewId(),
            WalletDebitRequestId.NewId(),
            Money.Create(amount, currency),
            reason,
            UserId.NewId());
    }

    private static DomainEventNotification<WalletDebitRequestCreatedEvent> Wrap(WalletDebitRequestCreatedEvent evt) =>
        new(evt);

    [Fact]
    public async Task Handle_WhenInvoked_CreatesNotificationWithExpectedFields()
    {
        var evt = BuildEvent(amount: 250_000m);

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            evt.OwnerId,
            "درخواست کسر از کیف پول",
            Arg.Is<string>(m => m!.Contains("250,000")
                             && m.Contains("IRT")
                             && m.Contains("تایید")
                             && m.Contains("رد")),
            "WalletDebitRequest",
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
            .ThrowsAsync(new InvalidOperationException("channel closed"));

        await Should.NotThrowAsync(async () =>
            await _sut.Handle(Wrap(evt), CancellationToken.None));

        await _auditService.Received(1).LogSystemEventAsync(
            "WalletDebitRequestCreatedNotificationFailed",
            Arg.Is<string>(s => s!.Contains(evt.OwnerId.Value.ToString())
                             && s.Contains("channel closed")),
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
