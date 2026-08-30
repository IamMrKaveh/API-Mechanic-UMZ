using Application.Common.Events;
using Application.Notification.Contracts;
using Application.Wallet.EventHandlers;
using Domain.User.ValueObjects;
using Domain.Wallet.Events;
using Domain.Wallet.ValueObjects;

namespace Tests.Application.Wallet.EventHandlers;

public sealed class SendWalletDebitNotificationHandlerTests
{
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly SendWalletDebitNotificationHandler _sut;

    public SendWalletDebitNotificationHandlerTests()
    {
        _sut = new SendWalletDebitNotificationHandler(_notificationService, _auditService);
    }

    private static WalletDebitedEvent BuildDebitEvent(
        string description,
        decimal amount = 40_000m,
        decimal newBalance = 60_000m,
        string currency = "IRT")
    {
        return new WalletDebitedEvent(
            WalletId.NewId(),
            UserId.NewId(),
            Money.Create(amount, currency),
            Money.Create(newBalance, currency),
            description,
            "ref-debit-001");
    }

    private static DomainEventNotification<WalletDebitedEvent> Wrap(WalletDebitedEvent evt) => new(evt);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("regular purchase")]
    [InlineData("[admin-debit] wrong casing")]
    [InlineData(" [ADMIN-DEBIT] leading space")]
    public async Task Handle_WhenDescriptionDoesNotStartWithAdminDebitPrefix_DoesNothing(string description)
    {
        var evt = BuildDebitEvent(description);

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.DidNotReceiveWithAnyArgs().CreateNotificationAsync(
            default(UserId)!, default!, default!, default!, default, default, default, default);
        await _auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenDescriptionHasAdminDebitPrefix_CreatesNotificationWithExpectedFields()
    {
        var evt = BuildDebitEvent("[ADMIN-DEBIT] correction", amount: 80_000m, newBalance: 20_000m);

        await _sut.Handle(Wrap(evt), CancellationToken.None);

        await _notificationService.Received(1).CreateNotificationAsync(
            evt.OwnerId,
            "کسر از کیف پول توسط پشتیبانی",
            Arg.Is<string>(m => m!.Contains("80,000")
                             && m.Contains("IRT")
                             && m.Contains("20,000")),
            "WalletDebit",
            "/wallet",
            evt.WalletId.Value,
            "Wallet",
            Arg.Any<CancellationToken>());
        await _auditService.DidNotReceiveWithAnyArgs().LogSystemEventAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenNotificationServiceThrows_LogsSystemEventAndSwallows()
    {
        var evt = BuildDebitEvent("[ADMIN-DEBIT] chargeback");
        _notificationService
            .CreateNotificationAsync(
                Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("push failed"));

        await Should.NotThrowAsync(async () =>
            await _sut.Handle(Wrap(evt), CancellationToken.None));

        await _auditService.Received(1).LogSystemEventAsync(
            "WalletDebitNotificationFailed",
            Arg.Is<string>(s => s!.Contains(evt.OwnerId.Value.ToString())
                             && s.Contains("push failed")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        var evt = BuildDebitEvent("[ADMIN-DEBIT] adjustment");

        await _sut.Handle(Wrap(evt), cts.Token);

        await _notificationService.Received(1).CreateNotificationAsync(
            Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), cts.Token);
    }
}
