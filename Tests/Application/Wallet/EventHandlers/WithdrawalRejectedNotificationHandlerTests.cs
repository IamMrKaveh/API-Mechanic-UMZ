using Application.Common.Events;
using Application.Notification.Contracts;
using Application.Wallet.EventHandlers;
using Domain.User.ValueObjects;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;

namespace Tests.Application.Wallet.EventHandlers;

public sealed class WithdrawalRejectedNotificationHandlerTests
{
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly WithdrawalRejectedNotificationHandler _sut;

    public WithdrawalRejectedNotificationHandlerTests()
    {
        _sut = new WithdrawalRejectedNotificationHandler(_notificationService);
    }

    private static WithdrawalRejectedEvent BuildEvent(string reason = "invalid IBAN")
    {
        return new WithdrawalRejectedEvent(
            WalletWithdrawalRequestId.NewId(),
            UserId.NewId(),
            UserId.NewId(),
            reason);
    }

    private static DomainEventNotification<WithdrawalRejectedEvent> Wrap(WithdrawalRejectedEvent evt) => new(evt);

    [Fact]
    public async Task Handle_WhenInvoked_CreatesNotificationWithExpectedFields()
    {
        var evt = BuildEvent("duplicate request");

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            evt.UserId,
            "درخواست برداشت رد شد",
            Arg.Is<string>(m => m!.Contains("duplicate request")),
            "WithdrawalRejected",
            "/dashboard/wallet/withdrawals",
            evt.WithdrawalId.Value,
            "Withdrawal",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNotificationServiceThrows_SwallowsException()
    {
        var evt = BuildEvent();
        _notificationService
            .CreateNotificationAsync(
                Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("push failed"));

        await Should.NotThrowAsync(async () =>
            await _sut.Handle(Wrap(evt), CancellationToken.None));
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
