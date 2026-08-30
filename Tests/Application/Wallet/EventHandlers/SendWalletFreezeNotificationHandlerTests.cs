using Application.Common.Events;
using Application.Notification.Contracts;
using Application.Wallet.EventHandlers;
using Domain.User.ValueObjects;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;

namespace Tests.Application.Wallet.EventHandlers;

public sealed class SendWalletFreezeNotificationHandlerTests
{
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly SendWalletFreezeNotificationHandler _sut;

    public SendWalletFreezeNotificationHandlerTests()
    {
        _sut = new SendWalletFreezeNotificationHandler(_notificationService, _auditService);
    }

    private static WalletFrozenEvent BuildEvent(string reason)
    {
        return new WalletFrozenEvent(
            WalletId.NewId(),
            UserId.NewId(),
            reason,
            UserId.NewId());
    }

    private static DomainEventNotification<WalletFrozenEvent> Wrap(WalletFrozenEvent evt) => new(evt);

    [Fact]
    public async Task Handle_WhenReasonProvided_CreatesNotificationContainingReason()
    {
        var evt = BuildEvent("suspicious activity");

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            evt.OwnerId,
            "مسدود شدن کیف پول",
            Arg.Is<string>(m => m!.Contains("suspicious activity")
                             && m.Contains("پشتیبانی")),
            "WalletFrozen",
            "/wallet",
            evt.WalletId.Value,
            "Wallet",
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenReasonEmptyOrWhitespace_UsesDefaultReasonPlaceholder(string reason)
    {
        var evt = BuildEvent(reason);

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            evt.OwnerId,
            "مسدود شدن کیف پول",
            Arg.Is<string>(m => m!.Contains("بدون دلیل ثبت‌شده")),
            "WalletFrozen",
            "/wallet",
            evt.WalletId.Value,
            "Wallet",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotificationServiceThrows_LogsSystemEventAndSwallows()
    {
        var evt = BuildEvent("compliance-hold");
        _notificationService
            .CreateNotificationAsync(
                Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("gateway timeout"));

        await Should.NotThrowAsync(async () =>
            await _sut.Handle(Wrap(evt), CancellationToken.None));

        await _auditService.Received(1).LogSystemEventAsync(
            "WalletFreezeNotificationFailed",
            Arg.Is<string>(s => s!.Contains(evt.OwnerId.Value.ToString())
                             && s.Contains("gateway timeout")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        var evt = BuildEvent("suspended");

        await _sut.Handle(Wrap(evt), cts.Token);

        await _notificationService.Received(1).CreateNotificationAsync(
            Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), cts.Token);
    }
}
