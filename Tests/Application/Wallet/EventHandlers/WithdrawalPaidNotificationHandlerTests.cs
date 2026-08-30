using Application.Common.Events;
using Application.Notification.Contracts;
using Application.Wallet.EventHandlers;
using Domain.User.ValueObjects;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;

namespace Tests.Application.Wallet.EventHandlers;

public sealed class WithdrawalPaidNotificationHandlerTests
{
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly WithdrawalPaidNotificationHandler _sut;

    public WithdrawalPaidNotificationHandlerTests()
    {
        _sut = new WithdrawalPaidNotificationHandler(_notificationService);
    }

    private static WithdrawalPaidEvent BuildEvent(
        decimal amount = 750_000m,
        string bankRef = "BANK-REF-9001")
    {
        return new WithdrawalPaidEvent(
            WalletWithdrawalRequestId.NewId(),
            UserId.NewId(),
            Money.Create(amount),
            UserId.NewId(),
            bankRef);
    }

    private static DomainEventNotification<WithdrawalPaidEvent> Wrap(WithdrawalPaidEvent evt) => new(evt);

    [Fact]
    public async Task Handle_WhenInvoked_CreatesNotificationWithExpectedFields()
    {
        var evt = BuildEvent(amount: 1_500_000m, bankRef: "BANK-REF-42");

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            evt.UserId,
            "برداشت شما پرداخت شد",
            Arg.Is<string>(m => m!.Contains("1,500,000")
                             && m.Contains("BANK-REF-42")),
            "WithdrawalPaid",
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
            .ThrowsAsync(new InvalidOperationException("delivery failed"));

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
